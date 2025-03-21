using System;
using System.Collections.Immutable;
using System.Linq;
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
        ReversePatchType.Descriptor,
        PatchTypeMethodNameViolation.Descriptor,
        NonStaticPatchMethod.Descriptor,
    ];

    delegate ImmutableArray<Diagnostic> PatchClassRuleCheck(
        PatchClassData patchClassData,
        CancellationToken cancellationToken);

    static readonly ImmutableArray<PatchClassRuleCheck> patchClassChecks =
    [
        PatchRule.Check<MissingClassAttribute>,
        PatchRule.Check<NoPatchMethods>,
    ];

    static readonly ImmutableArray<PatchClassRuleCheck> targetMethodChecks =
    [
        PatchRule.Check<MultipleTargetMethodDefinitions>,
        PatchRule.Check<InvalidPatchMethodReturnType.TargetMethod>,
        PatchRule.Check<InvalidPatchMethodReturnType.TargetMethods>
    ];

    delegate ImmutableArray<Diagnostic> PatchMethodRuleCheck(
        PatchMethodData patchMethodData,
        SemanticModel semanticModel,
        CancellationToken cancellationToken);

    static readonly ImmutableArray<PatchMethodRuleCheck> patchMethodChecks =
    [
        PatchRule.Check<MissingPatchTypeAttribute>,
        PatchRule.Check<PatchTypeAttributeConflict>,
        PatchRule.Check<InvalidPatchMethodReturnType.PatchMethod>,
        PatchRule.Check<PaasthroughPostfixResultInjection>,
        PatchRule.Check<AssignmentToNonRefResultArgument>,
        PatchRule.Check<InjectedParamterNotFoundOnTargetMethod>,
        PatchRule.Check<PatchAttributeConflict>,
        PatchRule.Check<InvalidInjectedParameterType>,
        PatchRule.Check<InvalidTranspilerParameter>,
        PatchRule.Check<UseOutForPrefixStateInjection>,
        PatchRule.Check<ParameterIndexInjection>,
        PatchRule.Check<ReversePatchType>,
        PatchRule.Check<PatchTypeMethodNameViolation>,
        PatchRule.Check<NonStaticPatchMethod>,
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(context =>
        {
            // Notes: The analyzer needs to see non-public members and types from external references.
            // However, the provided compilation and semantic model cannot see non-public types or members
            // (even with if you create a new SemanticModel with ignoreAccessibility = true)
            // Workaround: create a single new compilation on Compilation start and share the new SemanticModel
            // between analysis contexts where possible
            var compilation = context.Compilation.WithAllMembers();

#pragma warning disable RS1030 // Do not invoke Compilation.GetSemanticModel() method within a diagnostic analyzer
            SemanticModel GetSemanticModel(SyntaxTree tree) => compilation.GetSemanticModel(tree, true);
#pragma warning restore RS1030 // Do not invoke Compilation.GetSemanticModel() method within a diagnostic analyzer

            SemanticModel GetCachedSemanticModel(SyntaxTree tree) =>
                context.TryGetValue<SemanticModel>(tree, new(GetSemanticModel), out var sm) ?
                    sm : GetSemanticModel(tree);

            context.RegisterSyntaxNodeAction(
                snContext =>
                {
                    AnalyzeClassDeclaration(
                        snContext.Node,
                        compilation,
                        GetCachedSemanticModel,
                        report => snContext.ReportDiagnostic(report),
                        snContext.CancellationToken);
                }, SyntaxKind.ClassDeclaration);
        });
    }

    private static void AnalyzeClassDeclaration(
        SyntaxNode node,
        Compilation compilation,
        Func<SyntaxTree, SemanticModel> getSemanticModel,
        Action<Diagnostic> report,
        CancellationToken ct)
    {
        if (node is not ClassDeclarationSyntax cds ||
            getSemanticModel(cds.SyntaxTree) is not { } sm ||
            sm.GetDeclaredSymbol(cds, ct) is not INamedTypeSymbol classSymbol)
            return;

        if (CommonSymbols.Get(compilation, ct) is not { } commonSymbols)
            return;

        var harmonyAttribute = commonSymbols.HarmonyPatchAttribute;

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
                        commonSymbols.HarmonyPatchTypeAttributes.Values
                            .Any(at => at.Equals(type, SymbolEqualityComparer.Default))
                        ))
                    .ToImmutableArray();

                return (m, attrs);
            })
            .Where(pair => pair.attrs.Length > 0 ||
                (classAttributes.Length > 0 && HarmonyConstants.HarmonyPatchTypeNames.Contains(pair.m.Name)))
            .ToImmutableArray();
        
        if (classAttributes.Length == 0 && patchMethods.Length == 0)
            return;

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

        var patchClassData = new PatchClassData(classSymbol, classAttributes, patchMethodsData, compilation, commonSymbols);

#region Rules for patch class
        foreach (var diagnostic in patchClassChecks.SelectMany(check => check(patchClassData, ct)))
        {
            if (ct.IsCancellationRequested)
                break;

            report(diagnostic);
        }
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
            foreach (var diagnostic in patchMethodChecks.SelectMany(check => check(patchMethodData, sm, ct)))
            {
                if (ct.IsCancellationRequested)
                    break;

                report(diagnostic);
            }
        }
#endregion

        var diagnostics = ImmutableArray<Diagnostic>.Empty;

#region Rules for TargetMethod/TargetMethods
        if (patchClassData.TargetMethodMethods.Value.Concat(patchClassData.TargetMethodsMethods.Value).Count() > 0)
        {
            foreach (var diagnostic in targetMethodChecks.SelectMany(check => check(patchClassData, ct)))
            {
                if (ct.IsCancellationRequested)
                    break;

                report(diagnostic);
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

                var missingMethodTypes = PatchRule.Check<MissingMethodType>(patchMethodData, sm, ct);
                if (missingMethodTypes.Length > 0)
                {
                    diagnostics = diagnostics.AddRange(missingMethodTypes);

                    continue;
                }

                var ambiguous = PatchRule.Check<AmbiguousMatch>(patchMethodData, sm, ct);
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
                    messageArgs: [String.Join(", ", patchMethodData.TargetType?.MemberNames ?? [])]));
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
