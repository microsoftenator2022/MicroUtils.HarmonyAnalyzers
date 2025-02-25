using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using MicroUtils.HarmonyAnalyzers.Rules;

namespace MicroUtils.HarmonyAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public partial class PatchClassAnalyzer : DiagnosticAnalyzer
{
#if DEBUG
    internal static readonly DiagnosticDescriptor DebugMessage = new(
#pragma warning disable RS2000 // Add analyzer diagnostic IDs to analyzer release
        "DEBUG",
#pragma warning restore RS2000 // Add analyzer diagnostic IDs to analyzer release
        "Debug message",
        "{0}",
        "Debug",
        DiagnosticSeverity.Info,
        true);
#endif

    internal static readonly DiagnosticDescriptor PatchInfo = new(
        "MHI000",
        "Harmony Patch Method Info",
        "{0}",
        "PatchInfo",
        DiagnosticSeverity.Hidden,
        true);

    [ThreadStatic]
    static StringBuilder? StringBuilder;

    private static ImmutableArray<Diagnostic> PatchMethodInfo(PatchMethodData methodData)
    {
        var args = Optional.MaybeNullableValue(methodData.ArgumentTypes)
            .Select(args => string.Join(", ", args.AsEnumerable()));

        var sb = (StringBuilder ??= new()).Clear()
            .AppendLine("Patch Method Info")
            .AppendLine($"Patch type: {Optional.MaybeNullableValue(methodData.PatchType)}")
            .AppendLine($"Target type: {Optional.MaybeValue(methodData.TargetType)}")
            .AppendLine($"Target method name: {Optional.MaybeValue(methodData.TargetMethodName)}")
            .AppendLine($"Method type: {Optional.MaybeNullableValue(methodData.TargetMethodType)}")
            .AppendLine($"Target method args: {args}")
            .Append($"Effective target method: {methodData.TargetMethod}");

        return methodData.CreateDiagnostics(PatchInfo, messageArgs: [sb.ToString()]);
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        PatchInfo,
#if DEBUG
        DebugMessage,
#endif
        MissingClassAttribute.Descriptor,
        MissingPatchTypeAttribute.Descriptor,
        MissingMethodType.Descriptor,
        AmbiguousMatch.Descriptor,
        TargetMethodMatchFailed.Descriptor,
        NoPatchMethods.Descriptor,
        MultipleTargetMethodDefinitions.Descriptor,
        PatchTypeAttributeConflict.Descriptor,
        InvalidPatchMethodReturnType.Descriptor,
        PaasthroughPostfixResultInjection.Descriptor,
        AssignmentToNonRefResultArgument.Descriptor,
        InjectedParamterNotFoundOnTargetMethod.Descriptor,
        PatchAttributeConflict.Descriptor,
        InvalidInjectedParameterType.Descriptor,
        InvalidTranspilerParameter.Descriptor,
        UseOutForPrefixStateInjection.Descriptor,
        ParameterIndexInjection.Descriptor,
        ReversePatchType.Descriptor
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(context =>
        {
            // Need to see non-public members and types from external references.
            var compilation = context.Compilation.WithAllMembers();

            // This should be fine
#pragma warning disable RS1030 // Do not invoke Compilation.GetSemanticModel() method within a diagnostic analyzer
            SemanticModel GetSemanticModel(SyntaxTree tree) => compilation.GetSemanticModel(tree, true);
#pragma warning restore RS1030 // Do not invoke Compilation.GetSemanticModel() method within a diagnostic analyzer

            SemanticModel GetCachedSemanticModel(SyntaxTree tree) =>
                context.TryGetValue<SemanticModel>(tree, new(GetSemanticModel), out var sm) ?
                    sm : GetSemanticModel(tree);

            context.RegisterSyntaxNodeAction(
                snContext =>
                {
                    if (snContext.Node is not ClassDeclarationSyntax cds)
                        return;

                    var semanticModel = GetCachedSemanticModel(snContext.FilterTree);
#if DEBUG
                    try
                    {
#endif
                        AnalyzeClassDeclaration(
                            cds,
                            compilation,
                            semanticModel,
                            report => snContext.ReportDiagnostic(report),
                            snContext.CancellationToken);
#if DEBUG
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(ex.StackTrace);
                    }
#endif
                }, SyntaxKind.ClassDeclaration);
        });
    }

