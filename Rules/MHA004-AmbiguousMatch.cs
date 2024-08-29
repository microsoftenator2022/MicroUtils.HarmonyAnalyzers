using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MicroUtils.HarmonyAnalyzers.Rules;
internal static class AmbiguousMatch
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        "MHA004",
        "Ambiguous match for HarmonyPatch target method",
        "Ambiguous match for target of patch method '{0}'. Candidate methods: {1}.",
        nameof(Constants.RuleCategory.TargetMethod),
        DiagnosticSeverity.Warning,
        true);

    internal static bool Check(
        SyntaxNodeAnalysisContext context,
        PatchMethodData patchMethodData,
        ImmutableArray<Location> locations)
    {
        if (!patchMethodData.IsAmbiguousMatch)
            return false;

        context.ReportDiagnostic(Diagnostic.Create(
            descriptor: Descriptor,
            location: locations[0],
            additionalLocations: locations.Skip(1),
            messageArgs: [patchMethodData.PatchMethod, string.Join(", ", patchMethodData.GetCandidateMethods())]));

        return true;
    }
}
