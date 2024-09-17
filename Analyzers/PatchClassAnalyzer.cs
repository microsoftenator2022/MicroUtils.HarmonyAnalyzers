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

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
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

        context.RegisterSyntaxNodeAction(
#if DEBUG
        context =>
        {
            try
            {
                AnalyzeClassDeclaration(context);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.StackTrace);
            }
        }
#else
        AnalyzeClassDeclaration
#endif
        , SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not ClassDeclarationSyntax cds)
            return;

        if (context.SemanticModel.GetDeclaredSymbol(cds, context.CancellationToken) is not INamedTypeSymbol classSymbol)
            return;

        if (HarmonyHelpers.GetHarmonyPatchType(context.Compilation, context.CancellationToken) is not { } harmonyAttribute)
            return;

        if (context.Compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1") is not { } IEnumerableTType)
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
                        HarmonyHelpers.GetHarmonyPatchTypeAttributeTypes(context.Compilation, context.CancellationToken)
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
                var methodData = new PatchMethodData(classSymbol, pair.m, context.Compilation)
                    .AddTargetMethodData(classAttributes)
                    .AddTargetMethodData(pair.attrs);

                if (HarmonyHelpers.TryParseHarmonyPatchType(pair.m.Name, out var methodNamePatchType))
                    methodData = methodData with { PatchType = methodNamePatchType };

                var maybeAttr = methodData.GetPatchTypeAttributes(context.Compilation, context.CancellationToken).TryFirst();

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
            if (context.CancellationToken.IsCancellationRequested)
                break;
#if DEBUG
            context.ReportAll(patchMethodData.CreateDiagnostics(DebugMessage, messageArgs: [patchMethodData]));
#endif
            diagnostics = diagnostics
                .AddRange(MissingPatchTypeAttribute.Check(patchMethodData))
                .AddRange(PatchTypeAttributeConflict.Check(patchMethodData, context.CancellationToken))
                .AddRange(InvalidPatchMethodReturnType.CheckPatchMethod(patchMethodData, context.CancellationToken))
                .AddRange(PaasthroughPostfixResultInjection.Check(patchMethodData))
                .AddRange(AssignmentToNonRefResultArgument.Check(
                    context.SemanticModel, patchMethodData, context.CancellationToken))
                .AddRange(InjectedParamterNotFoundOnTargetMethod.Check(patchMethodData, context.CancellationToken))
                .AddRange(PatchAttributeConflict.Check(patchMethodData, context.CancellationToken))
                .AddRange(InvalidInjectedParameterType.Check(patchMethodData))
                .AddRange(InvalidTranspilerParameter.Check(patchMethodData, context.CancellationToken))
                .AddRange(UseOutForPrefixStateInjection.Check(patchMethodData))
                .AddRange(ParameterIndexInjection.Check(patchMethodData))
                .AddRange(ReversePatchType.Check(patchMethodData, context.CancellationToken));
        }
#endregion

#region Rules for TargetMethod/TargetMethods
        bool isTargetMethod(IMethodSymbol m) =>
            m.Name is HarmonyConstants.TargetMethodMethodName ||
            m.GetAttributes().Any(attr => attr.AttributeClass is not null &&
                attr.AttributeClass.Equals(HarmonyHelpers.GetHarmonyTargetMethodType(context.Compilation, context.CancellationToken), SymbolEqualityComparer.Default));

        bool isTargetMethods(IMethodSymbol m) =>
            m.Name is HarmonyConstants.TargetMethodsMethodName ||
            m.GetAttributes().Any(attr => attr.AttributeClass is not null &&
                attr.AttributeClass.Equals(HarmonyHelpers.GetHarmonyTargetMethodsType(context.Compilation, context.CancellationToken), SymbolEqualityComparer.Default));

        var targetMethodMethods = classSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(isTargetMethod)
            .ToImmutableArray();

        var targetMethodsMethods = classSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(isTargetMethods)
            .ToImmutableArray();

        var MethodBaseType = context.Compilation.GetTypeByMetadataName(typeof(MethodBase).ToString());
        var IEnumerableMethodBaseType = MethodBaseType is { } mb ? IEnumerableTType?.Construct(mb) : null;

        var allPatchTargetMethodMembers = targetMethodMethods.Concat(targetMethodsMethods).ToImmutableArray();

        if (MethodBaseType is not null && IEnumerableMethodBaseType is not null && allPatchTargetMethodMembers.Count() > 0)
        {
            diagnostics = diagnostics
                .AddRange(MultipleTargetMethodDefinitions.Check(
                    classSymbol, classAttributes, patchMethodsData, allPatchTargetMethodMembers, context.CancellationToken));

            foreach (var m in targetMethodMethods)
            {
                diagnostics = diagnostics
                    .AddRange(InvalidPatchMethodReturnType.CheckTargetMethod(context.Compilation, m, MethodBaseType));
            }

            foreach (var m in targetMethodsMethods)
            {
                diagnostics = diagnostics
                    .AddRange(InvalidPatchMethodReturnType.CheckTargetMethods(context.Compilation, m, IEnumerableMethodBaseType));
            }
        }
#endregion
        
#region Rules for target method resolution
        else
        {
            foreach (var patchMethodData in patchMethodsData)
            {
                if (context.CancellationToken.IsCancellationRequested)
                    break;

                if (patchMethodData.TargetMethod is not null)
                    continue;


                var missingMethodTypes = MissingMethodType.Check(patchMethodData, context.CancellationToken);
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

            }
        }
#endregion

        foreach (var diagnostic in diagnostics)
        {
            if (context.CancellationToken.IsCancellationRequested)
                return;

            context.ReportDiagnostic(diagnostic);
        }
    }
}
