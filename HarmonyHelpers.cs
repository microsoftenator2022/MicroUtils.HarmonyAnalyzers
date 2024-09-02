using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MicroUtils.HarmonyAnalyzers;
internal static class HarmonyHelpers
{
    public static INamedTypeSymbol? GetHarmonyPatchType(Compilation compilation, CancellationToken ct) =>
        compilation.GetType(HarmonyConstants.Namespace_HarmonyLib, HarmonyConstants.Attribute_HarmonyLib_HarmonyPatch, ct);

    public static INamedTypeSymbol? GetHarmonyMethodTypeType(Compilation compilation, CancellationToken ct) =>
        compilation.GetType(HarmonyConstants.Namespace_HarmonyLib, HarmonyConstants.Attribute_HarmonyLib_HarmonyPatch, ct);

    public static IEnumerable<(HarmonyConstants.HarmonyPatchType, INamedTypeSymbol)> GetHarmonyPatchMethodAttributeTypes(Compilation compilation, CancellationToken ct) =>
        Enum.GetValues(typeof(HarmonyConstants.HarmonyPatchType)).Cast<HarmonyConstants.HarmonyPatchType>()
            .SelectMany<HarmonyConstants.HarmonyPatchType, (HarmonyConstants.HarmonyPatchType, INamedTypeSymbol)>(pt =>
                pt.GetPatchTypeAttributeType(compilation, ct) is { } t ? [(pt, t)] : []);

    public static INamedTypeSymbol? GetHarmonyTargetMethodType(Compilation compilation, CancellationToken ct) =>
        compilation.GetType(HarmonyConstants.Namespace_HarmonyLib, HarmonyConstants.Attribute_HarmonyLib_HarmonyTargetMethod, ct);

    public static INamedTypeSymbol? GetHarmonyTargetMethodsType(Compilation compilation, CancellationToken ct) =>
        compilation.GetType(HarmonyConstants.Namespace_HarmonyLib, HarmonyConstants.Attribute_HarmonyLib_HarmonyTargetMethods, ct);

    public static INamedTypeSymbol? GetHarmonyCodeInstructionType(Compilation compilation, CancellationToken ct) =>
        compilation.GetType(HarmonyConstants.Namespace_HarmonyLib, HarmonyConstants.Type_HarmonyLib_CodeInstruction, ct);

    public static readonly Regex FieldInjectionRegex = new(@"^___(\w+)$");

    public static readonly Regex ArgInjectionRegex = new(@"^__(\d+)$");

    public static bool IsInjectionNameConstant(string name) => name switch
    {
        HarmonyConstants.Parameter_injection__args => true,
        HarmonyConstants.Parameter_injection__instance => true,
        HarmonyConstants.Parameter_injection__originalMethod => true,
        HarmonyConstants.Parameter_injection__result => true,
        HarmonyConstants.Parameter_injection__resultRef => true,
        HarmonyConstants.Parameter_injection__runOriginal => true,
        HarmonyConstants.Parameter_injection__state => true,
        _ => false
    };
}
