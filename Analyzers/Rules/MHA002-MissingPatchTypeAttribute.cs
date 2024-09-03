using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MicroUtils.HarmonyAnalyzers.Rules;

using static DiagnosticId;

internal class MissingPatchTypeAttribute
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        nameof(MHA002),
        $"Missing Harmony patch type method attribute",
        "Patch method requires a Harmony patch type attribute",
        nameof(RuleCategory.PatchAttribute),
        DiagnosticSeverity.Warning,
        true);

    internal static void Check(
        SyntaxNodeAnalysisContext context,
        PatchMethodData methodData,
        ImmutableArray<INamedTypeSymbol> patchTypeAttributeTypes)
    {
        if (methodData.PatchType is null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                descriptor: Descriptor,
                location: methodData.PatchMethod.Locations[0],
                additionalLocations: methodData.PatchMethod.Locations.Skip(1),
                properties: ImmutableDictionary<string, string?>.Empty
                    .Add("TargetType", methodData.TargetType?.GetFullMetadataName())
                    .Add("TargetMethod", methodData.TargetMethod?.MetadataName)));
        }
    }
}
