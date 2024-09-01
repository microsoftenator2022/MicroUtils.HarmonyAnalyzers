using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MicroUtils.HarmonyAnalyzers.Rules;

internal static class PatchAttributeConflict
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        "MHA010",
        "HarmonyPatch attributes conflict",
        "Conflicting patch attributes: {0}",
        nameof(Constants.RuleCategory.PatchAttribute),
        DiagnosticSeverity.Warning,
        true);

    internal static void Check(
        SyntaxNodeAnalysisContext context,
        PatchMethodData methodData)
    {
        var conflicts = methodData.GetConflicts(context.CancellationToken).ToImmutableArray();

        if (conflicts.Length < 1)
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            descriptor: Descriptor,
            location: methodData.PatchMethod.Locations[0],
            additionalLocations: methodData.PatchMethod.Locations.Skip(1),
            messageArgs: [string.Join(", ", conflicts)]));
    }
}