    private static void AnalyzeClassDeclaration(
        ClassDeclarationSyntax cds,
        Compilation compilation,
        SemanticModel sm,
        Action<Diagnostic> report,
        CancellationToken ct)
    {
        if (sm.GetDeclaredSymbol(cds, ct) is not INamedTypeSymbol classSymbol)
            return;

        if (HarmonyHelpers.GetHarmonyPatchType(compilation, ct) is not { } harmonyAttribute)
            return;

        var classAttributes = classSymbol.GetAttributes()
            .Where(attr => attr.AttributeClass?.Equals(harmonyAttribute, SymbolEqualityComparer.Default) ?? false)
            .ToImmutableArray();

        var patchMethods = classSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Select(m =>
            {
                var attrs = m.GetAttributes()
                    .Where(attr => attr.AttributeClass is { } type && 
                        (type.Equals(harmonyAttribute, SymbolEqualityComparer.Default) ||
                        HarmonyHelpers.GetHarmonyPatchTypeAttributeTypes(compilation, ct)
                            .Any(at => at.Item2.Equals(type, SymbolEqualityComparer.Default))
                        ))
                    .ToImmutableArray();

                return (m, attrs);
            })
            .Where(pair => pair.attrs.Length > 0 || 
                (classAttributes.Length > 0 && HarmonyConstants.HarmonyPatchTypeNames.Contains(pair.m.Name)))
            .ToImmutableArray();
        
        if (classAttributes.Length == 0 && patchMethods.Length == 0)
            return;

        var diagnostics = ImmutableArray<Diagnostic>.Empty;

        var patchMethodsData = patchMethods
            .Select(pair =>
            {
                var methodData = new PatchMethodData(classSymbol, pair.m, compilation)
                    .AddTargetMethodData(classAttributes)
                    .AddTargetMethodData(pair.attrs);

                if (HarmonyHelpers.TryParseHarmonyPatchType(pair.m.Name, out var methodNamePatchType))
                    methodData = methodData with { PatchType = methodNamePatchType };

                var maybeAttr = methodData.GetPatchTypeAttributes(compilation, ct).TryFirst();

                if (maybeAttr.HasValue)
                    methodData = methodData with { PatchType = maybeAttr.Value.Item2 };

                return methodData;
            })
            .ToImmutableArray();

#region Rules for patch class
        diagnostics = diagnostics
            .AddRange(MissingClassAttribute.Check(classSymbol, classAttributes, patchMethodsData, harmonyAttribute))
            .AddRange(NoPatchMethods.Check(classSymbol, classAttributes, patchMethodsData));
#endregion

#region General patch method rules
        foreach (var patchMethodData in patchMethodsData)
        {
            if (ct.IsCancellationRequested)
                break;
#if DEBUG
            foreach (var d in patchMethodData.CreateDiagnostics(DebugMessage, messageArgs: [patchMethodData]))
            {
                if (ct.IsCancellationRequested)
                    break;

                report(d);
            }
#endif
            diagnostics = diagnostics
                .AddRange(MissingPatchTypeAttribute.Check(patchMethodData))
                .AddRange(PatchTypeAttributeConflict.Check(patchMethodData, ct))
                .AddRange(InvalidPatchMethodReturnType.CheckPatchMethod(patchMethodData, ct))
                .AddRange(PaasthroughPostfixResultInjection.Check(patchMethodData))
                .AddRange(AssignmentToNonRefResultArgument.Check(sm, patchMethodData, ct))
                .AddRange(InjectedParamterNotFoundOnTargetMethod.Check(patchMethodData, ct))
                .AddRange(PatchAttributeConflict.Check(patchMethodData, ct))
                .AddRange(InvalidInjectedParameterType.Check(patchMethodData))
                .AddRange(InvalidTranspilerParameter.Check(patchMethodData, ct))
                .AddRange(UseOutForPrefixStateInjection.Check(patchMethodData))
                .AddRange(ParameterIndexInjection.Check(patchMethodData))
                .AddRange(ReversePatchType.Check(patchMethodData, ct));
        }
#endregion

#region Rules for TargetMethod/TargetMethods
        bool isTargetMethod(IMethodSymbol m) =>
            m.Name is HarmonyConstants.TargetMethodMethodName ||
            m.GetAttributes().Any(attr => attr.AttributeClass is not null &&
                attr.AttributeClass.Equals(HarmonyHelpers.GetHarmonyTargetMethodType(compilation, ct), SymbolEqualityComparer.Default));

        bool isTargetMethods(IMethodSymbol m) =>
            m.Name is HarmonyConstants.TargetMethodsMethodName ||
            m.GetAttributes().Any(attr => attr.AttributeClass is not null &&
                attr.AttributeClass.Equals(HarmonyHelpers.GetHarmonyTargetMethodsType(compilation, ct), SymbolEqualityComparer.Default));

        var targetMethodMethods = classSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(isTargetMethod)
            .ToImmutableArray();

        var targetMethodsMethods = classSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(isTargetMethods)
            .ToImmutableArray();

        var MethodBaseType = compilation.GetTypeByMetadataName(typeof(MethodBase).ToString());
        var IEnumerableMethodBaseType = MethodBaseType is { } mb ?
            (compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T))?.Construct(mb) : null;

