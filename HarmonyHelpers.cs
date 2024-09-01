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

    public static INamedTypeSymbol? GetHarmonyCodeInstructionType(Compilation compilation, CancellationToken ct) =>
        compilation.GetType(Constants.Namespace_HarmonyLib, Constants.Type_HarmonyLib_CodeInstruction, ct);

    public static readonly Regex FieldInjectionRegex = new(@"^___(\w+)$");

    public static readonly Regex ArgInjectionRegex = new(@"^__(\d+)$");

    public static bool IsInjectionNameConstant(string name) => name switch
    {
        Constants.Parameter_injection__args => true,
        Constants.Parameter_injection__instance => true,
        Constants.Parameter_injection__originalMethod => true,
        Constants.Parameter_injection__result => true,
        Constants.Parameter_injection__resultRef => true,
        Constants.Parameter_injection__runOriginal => true,
        Constants.Parameter_injection__state => true,
        _ => false
    };
}
