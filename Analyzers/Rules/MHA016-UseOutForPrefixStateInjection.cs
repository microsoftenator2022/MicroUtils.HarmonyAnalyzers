using System.Collections.Immutable;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MicroUtils.HarmonyAnalyzers.Rules;

using static DiagnosticId;

internal readonly struct UseOutForPrefixStateInjection : IPatchMethodRule
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        nameof(MHA016),
        "Use out modifier for __state parameter in prefix",
        "Use out modifier for __state parameter",
        nameof(RuleCategory.PatchMethod),
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: ReferenceDoc.GetUriString(MHA016).ValueOrDefault());

    DiagnosticDescriptor IPatchRule.Descriptor => Descriptor;

    public ImmutableArray<Diagnostic> Check(
        PatchMethodData methodData,
        SemanticModel _1,
        CancellationToken ct)
    {
        if (methodData.PatchType is not HarmonyConstants.HarmonyPatchType.Prefix)
            return [];

        if (methodData.PatchMethod.Parameters.FirstOrDefault(p => p.Name == "__state") is { } stateParam &&
            stateParam.RefKind is not RefKind.Out)
        {
            var severity = stateParam.RefKind is RefKind.Ref ? DiagnosticSeverity.Info : DiagnosticSeverity.Warning;
            
            ImmutableArray<Location> locations = default;

            if (stateParam.RefKind is RefKind.Ref)
                locations = stateParam.DeclaringSyntaxReferences
                    .Select(sr => sr.GetSyntax(ct))
                    .OfType<ParameterSyntax>()
                    .Choose(n => n.Modifiers.TryFirst(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.RefKeyword)))
                    .Select(n => n.GetLocation())
                    .ToImmutableArray();

            if (locations.IsDefaultOrEmpty)
                locations = stateParam.Locations;

            return methodData.CreateDiagnostics(Descriptor, primaryLocations: locations, severity: severity);
        }

        return [];
    }
}
