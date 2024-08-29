using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MicroUtils.HarmonyAnalyzers;
internal class HarmonyTypeHelpers
{
    public static INamedTypeSymbol? GetHarmonyPatchType(Compilation compilation, CancellationToken ct) =>
        compilation.GetType(Constants.Namespace_HarmonyLib, Constants.Attribute_HarmonyLib_HarmonyPatch, ct);

    public static INamedTypeSymbol? GetHarmonyMethodTypeType(Compilation compilation, CancellationToken ct) =>
        compilation.GetType(Constants.Namespace_HarmonyLib, Constants.Attribute_HarmonyLib_HarmonyPatch, ct);

    public static IEnumerable<INamedTypeSymbol> GetHarmonyPatchMethodAttributeTypes(Compilation compilation, CancellationToken ct) =>
        Constants.HarmonyPatchTypeAttributeNames
            .Select(name => compilation.GetType(Constants.Namespace_HarmonyLib, name, ct))
            .NotNull();

    public static INamedTypeSymbol? GetHarmonyTargetMethodType(Compilation compilation, CancellationToken ct) =>
        compilation.GetType(Constants.Namespace_HarmonyLib, Constants.Attribute_HarmonyLib_HarmonyTargetMethod, ct);

    public static INamedTypeSymbol? GetHarmonyTargetMethodsType(Compilation compilation, CancellationToken ct) =>
        compilation.GetType(Constants.Namespace_HarmonyLib, Constants.Attribute_HarmonyLib_HarmonyTargetMethods, ct);
}
