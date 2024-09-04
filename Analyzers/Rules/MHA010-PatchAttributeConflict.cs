using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MicroUtils.HarmonyAnalyzers.Rules;

using static DiagnosticId;

internal static class PatchAttributeConflict
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        nameof(MHA010),
        "HarmonyPatch attributes conflict",
        "Conflicting patch attributes: {0}",
        nameof(RuleCategory.PatchAttribute),
        DiagnosticSeverity.Warning,
        true);

    internal static void Check(
        SyntaxNodeAnalysisContext context,
        PatchMethodData methodData)
    {
        var conflicts = methodData.GetConflicts(context.CancellationToken).ToImmutableArray();

        if (conflicts.Length < 1)
            return;

        foreach (var c in conflicts)
        {
            context.ReportDiagnostic(methodData.CreateDiagnostic(
            descriptor: Descriptor,
            locations: ((IEnumerable<Location?>)[c.ApplicationSyntaxReference?.GetSyntax().GetLocation()])
                .Concat([methodData.PatchMethod.Locations[0]]).NotNull().ToImmutableArray(),
            messageArgs: [string.Join(", ", conflicts)]));
        }
    }
}
