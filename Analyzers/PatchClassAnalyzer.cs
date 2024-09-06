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
        PatchMethodParamterNotFoundOnTargetMethod.Descriptor,
        PatchAttributeConflict.Descriptor
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(snContext => AnalyzeClassDeclaration(snContext, context), SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context, AnalysisContext analContext)
    {
        if (context.Node is not ClassDeclarationSyntax cds)
            return;

        if (context.SemanticModel.GetDeclaredSymbol(cds, context.CancellationToken) is not INamedTypeSymbol classSymbol)
            return;

        if (HarmonyHelpers.GetHarmonyPatchType(context.Compilation, context.CancellationToken) is not { } harmonyAttribute)
            return;

        if (context.Compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1") is not { } IEnumerableTType)
            return;

        var patchTypeAttributeTypesMap =
            HarmonyHelpers.GetHarmonyPatchTypeAttributeTypes(context.Compilation, context.CancellationToken).ToImmutableArray();
        
        var patchTypeAttributeTypes = patchTypeAttributeTypesMap.Select(pair => pair.Item2).ToImmutableArray();

        var classAttributes = classSymbol.GetAttributes()
            .Where(attr => attr.AttributeClass?.Equals(harmonyAttribute, SymbolEqualityComparer.Default) ?? false)
            .ToImmutableArray();

        var patchMethods = classSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Select(m =>
            {
                var attrs = m.GetAttributes()
                    .Where(attr => attr.AttributeClass is { } type && type.Equals(harmonyAttribute, SymbolEqualityComparer.Default))
                    .ToImmutableArray();

                return (m, attrs);
            })
            .Where(static pair => pair.attrs.Length > 0 || HarmonyConstants.HarmonyPatchTypeNames.Contains(pair.m.Name))
            .ToImmutableArray();
        
        if (classAttributes.Length == 0 && patchMethods.Length == 0)
            return;

        var diagnostics = ImmutableArray<Diagnostic>.Empty;

        var patchMethodsData = patchMethods
            .Select(pair =>
            {
                var methodData = new PatchMethodData(classSymbol, pair.m)
                    .AddTargetMethodData(classAttributes)
                    .AddTargetMethodData(pair.attrs);

                if (Enum.TryParse<HarmonyConstants.HarmonyPatchType>(pair.m.Name, out var methodNamePatchType))
                    methodData = methodData with { PatchType = methodNamePatchType };

                foreach (var (pt, attributeType) in patchTypeAttributeTypesMap)
                {
                    if (pair.m.GetAttributes()
                            .Select(attr => attr.AttributeClass)
                            .Contains(attributeType, SymbolEqualityComparer.Default))
                    {
                        var conflicts = PatchTypeAttributeConflict
                            .Check(context.Compilation, methodData, attributeType, context.CancellationToken)
                            .ToImmutableArray();

                        if (conflicts.Length < 1)
                        {
                            methodData = methodData with { PatchType = pt };

                            diagnostics = diagnostics.AddRange(conflicts);

                        }
                    }
                }

                diagnostics = diagnostics.AddRange(MissingPatchTypeAttribute.Check(methodData));

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
            //context.ReportDiagnostic(Diagnostic.Create(
            //    descriptor: DebugMessage,
            //    location: patchMethodData.PatchMethod.Locations[0],
            //    messageArgs: patchMethodData));
#endif
            diagnostics = diagnostics
                .AddRange(InvalidPatchMethodReturnType.CheckPatchMethod(
                    context.Compilation, patchMethodData, IEnumerableTType, context.CancellationToken))
                .AddRange(PaasthroughPostfixResultInjection.Check(patchMethodData))
                .AddRange(AssignmentToNonRefResultArgument.Check(
                    context.SemanticModel, patchMethodData, context.CancellationToken))
                .AddRange(PatchMethodParamterNotFoundOnTargetMethod.Check(
                    context.Compilation, patchMethodData, context.CancellationToken))
                .AddRange(PatchAttributeConflict.Check(patchMethodData, context.CancellationToken));

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

                diagnostics = diagnostics.AddRange(patchMethodData.CreateDiagnostics(TargetMethodMatchFailed.Descriptor));

                //context.ReportDiagnostic(Diagnostic.Create(
                //    descriptor: TargetMethodMatchFailed.Descriptor,
                //    location: patchMethodData.PatchMethod.Locations[0],
                //    additionalLocations: patchMethodData.PatchMethod.Locations.Skip(1)));
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
