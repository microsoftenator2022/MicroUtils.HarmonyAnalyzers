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
        AssignmentToNonRefResultArgument.Descriptor
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

        if (HarmonyTypeHelpers.GetHarmonyPatchType(context.Compilation, context.CancellationToken) is not { } harmonyAttribute)
            return;

        if (context.Compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1") is not { } IEnumerableTType)
            return;

        var patchTypeAttributeTypes =
            HarmonyTypeHelpers.GetHarmonyPatchMethodAttributeTypes(context.Compilation, context.CancellationToken).ToImmutableArray();

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
            .Where(static pair => pair.attrs.Length > 0 || Constants.HarmonyPatchTypeNames.Contains(pair.m.Name))
            .ToImmutableArray();
        
        if (classAttributes.Length == 0 && patchMethods.Length == 0)
            return;

        var patchMethodsData = patchMethods
            .Select(pair =>
            {
                var methodData = new PatchMethodData(classSymbol, pair.m)
                    .AddTargetMethodData(classAttributes)
                    .AddTargetMethodData(pair.attrs);

                if (Enum.TryParse<Constants.HarmonyPatchType>(pair.m.Name, out var methodNamePatchType))
                    methodData = methodData with { PatchType = methodNamePatchType };

                foreach (var pt in Enum.GetValues(typeof(Constants.HarmonyPatchType)).Cast<Constants.HarmonyPatchType>())
                {
                    var attributeType = pt.GetPatchTypeAttributeType(context.Compilation, context.CancellationToken);

                    if (attributeType is not null &&
                        pair.m.GetAttributes()
                            .Select(attr => attr.AttributeClass)
                            .Contains(attributeType, SymbolEqualityComparer.Default))
                    {
                        if (!PatchTypeAttributeConflict.Check(context, methodData, attributeType))

                            methodData = methodData with { PatchType = pt };
                    }
                }

                MissingPatchTypeAttribute.Check(context, methodData, patchTypeAttributeTypes);

                return methodData;
            })
            .ToImmutableArray();

        // Rules for patch class
        MissingClassAttribute.Check(context, classSymbol, classAttributes, patchMethodsData, harmonyAttribute);
        NoPatchMethods.Check(context, classSymbol, classAttributes, patchMethodsData);

        // General patch method rules
        foreach (var patchMethodData in patchMethodsData)
        {
            if (context.CancellationToken.IsCancellationRequested)
                break;
#if DEBUG
            context.ReportDiagnostic(Diagnostic.Create(
                descriptor: DebugMessage,
                location: patchMethodData.PatchMethod.Locations[0],
                messageArgs: patchMethodData));
#endif
            InvalidPatchMethodReturnType.CheckPatchMethod(context, patchMethodData, IEnumerableTType);
            PaasthroughPostfixResultInjection.Check(context, patchMethodData);
            AssignmentToNonRefResultArgument.Check(context, patchMethodData);
        }

        bool isTargetMethodMethod(IMethodSymbol m) =>
            m.Name is Constants.TargetMethodMethodName ||
            m.GetAttributes().Any(attr => attr.AttributeClass is not null &&
                attr.AttributeClass.Equals(HarmonyTypeHelpers.GetHarmonyTargetMethodType(context.Compilation, context.CancellationToken), SymbolEqualityComparer.Default));

        bool isTargetMethodsMethod(IMethodSymbol m) =>
            m.Name is Constants.TargetMethodsMethodName ||
            m.GetAttributes().Any(attr => attr.AttributeClass is not null &&
                attr.AttributeClass.Equals(HarmonyTypeHelpers.GetHarmonyTargetMethodsType(context.Compilation, context.CancellationToken), SymbolEqualityComparer.Default));

        var targetMethodMethods = classSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(isTargetMethodMethod)
            .ToImmutableArray();

        var targetMethodsMethods = classSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(isTargetMethodsMethod)
            .ToImmutableArray();

        var MethodBaseType = context.Compilation.GetTypeByMetadataName(typeof(MethodBase).ToString());
        var IEnumerableMethodBaseType = MethodBaseType is { } mb ? IEnumerableTType?.Construct(mb) : null;

        // Rules for TargetMethod/TargetMethods
        if (MethodBaseType is not null && IEnumerableMethodBaseType is not null &&
            targetMethodMethods.Concat(targetMethodsMethods).Count() > 0)
        {
            MultipleTargetMethodDefinitions.Check(context, classSymbol, classAttributes, patchMethodsData, targetMethodMethods);

            foreach (var m in targetMethodMethods)
            {
                InvalidPatchMethodReturnType.CheckTargetMethod(context, m, MethodBaseType);
            }

            foreach (var m in targetMethodsMethods)
            {
                InvalidPatchMethodReturnType.CheckTargetMethods(context, m, IEnumerableMethodBaseType);
            }

            return;
        }

        // Rules for target method resolution
        foreach (var patchMethodData in patchMethodsData)
        {
            if (context.CancellationToken.IsCancellationRequested)
                break;

            if (patchMethodData.TargetMethod is not null)
                continue;

            if (MissingMethodType.Check(context, patchMethodData))
                continue;

            if (AmbiguousMatch.Check(context, patchMethodData))
                continue;

            context.ReportDiagnostic(Diagnostic.Create(
                descriptor: TargetMethodMatchFailed.Descriptor,
                location: patchMethodData.PatchMethod.Locations[0],
                additionalLocations: patchMethodData.PatchMethod.Locations.Skip(1)));
        }
    }
}
