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

internal readonly struct AmbiguousMatch : IPatchMethodRule
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        nameof(MHA004),
        "Ambiguous match for HarmonyPatch target method",
        "Ambiguous target method for patch. Candidate methods: {0}.",
        nameof(RuleCategory.TargetMethod),
        DiagnosticSeverity.Warning,
        true);

    DiagnosticDescriptor IPatchRule.Descriptor => Descriptor;

    public ImmutableArray<Diagnostic> Check(
        PatchMethodData patchMethodData,
        SemanticModel _0,
        CancellationToken _1)
    {
        if (!patchMethodData.IsAmbiguousMatch)
            return [];

        return patchMethodData.CreateDiagnostics(
            descriptor: Descriptor,
            messageArgs: [string.Join(", ", patchMethodData.GetCandidateMethods())]);
    }
}
