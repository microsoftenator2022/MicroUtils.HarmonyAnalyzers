using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MicroUtils.HarmonyAnalyzers.Rules;
internal static class MultipleTargetMethodDefinitions
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        "MHA007",
        "Conflicting target method definitions in patch class",
        "Patch class has more than one of: TargetMethod, TargetMethods, parametrized HarmonyPatch attributes",
        nameof(Constants.RuleCategory.TargetMethod),
        DiagnosticSeverity.Warning,
        true);

    internal static void Check(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol classSymbol,
        ImmutableArray<AttributeData> classAttributes,
        ImmutableArray<PatchMethodData> patchMethods,
        ImmutableArray<IMethodSymbol> targetMethodMethods)
    {
        var targetMethodMethodLocations =
                targetMethodMethods
                    .SelectMany(m => m.Locations);

        var allPatchAttributes = classAttributes.Concat(patchMethods.SelectMany(patchMethodData => patchMethodData.HarmonyPatchAttributes));

        var allTargetMethodLocations =
            targetMethodMethodLocations
                .Concat(allPatchAttributes
                    .Where(attr => attr.ConstructorArguments.Length > 0)
                    .Select(attr => attr.ApplicationSyntaxReference)
                    .NotNull()
                    .Select(s => s.GetSyntax().GetLocation()))
                .ToImmutableArray();

        if (allTargetMethodLocations.Length > 1)
        {
            void report(IEnumerable<Location> locations) =>
                context.ReportDiagnostic(Diagnostic.Create(
                    descriptor: Descriptor,
                    location: locations.First(),
                    additionalLocations: locations.Skip(1)));

            report(classSymbol.Locations);

            for (var i = 0; i < allTargetMethodLocations.Length; i++)
            {
                if (context.CancellationToken.IsCancellationRequested)
                    return;

                report([allTargetMethodLocations[i]]);
            }
        }
    }
}
