using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;

namespace MicroUtils.HarmonyAnalyzers.Rules;

using static DiagnosticId;

internal static class UseOutForPrefixStateInjection
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        nameof(MHA016),
        "Use out modifier for __state parameter in prefix",
        "Use out modifier for __state parameter",
        nameof(RuleCategory.PatchMethod),
        DiagnosticSeverity.Info,
        true);

    internal static ImmutableArray<Diagnostic> Check(PatchMethodData methodData)
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
