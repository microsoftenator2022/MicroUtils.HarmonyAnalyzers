using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;

namespace MicroUtils.HarmonyAnalyzers;

public static partial class Constants
{
    public const string Namespace_HarmonyLib = "HarmonyLib";

    public const string Attribute_HarmonyLib_HarmonyPatch = "HarmonyPatch";
    public const string Attribute_HarmonyLib_HarmonyTargetMethod = "HarmonyTargetMethod";
    public const string Attribute_HarmonyLib_HarmonyTargetMethods = "HarmonyTargetMethods";

    public const string Type_HarmonyLib_MethodType = "MethodType";
    public const string Type_HarmonyLib_CodeInstruction = "CodeInstruction";

    public const string Parameter_declaringType = "declaringType";
    public const string Parameter_argumentTypes = "argumentTypes";
    public const string Parameter_methodName = "methodName";
    public const string Parameter_argumentVariations = "argumentVariations";
    public const string Parameter_methodType = "methodType";

    public const string TargetMethodMethodName = "TargetMethod";
    public const string TargetMethodsMethodName = "TargetMethods";

    public enum RuleCategory
    {
        PatchAttribute,
        TargetMethod,
        PatchMethod
    }

    internal enum HarmonyPatchType
    {
        Prefix,
        Postfix,
        Transpiler,
        Finalizer
    }

    public static readonly ImmutableArray<string> HarmonyPatchTypeNames =
        Enum.GetNames(typeof(HarmonyPatchType)).ToImmutableArray();

    public static readonly ImmutableArray<string> HarmonyPatchTypeAttributeNames =
        HarmonyPatchTypeNames.Select(n => $"Harmony{n}").ToImmutableArray();

    internal static INamedTypeSymbol? GetPatchTypeAttributeType(this HarmonyPatchType patchType, Compilation compilation, CancellationToken ct) =>
        compilation.GetType(Namespace_HarmonyLib, $"Harmony{patchType}", ct);

    internal enum PatchTargetMethodType
    {
        Normal,
        Getter,
        Setter,
        Constructor,
        StaticConstructor,
        Enumerator,
        Async
    }

    public static readonly ImmutableArray<string> PatchTargetMethodTypeNames =
        Enum.GetNames(typeof(PatchTargetMethodType)).ToImmutableArray();
}