using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HarmonyAnalyzers.Rules;
internal static class MultipleTargetMethodDefinitions
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        "MHA007",
        "Multiple target method definition in patch class",
        "Patch class {0} has more than one of: TargetMethod, TargetMethods, parametrized HarmonyPatch attributes",
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

        var allPatchAttributes = classAttributes.Concat(patchMethods.SelectMany(patchMethodData => patchMethodData.SourceAttributes));

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
                    additionalLocations: locations.Skip(1),
                    classSymbol));

            report(classSymbol.Locations);

            for (var i = 0; i < allTargetMethodLocations.Length; i++)
            {
                report([allTargetMethodLocations[i]]);
            }
        }
    }
}
