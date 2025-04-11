using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;

namespace MicroUtils.HarmonyAnalyzers.Rules;

using static DiagnosticId;

internal readonly struct PatchTypeAttributeConflict : IPatchMethodRule
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        nameof(MHA011),
        "Patch type conflict",
        "Patch type attribute conflict",
        nameof(RuleCategory.PatchMethod),
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: ReferenceDoc.GetUriString(MHA011).ValueOrDefault());

    DiagnosticDescriptor IPatchRule.Descriptor => Descriptor;

    internal static IEnumerable<Location> CheckInternal(
        PatchMethodData methodData,
        CancellationToken ct)
    {
        if (methodData.PatchType is not null)
            return [];

        HarmonyConstants.HarmonyPatchType? methodNamePatchType = null;
        
        if (HarmonyHelpers.TryParseHarmonyPatchType(methodData.PatchMethod.Name, out var patchType))
            methodNamePatchType = patchType;

        var patchTypeAttributes =
            methodData.GetPatchTypeAttributes(methodData.Compilation, ct)
                .ToImmutableArray();

        if (patchTypeAttributes.Length == 0)
            return [];

        if (patchTypeAttributes.Length == 1 &&
            methodNamePatchType is not null &&
            patchTypeAttributes[0].Item2 == methodNamePatchType)
                return [];

        return patchTypeAttributes
            .Select(pair => pair.Item1.ApplicationSyntaxReference?.GetSyntax().GetLocation())
            .Choose(Optional.MaybeValue)
            .Concat(methodNamePatchType is not null ? methodData.PatchMethod.Locations : []);
    }

    public ImmutableArray<Diagnostic> Check(
        PatchMethodData methodData,
        SemanticModel _1,
        CancellationToken ct)
    {
        var conflictLocations = CheckInternal(methodData, ct).ToImmutableArray();
        if (conflictLocations.Length > 0)
            return methodData.CreateDiagnostics(Descriptor, conflictLocations);

        return [];
    }
}
