using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MicroUtils.HarmonyAnalyzers.Rules;

using static DiagnosticId;

internal readonly struct PaasthroughPostfixResultInjection : IPatchMethodRule
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        nameof(MHA012),
        "__result injection in passthrough Postfix",
        "Unnecessary injected __result parameter in passthrough postfix",
        nameof(RuleCategory.PatchMethod),
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    DiagnosticDescriptor IPatchRule.Descriptor => Descriptor;

    public ImmutableArray<Diagnostic> Check(
        PatchMethodData methodData,
        SemanticModel _1,
        CancellationToken _2)
    {
        if (methodData.PatchType is not HarmonyConstants.HarmonyPatchType.Postfix)
            return [];

        if (!methodData.PatchMethod.ReturnsVoid &&
            methodData.PatchMethod.Parameters.Skip(1).FirstOrDefault(p => p.Name == HarmonyConstants.Parameter_injection__result) is { } p)
        {
            if (p.RefKind is RefKind.Ref or RefKind.Out)
                return methodData.CreateDiagnostics(Descriptor, primaryLocations: p.Locations, severity: DiagnosticSeverity.Warning);

            return methodData.CreateDiagnostics(Descriptor, primaryLocations: p.Locations);
        }

        return [];
    }
}
