using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MicroUtils.HarmonyAnalyzers.Rules;
internal class MissingPatchTypeAttribute
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        "MHA002",
        $"Missing Harmony patch type method attribute",
        "'{0}' is missing a Harmony patch type attribute (" + string.Join(", ", Constants.HarmonyPatchTypeAttributeNames) + ")",
        nameof(Constants.RuleCategory.PatchAttribute),
        DiagnosticSeverity.Warning,
        true);

    internal static void Check(
        SyntaxNodeAnalysisContext context,
        IMethodSymbol method,
        ImmutableArray<INamedTypeSymbol> patchTypeAttributeTypes,
        ImmutableArray<Location> locations)
    {
        if (!Constants.HarmonyPatchTypeNames.Contains(method.Name) &&
            !method.GetAttributes().Select(attr => attr.AttributeClass)
                .ContainsAny(patchTypeAttributeTypes, SymbolEqualityComparer.Default))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                descriptor: Descriptor,
                location: locations[0],
                additionalLocations: locations.Skip(1),
                messageArgs: method));
        }
    }
}
