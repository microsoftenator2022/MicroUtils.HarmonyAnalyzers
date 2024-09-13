using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

using MicroUtils.HarmonyAnalyzers;

namespace MicroUtils.HarmonyAnalyzers;

using static HarmonyConstants;

public static class HarmonyHelpers
{
    public static INamedTypeSymbol? GetHarmonyPatchType(Compilation compilation, CancellationToken ct) =>
        compilation.GetType(Namespace_HarmonyLib, Attribute_HarmonyLib_HarmonyPatch, ct);

    public static INamedTypeSymbol? GetHarmonyMethodTypeType(Compilation compilation, CancellationToken ct) =>
        compilation.GetType(Namespace_HarmonyLib, Type_HarmonyLib_MethodType, ct);

    public static IEnumerable<(HarmonyPatchType, INamedTypeSymbol)> GetHarmonyPatchTypeAttributeTypes(Compilation compilation, CancellationToken ct) =>
        Enum.GetValues(typeof(HarmonyPatchType)).Cast<HarmonyPatchType>()
            .SelectMany<HarmonyPatchType, (HarmonyPatchType, INamedTypeSymbol)>(pt =>
                GetPatchTypeAttributeType(pt, compilation, ct) is { } t ? [(pt, t)] : []);

    public static INamedTypeSymbol? GetHarmonyTargetMethodType(Compilation compilation, CancellationToken ct) =>
        compilation.GetType(Namespace_HarmonyLib, Attribute_HarmonyLib_HarmonyTargetMethod, ct);

    public static INamedTypeSymbol? GetHarmonyTargetMethodsType(Compilation compilation, CancellationToken ct) =>
        compilation.GetType(Namespace_HarmonyLib, Attribute_HarmonyLib_HarmonyTargetMethods, ct);

    public static INamedTypeSymbol? GetHarmonyCodeInstructionType(Compilation compilation, CancellationToken ct) =>
        compilation.GetType(Namespace_HarmonyLib, Type_HarmonyLib_CodeInstruction, ct);

    public static readonly Regex FieldInjectionRegex = new(@"^___(\w+)$");

    public static readonly Regex ArgInjectionRegex = new(@"^__(\d+)$");

    public static bool IsInjectionNameConstant(string name) => name switch
    {
        Parameter_injection__args => true,
        Parameter_injection__exception => true,
        Parameter_injection__instance => true,
        Parameter_injection__originalMethod => true,
        Parameter_injection__result => true,
        Parameter_injection__resultRef => true,
        Parameter_injection__runOriginal => true,
        Parameter_injection__state => true,
        _ => false
    };

    public static ITypeSymbol? GetInjectionParameterType(string parameterName, Compilation compilation, IMethodSymbol? targetMethod = null)
    {
        return parameterName switch
        {
            Parameter_injection__args => typeof(object[]).ToNamedTypeSymbol(compilation),
            Parameter_injection__exception => typeof(Exception).ToNamedTypeSymbol(compilation),
            Parameter_injection__instance => targetMethod?.ContainingType,
            Parameter_injection__originalMethod => typeof(MethodBase).ToNamedTypeSymbol(compilation),
            Parameter_injection__result => targetMethod?.ReturnType,
            Parameter_injection__runOriginal => typeof(bool).ToNamedTypeSymbol(compilation),
            _ => null
        };
    }

    public static ITypeSymbol? GetIEnumerableCodeInstructionType(Compilation compilation, CancellationToken ct)
    {
        if (GetHarmonyCodeInstructionType(compilation, ct) is not { } ci)
            return null;

        return compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1")?.Construct(ci);
    }
        

