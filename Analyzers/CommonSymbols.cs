using System.Collections.Immutable;
using System.Reflection;
using System.Threading;

using Microsoft.CodeAnalysis;

using static MicroUtils.HarmonyAnalyzers.HarmonyConstants;

namespace MicroUtils.HarmonyAnalyzers;
internal readonly struct CommonSymbols(
    INamedTypeSymbol harmonyPatchAttribute,
    ImmutableArray<(HarmonyPatchType, INamedTypeSymbol)> harmonyPatchTypeAttributes,
    INamedTypeSymbol harmonyTargetMethod,
    INamedTypeSymbol harmonyTargetMethods,
    INamedTypeSymbol methodBase,
    INamedTypeSymbol ienumerable_MethodBase)
{
    public readonly INamedTypeSymbol HarmonyPatchAttribute = harmonyPatchAttribute;
    public readonly ImmutableDictionary<HarmonyPatchType, INamedTypeSymbol> HarmonyPatchTypeAttributes =
        harmonyPatchTypeAttributes
            .ToImmutableDictionary(keySelector: pair => pair.Item1, elementSelector: pair => pair.Item2);
    public readonly INamedTypeSymbol HarmonyTargetMethod = harmonyTargetMethod;
    public readonly INamedTypeSymbol HarmonyTargetMethods = harmonyTargetMethods;
    public readonly INamedTypeSymbol MethodBase = methodBase;
    public readonly INamedTypeSymbol IEnumerable_MethodBase = ienumerable_MethodBase;

    public static CommonSymbols? Get(Compilation compilation, CancellationToken ct)
    {
        var harmonyPatchAttribute = HarmonyHelpers.GetHarmonyPatchType(compilation, ct);
        var harmonyPatchTypeAttributes = HarmonyHelpers.GetHarmonyPatchTypeAttributeTypes(compilation, ct);
        var harmonyTargetMethod = HarmonyHelpers.GetHarmonyTargetMethodType(compilation, ct);
        var harmonyTargetMethods = HarmonyHelpers.GetHarmonyTargetMethodsType(compilation, ct);
        var methodBase = compilation.GetTypeByMetadataName(typeof(MethodBase).ToString());
        var ienumerableMethodBase = methodBase is { } mb ?
            (compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T))?.Construct(mb) : null;

        if (ct.IsCancellationRequested ||
            harmonyPatchAttribute is null ||
            harmonyPatchTypeAttributes == default ||
            harmonyTargetMethod is null ||
            harmonyTargetMethods is null ||
            methodBase is null ||
            ienumerableMethodBase is null)
            return null;

        return new(
            harmonyPatchAttribute,
            harmonyPatchTypeAttributes,
            harmonyTargetMethod,
            harmonyTargetMethods,
            methodBase,
            ienumerableMethodBase);
    }
}
