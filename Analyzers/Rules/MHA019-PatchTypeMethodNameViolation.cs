using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

using Microsoft.CodeAnalysis;

namespace MicroUtils.HarmonyAnalyzers.Rules;

using static DiagnosticId;
using static MicroUtils.HarmonyAnalyzers.HarmonyConstants;

internal readonly struct PatchTypeMethodNameViolation : IPatchMethodRule
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        nameof(MHA019),
        "Method name casing violation (patch type)",
        "Patch type method name should match patch type {0}",
        nameof(RuleCategory.PatchMethod),
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: ReferenceDoc.GetUriString(MHA019).ValueOrDefault());

    DiagnosticDescriptor IPatchRule.Descriptor => Descriptor;

    public ImmutableArray<Diagnostic> Check(PatchMethodData patchMethodData, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        bool IncorrectNameCase(HarmonyPatchType patchType)
        {
            var patchMethodName = patchMethodData.PatchMethod.Name;
            var patchTypeName = patchType.GetEnumValueName();
            return patchTypeName != patchMethodName &&
                patchMethodName.ToLowerInvariant() == patchTypeName.ToLowerInvariant();
        }

        if (patchMethodData.PatchType is { } patchType && IncorrectNameCase(patchType))
            return patchMethodData.CreateDiagnostics(Descriptor, messageArgs: [patchType.GetEnumValueName()]);

        if (Util.GetEnumValues<HarmonyPatchType>()
            .TryPick(pt => IncorrectNameCase(pt) ? Optional.Value(pt) : default) is { HasValue: true } matchingPatchType)
        {
            var patchTypeName = matchingPatchType.Value.GetEnumValueName();

            return patchMethodData.CreateDiagnostics(
                Descriptor,
                additionalProperties: dict => dict.SetItem(nameof(PatchMethodData.PatchType), patchTypeName),
                messageArgs: [patchTypeName]);
        }

        return [];
    }
}