    public static ImmutableArray<ITypeSymbol> ValidReturnTypes(
        HarmonyPatchType patchType,
        Compilation compilation,
        CancellationToken ct,
        IMethodSymbol? targetMethod = null,
        bool passthrough = false)
    {
        var voidType = compilation.GetTypeByMetadataName(typeof(void).ToString());
        var boolType = compilation.GetTypeByMetadataName(typeof(bool).ToString());
        //var CodeInstructionType = GetHarmonyCodeInstructionType(compilation, ct);
        var ExceptionType = compilation.GetTypeByMetadataName(typeof(Exception).ToString());
        //var IEnumerableTType = compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1");

        var IEnumerableCodeInstructionType = GetIEnumerableCodeInstructionType(compilation, ct);

        if (voidType is null ||
            boolType is null ||
            IEnumerableCodeInstructionType is null ||
            //CodeInstructionType is null ||
            //IEnumerableTType is null ||
            ExceptionType is null)
            return [];

        return patchType switch
        {
            HarmonyPatchType.Prefix => [voidType, boolType],
            HarmonyPatchType.Postfix =>
                passthrough && targetMethod?.ReturnType is { }  returnType ? [voidType, returnType] : [voidType],
            HarmonyPatchType.Transpiler => [IEnumerableCodeInstructionType],
            HarmonyPatchType.Finalizer => [voidType, ExceptionType],
            HarmonyPatchType.ReversePatch => [targetMethod?.ReturnType is { } returnType ? returnType : voidType],
            _ => []
        };
    }

    public static ImmutableArray<ITypeSymbol> ValidReturnTypes(
        PatchMethodData methodData,
        Compilation compilation,
        CancellationToken ct)
    {
        if (methodData.PatchType is not { } patchType)
            return [];

        return ValidReturnTypes(
            patchType,
            compilation,
            ct,
            methodData.TargetMethod,
            methodData.PatchMethod.MayBePassthroughPostfix(methodData.TargetMethod, compilation));
    }

    public static (bool, ImmutableArray<ITypeSymbol>) HasValidReturnType(
        this PatchMethodData methodData,
        Compilation compilation,
        CancellationToken ct)
    {
        return (methodData.PatchType, ValidReturnTypes(methodData, compilation, ct)) switch
        {
            (null, _) => (true, []),
            (HarmonyPatchType.ReversePatch, var types) => (types.Any(t => t.Equals(methodData.TargetMethod?.ReturnType, SymbolEqualityComparer.Default)), types),
            (_, var types) => (types.Any(t => compilation.ClassifyConversion(methodData.PatchMethod.ReturnType, t).IsStandardImplicit()), types)
        };
    }

    public static bool MayBePassthroughPostfix(this IMethodSymbol patchMethod, IMethodSymbol? targetMethod, Compilation compilation)
    {
        if (targetMethod is null)
        {
            return patchMethod.Parameters.Length > 0 &&
                compilation.ClassifyConversion(patchMethod.Parameters[0].Type, patchMethod.ReturnType).IsStandardImplicit();
        }    

        return patchMethod.Parameters.Length > 0 &&
            compilation.ClassifyConversion(patchMethod.ReturnType, targetMethod.ReturnType).IsStandardImplicit() &&
            compilation.ClassifyConversion(targetMethod.ReturnType, patchMethod.Parameters[0].Type).IsStandardImplicit();
    }

    public static bool TryParseHarmonyPatchType(string name, out HarmonyPatchType patchType) => Enum.TryParse(name, out patchType);

    public static IEnumerable<(AttributeData, HarmonyPatchType)> GetPatchTypeAttributes(
        this PatchMethodData methodData, Compilation compilation, CancellationToken ct)
    {
        var patchTypeAttributesTypes = GetHarmonyPatchTypeAttributeTypes(compilation, ct);

        if (ct.IsCancellationRequested)
            return [];

        return methodData.PatchMethod.GetAttributes()
            .Choose(attr =>
            {
                foreach (var (patchType, attributeType) in patchTypeAttributesTypes)
                {
                    if (ct.IsCancellationRequested)
                        return default;

                    if (attributeType.Equals(attr.AttributeClass, SymbolEqualityComparer.Default))
                        return Optional.Value((attr, patchType));
                }

                return default;
            });
    }

    public static INamedTypeSymbol? GetPatchTypeAttributeType(this HarmonyPatchType patchType, Compilation compilation, CancellationToken ct) =>
        compilation.GetType(Namespace_HarmonyLib, $"Harmony{patchType}", ct);
}
