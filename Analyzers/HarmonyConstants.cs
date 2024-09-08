using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;

namespace MicroUtils.HarmonyAnalyzers;
public static partial class HarmonyConstants
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

    public const string Parameter_injection__args = "__args";
    public const string Parameter_injection__exception = "__exception";
    public const string Parameter_injection__instance = "__instance";
    public const string Parameter_injection__originalMethod = "__originalMethod";
    public const string Parameter_injection__result = "__result";
    public const string Parameter_injection__resultRef = "__resultRef";
    public const string Parameter_injection__runOriginal = "__runOriginal";
    public const string Parameter_injection__state = "__state";
    
    public const string TargetMethodMethodName = "TargetMethod";
    public const string TargetMethodsMethodName = "TargetMethods";

    public enum HarmonyPatchType
    {
        Prefix,
        Postfix,
        Transpiler,
        Finalizer,
        ReversePatch
    }

    public static readonly ImmutableArray<string> HarmonyPatchTypeNames =
        Enum.GetNames(typeof(HarmonyPatchType)).ToImmutableArray();

    public static readonly ImmutableArray<string> HarmonyPatchTypeAttributeNames =
        HarmonyPatchTypeNames.Select(n => $"Harmony{n}").ToImmutableArray();

    public enum PatchTargetMethodType
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
