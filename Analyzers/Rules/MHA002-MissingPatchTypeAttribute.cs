using System.Collections.Immutable;
using System.Threading;

using Microsoft.CodeAnalysis;

namespace MicroUtils.HarmonyAnalyzers.Rules;

using static DiagnosticId;

internal readonly struct MissingPatchTypeAttribute : IPatchMethodRule
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        nameof(MHA002),
        $"Missing Harmony patch type method attribute",
        "Patch method requires a Harmony patch type attribute",
        nameof(RuleCategory.PatchAttribute),
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: ReferenceDoc.GetUriString(MHA002).ValueOrDefault());

    DiagnosticDescriptor IPatchRule.Descriptor => Descriptor;

    public ImmutableArray<Diagnostic> Check(
        PatchMethodData methodData,
        SemanticModel _1,
        CancellationToken _2)
    {
        if (methodData.PatchType is null)
        {
            return methodData.CreateDiagnostics(Descriptor);
        }

        return [];
    }
}
