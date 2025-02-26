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

internal readonly struct PatchAttributeConflict : IPatchMethodRule
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        nameof(MHA010),
        "HarmonyPatch attributes conflict",
        "Conflicting patch attributes: {0}",
        nameof(RuleCategory.PatchAttribute),
        DiagnosticSeverity.Warning,
        true);

    DiagnosticDescriptor IPatchRule.Descriptor => Descriptor;

    private static IEnumerable<Diagnostic> CheckInternal(
        PatchMethodData methodData,
        CancellationToken ct)
    {
        var conflicts = methodData.GetConflicts(ct).ToImmutableArray();

        if (conflicts.Length < 1)
            yield break;

        foreach (var c in conflicts)
        {
            var locations = (c.ApplicationSyntaxReference?.GetSyntax().GetLocation())
                .EmptyIfNull()
                .ToImmutableArray();

            foreach (var d in methodData.CreateDiagnostics(Descriptor, locations, messageArgs: [string.Join(", ", conflicts)]))
                yield return d;
        }
    }

    public ImmutableArray<Diagnostic> Check(
        PatchMethodData methodData,
        SemanticModel _1,
        CancellationToken ct) => CheckInternal(methodData, ct).ToImmutableArray();
}
