using System;
using System.Collections.Immutable;
using System.Threading;

using Microsoft.CodeAnalysis;

namespace MicroUtils.HarmonyAnalyzers.Rules;

using static DiagnosticId;

internal readonly struct NonStaticPatchMethod : IPatchMethodRule
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        nameof(MHA020),
        "Non-static patch method",
        "Patch method {0} should be static",
        nameof(RuleCategory.PatchMethod),
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: ReferenceDoc.GetUriString(MHA020).ValueOrDefault());

    DiagnosticDescriptor IPatchRule.Descriptor => Descriptor;

    public ImmutableArray<Diagnostic> Check(
        PatchMethodData patchMethodData,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        if (!patchMethodData.PatchMethod.IsStatic)
            return patchMethodData.CreateDiagnostics(Descriptor, messageArgs: [patchMethodData.PatchMethod]);

        return [];
    }
}
