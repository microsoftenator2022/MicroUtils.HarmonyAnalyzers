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

internal readonly struct MultipleTargetMethodDefinitions : IPatchClassRule
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        nameof(MHA007),
        "Conflicting target method definitions in patch class",
        "Patch class has more than one of: TargetMethod, TargetMethods, parametrized HarmonyPatch attributes",
        nameof(RuleCategory.TargetMethod),
        DiagnosticSeverity.Warning,
        true);

    DiagnosticDescriptor IPatchRule.Descriptor => Descriptor;

    private static IEnumerable<Diagnostic> CheckInternal(
        INamedTypeSymbol classSymbol,
        ImmutableArray<AttributeData> classAttributes,
        ImmutableArray<PatchMethodData> patchMethods,
        ImmutableArray<IMethodSymbol> targetMethodMethods,
        CancellationToken ct)
    {
        var targetMethodMethodLocations =
                targetMethodMethods
                    .SelectMany(m => m.Locations);

        var allPatchAttributes = classAttributes.Concat(patchMethods.SelectMany(patchMethodData => patchMethodData.HarmonyPatchAttributes));

        var allTargetMethodLocations =
            targetMethodMethodLocations
                .Concat(allPatchAttributes
                    .Where(attr => attr.ConstructorArguments.Length > 0)
                    .Choose(attr => Optional.MaybeValue(attr.ApplicationSyntaxReference))
                    .Select(s => s.GetSyntax().GetLocation()))
                .ToImmutableArray();

        if (allTargetMethodLocations.Length > 1)
        {
            foreach (var d in new DiagnosticBuilder(Descriptor)
                .ForAllLocations(classSymbol.Locations.Concat(allTargetMethodLocations).ToImmutableArray())
                .CreateAll())
                yield return d;
        }
    }

    public ImmutableArray<Diagnostic> Check(
        PatchClassData patchClassData,
        CancellationToken ct)
    {
        return CheckInternal(
            patchClassData.ClassSymbol,
            patchClassData.ClassAttributes,
            patchClassData.PatchMethods,
            patchClassData.TargetMethodMethods.Value,
            ct)
            .ToImmutableArray();
    }
}
