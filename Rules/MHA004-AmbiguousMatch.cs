using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MicroUtils.HarmonyAnalyzers.Rules;

using static DiagnosticId;

internal static class AmbiguousMatch
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        nameof(MHA004),
        "Ambiguous match for HarmonyPatch target method",
        "Ambiguous target method for patch. Candidate methods: {0}.",
        nameof(RuleCategory.TargetMethod),
        DiagnosticSeverity.Warning,
        true);

    internal static bool Check(
        SyntaxNodeAnalysisContext context,
        PatchMethodData patchMethodData)
    {
        if (!patchMethodData.IsAmbiguousMatch)
            return false;

        context.ReportDiagnostic(Diagnostic.Create(
            descriptor: Descriptor,
            location: patchMethodData.PatchMethod.Locations[0],
            additionalLocations: patchMethodData.PatchMethod.Locations.Skip(1),
            messageArgs: [string.Join(", ", patchMethodData.GetCandidateMethods())]));

        return true;
    }
}
