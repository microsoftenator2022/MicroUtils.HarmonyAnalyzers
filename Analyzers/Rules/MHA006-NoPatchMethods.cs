using System.Collections.Immutable;
using System.Threading;

using Microsoft.CodeAnalysis;

namespace MicroUtils.HarmonyAnalyzers.Rules;

using static DiagnosticId;

internal readonly struct NoPatchMethods : IPatchClassRule
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        nameof(MHA006),
        "No patch methods in patch class",
        "Patch class contains no patch methods",
        nameof(RuleCategory.PatchMethod),
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: ReferenceDoc.GetUriString(MHA006).ValueOrDefault());

    DiagnosticDescriptor IPatchRule.Descriptor => Descriptor;

    public ImmutableArray<Diagnostic> Check(
        PatchClassData patchClassData,
        CancellationToken _)
    {
        if (patchClassData.ClassAttributes.Length > 0 && patchClassData.PatchMethods.Length == 0)
        { 
            return new DiagnosticBuilder(Descriptor).ForAllLocations(patchClassData.ClassSymbol.Locations).CreateAll().ToImmutableArray();
        }

        return [];
    }
}
