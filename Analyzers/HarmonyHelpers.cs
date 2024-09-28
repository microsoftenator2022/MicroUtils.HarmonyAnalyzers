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

    public static bool MayBeHarmonyPatchMethod(
        IMethodSymbol method,
        INamedTypeSymbol harmonyPatchAttributeType,
        IEnumerable<INamedTypeSymbol> harmonyPatchTypeAttributeTypes)
    {
        if (HarmonyPatchTypeNames.Contains(method.Name))
            return true;

        if (method.GetAttributes()
            .Select(attr => attr.AttributeClass)
            .ContainsAny(
                harmonyPatchTypeAttributeTypes.Append(harmonyPatchAttributeType),
                SymbolEqualityComparer.Default))
            return true;

        return false;
    }

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

    public static ITypeSymbol? GetInjectionParameterType(
        string parameterName,
        Compilation compilation,
        INamedTypeSymbol? containingType,
        ITypeSymbol? returnType = null)
    {
        return parameterName switch
        {
            Parameter_injection__args => compilation.CreateArrayTypeSymbol(compilation.GetSpecialType(SpecialType.System_Object)),
            Parameter_injection__exception => typeof(Exception).ToNamedTypeSymbol(compilation),
            Parameter_injection__instance => containingType,
            Parameter_injection__originalMethod => typeof(MethodBase).ToNamedTypeSymbol(compilation),
            Parameter_injection__result => returnType,
            Parameter_injection__runOriginal => compilation.GetSpecialType(SpecialType.System_Boolean),
            _ => null
        };
    }

    public static ITypeSymbol? GetInjectionParameterType(string parameterName, PatchMethodData methodData) =>
        GetInjectionParameterType(
            parameterName,
            methodData.Compilation,
            methodData.TargetMethodInstanceTypes.FirstOrDefault(),
            methodData.TargetMethod?.ReturnType);

    [Obsolete("Use the PatchMethodData overload")]
    public static ITypeSymbol? GetInjectionParameterType(string parameterName, Compilation compilation, IMethodSymbol? targetMethod = null) =>
        GetInjectionParameterType(parameterName, compilation, targetMethod?.ContainingType, targetMethod?.ReturnType);

    public static ITypeSymbol? GetIEnumerableCodeInstructionType(Compilation compilation, CancellationToken ct)
    {
        if (GetHarmonyCodeInstructionType(compilation, ct) is not { } ci)
            return null;

        return compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).Construct(ci);
    }

    public static ImmutableArray<ITypeSymbol> ValidReturnTypes(
    HarmonyPatchType patchType,
    Compilation compilation,
    CancellationToken ct,
    ITypeSymbol? returnType = null,
    bool passthrough = false)
    {
        var voidType = compilation.GetSpecialType(SpecialType.System_Void);
        var boolType = compilation.GetSpecialType(SpecialType.System_Boolean);
        var ExceptionType = typeof(Exception).ToNamedTypeSymbol(compilation);

        var IEnumerableCodeInstructionType = GetIEnumerableCodeInstructionType(compilation, ct);

        if (IEnumerableCodeInstructionType is null ||
            ExceptionType is null)
            return [];

        return patchType switch
        {
            HarmonyPatchType.Prefix => [voidType, boolType],
            HarmonyPatchType.Postfix =>
                passthrough && returnType is not null ? [voidType, returnType] : [voidType],
            HarmonyPatchType.Transpiler => [IEnumerableCodeInstructionType],
            HarmonyPatchType.Finalizer => [voidType, ExceptionType],
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
            methodData.TargetMethod?.ReturnType,
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
            //(HarmonyPatchType.ReversePatch, var types) => (types.Any(t => t.Equals(methodData.TargetMethod.ReturnType, SymbolEqualityComparer.Default)), types),
            (HarmonyPatchType.ReversePatch, _) => (true, []),
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

    public static Optional<HarmonyPatchType> TryParseHarmonyPatchType(string name)
    {
        if (TryParseHarmonyPatchType(name, out HarmonyPatchType patchType))
            return patchType;

        return default;
    }

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
