using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;

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
        isEnabledByDefault: true);

    DiagnosticDescriptor IPatchRule.Descriptor => Descriptor;

    public ImmutableArray<Diagnostic> Check(
        PatchMethodData methodData,
        SemanticModel _1,
        CancellationToken _2)
    {
        if (methodData.PatchType is not HarmonyConstants.HarmonyPatchType.Prefix)
            return [];

        if (methodData.PatchMethod.Parameters.FirstOrDefault(p => p.Name == "__state") is { } stateParam &&
            stateParam.RefKind is not RefKind.Out or RefKind.Ref)
        {
            var severity = stateParam.RefKind is RefKind.Ref ? DiagnosticSeverity.Info : DiagnosticSeverity.Warning;

            return methodData.CreateDiagnostics(Descriptor, primaryLocations: stateParam.Locations, severity: severity);
        }

        return [];
    }
}
