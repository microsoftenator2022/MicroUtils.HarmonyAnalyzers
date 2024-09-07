using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MicroUtils.HarmonyAnalyzers.Rules;

using static DiagnosticId;

internal static class PaasthroughPostfixResultInjection
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        nameof(MHA012),
        "__result injection in passthrough Postfix",
        "Unnecessary injected __result parameter in passthrough postfix",
        nameof(RuleCategory.PatchMethod),
        DiagnosticSeverity.Info,
        true);

    internal static ImmutableArray<Diagnostic> Check(
        PatchMethodData methodData)
    {
        if (methodData.PatchType is not HarmonyConstants.HarmonyPatchType.Postfix)
            return [];

        if (!methodData.PatchMethod.ReturnsVoid &&
            methodData.PatchMethod.Parameters.Skip(1).FirstOrDefault(p => p.Name == HarmonyConstants.Parameter_injection__result) is { } p)
        {
            return methodData.CreateDiagnostics(Descriptor, primaryLocations: p.Locations);
        }

        return [];
    }
}