        var allPatchTargetMethodMembers = targetMethodMethods.Concat(targetMethodsMethods).ToImmutableArray();

        if (MethodBaseType is not null && IEnumerableMethodBaseType is not null && allPatchTargetMethodMembers.Count() > 0)
        {
            diagnostics = diagnostics
                .AddRange(MultipleTargetMethodDefinitions.Check(
                    classSymbol, classAttributes, patchMethodsData, allPatchTargetMethodMembers, ct));

            foreach (var m in targetMethodMethods)
            {
                diagnostics = diagnostics
                    .AddRange(InvalidPatchMethodReturnType.CheckTargetMethod(compilation, m, MethodBaseType));
            }

            foreach (var m in targetMethodsMethods)
            {
                diagnostics = diagnostics
                    .AddRange(InvalidPatchMethodReturnType.CheckTargetMethods(compilation, m, IEnumerableMethodBaseType));
            }
        }
#endregion
        
#region Rules for target method resolution
        else
        {
            foreach (var patchMethodData in patchMethodsData)
            {
                if (ct.IsCancellationRequested)
                    break;

                if (patchMethodData.TargetMethod is not null)
                    continue;

                var missingMethodTypes = MissingMethodType.Check(patchMethodData, ct);
                if (missingMethodTypes.Length > 0)
                {
                    diagnostics = diagnostics.AddRange(missingMethodTypes);

                    continue;
                }

                var ambiguous = AmbiguousMatch.Check(patchMethodData);
                if (ambiguous.Length > 0)
                {
                    diagnostics = diagnostics.AddRange(ambiguous);

                    continue;
                }

                // typeName target type may not exist within a reference of this compilation. Assume the user knows what they're doing
                // TODO: Consider an analyzer that recommends using typeof if the type *is* in a referenced assembly
                if (patchMethodData.HarmonyPatchAttributes.Any(attr => attr.AttributeConstructor?.Parameters.Any(p => p.Name == "typeName") ?? false))
                    continue;

                diagnostics = diagnostics.AddRange(patchMethodData.CreateDiagnostics(TargetMethodMatchFailed.Descriptor));
#if DEBUG
                diagnostics = diagnostics.Add(Diagnostic.Create(
                    DebugMessage,
                    patchMethodData.PatchMethod.Locations[0],
                    messageArgs: [String.Join(", ", patchMethodData.TargetType?.MemberNames)]));
#endif

            }
        }
        #endregion

        diagnostics = diagnostics.AddRange(patchMethodsData.SelectMany(m => PatchMethodInfo(m)));

        foreach (var diagnostic in diagnostics)
        {
            if (ct.IsCancellationRequested)
                return;

            report(diagnostic);
        }
    }
}
