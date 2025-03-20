using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MicroUtils.HarmonyAnalyzers.Rules;

using static DiagnosticId;

internal static class InvalidPatchMethodReturnType
{
    // Method return types must be assignable to one of these types:
    // Prefix: void, bool
    // Postfix: void, target method's return type
    // Transpiler: IEnumerable<CodeInstruction>
    // Finalizer: void, Exception
    // TargetMethod: MethodBase
    // TargetMethods: IEnumerable<MethodBase>

    internal static readonly DiagnosticDescriptor Descriptor = new(
        nameof(MHA009),
        "Invalid return type",
        "Patch method has invalid return type '{0}'. Valid return types: {1}."
#if DEBUG
        + " DEBUG: [{2}]."
#endif
        ,
        nameof(RuleCategory.PatchMethod),
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: ReferenceDoc.GetUriString(MHA009).ValueOrDefault());

    static IEnumerable<TypeSyntax> GetMethodReturnTypeNodes(IMethodSymbol method) =>
        method.DeclaringSyntaxReferences
            .Choose(s => Optional.MaybeValue(s.GetSyntax() as MethodDeclarationSyntax))
            .Select(s => s.ReturnType);

    internal readonly struct PatchMethod : IPatchMethodRule
    {
        DiagnosticDescriptor IPatchRule.Descriptor => Descriptor;

        private static IEnumerable<Diagnostic> CheckInternal(
            PatchMethodData methodData,
            CancellationToken ct)
        {
            if (methodData.PatchType is null)
                yield break;

            var compilation = methodData.Compilation;

        

            var voidType = compilation.GetSpecialType(SpecialType.System_Void);
            var boolType = compilation.GetSpecialType(SpecialType.System_Boolean);
            var ExceptionType = typeof(Exception).ToNamedTypeSymbol(compilation);

            if (ExceptionType is null)
            {
    #if DEBUG
                yield return Diagnostic.Create(
                    PatchClassAnalyzer.DebugMessage,
                    methodData.PatchMethod.Locations[0],
                    $"Exception = {ExceptionType}");
    #endif
                yield break;
            }

            if (methodData.PatchType is not { } patchType)
                yield break;

            var maybePassthrough = methodData.PatchMethod.MayBePassthroughPostfix(methodData.TargetMethod, compilation);

            var (hasValidReturnType, validReturnTypes) = HarmonyHelpers.HasValidReturnType(methodData, compilation, ct);

            if ((methodData.TargetMethod is not null || !maybePassthrough) && !hasValidReturnType
                )
            {
                foreach (var d in methodData.CreateDiagnostics(
                    descriptor: Descriptor,
                    primaryLocations: GetMethodReturnTypeNodes(methodData.PatchMethod)
                        .Select(n => n.GetLocation()).ToImmutableArray(),
                    messageArgs:
                    [
                        methodData.PatchMethod.ReturnType, string.Join(", ", validReturnTypes),
    #if DEBUG
                        maybePassthrough ?
                        $"Return type: {methodData.PatchMethod.ReturnType} -> {methodData.TargetMethod?.ReturnType}. " +
                        $@"Conversion: {(methodData.TargetMethod is not null ?
                            compilation.ClassifyConversion(methodData.PatchMethod.ReturnType, methodData.TargetMethod.ReturnType) :
                            null)}" : ""
    #endif
                    ]))
                {
                    yield return d;
                }
            }
        }

        public ImmutableArray<Diagnostic> Check(
            PatchMethodData methodData,
            SemanticModel _1,
            CancellationToken ct) => CheckInternal(methodData, ct).ToImmutableArray();
    }

    internal readonly struct TargetMethod : IPatchClassRule
    {
        DiagnosticDescriptor IPatchRule.Descriptor => Descriptor;

        public ImmutableArray<Diagnostic> Check(PatchClassData patchClassData, CancellationToken ct)
        {
            return patchClassData.TargetMethodMethods.Value
                .SelectMany(method =>
                {
                    if (ct.IsCancellationRequested)
                        return [];

                    if (!patchClassData.Compilation
                        .ClassifyConversion(method.ReturnType, patchClassData.CommonSymbols.MethodBase)
                        .IsStandardImplicit())
                    {
                        var diagnostic = new DiagnosticBuilder(Descriptor)
                        {
                            MessageArgs =
                            [
                                method.ReturnType, patchClassData.CommonSymbols.MethodBase,
#if DEBUG
                                null
#endif
                            ]
                        };

                        var locations = GetMethodReturnTypeNodes(method)
                            .Select(n => n.GetLocation())
                            .ToImmutableArray();

                        return diagnostic.ForAllLocations(locations).CreateAll();
                    }

                    return [];
                })
                .ToImmutableArray();
        }
    }

    internal readonly struct TargetMethods : IPatchClassRule
    {
        DiagnosticDescriptor IPatchRule.Descriptor => Descriptor;

        public ImmutableArray<Diagnostic> Check(PatchClassData patchClassData, CancellationToken ct)
        {
            return patchClassData.TargetMethodsMethods.Value
                .SelectMany(method =>
                {
                    if (ct.IsCancellationRequested)
                        return [];

                    if (!patchClassData.Compilation
                        .ClassifyConversion(method.ReturnType, patchClassData.CommonSymbols.IEnumerable_MethodBase)
                        .IsStandardImplicit())
                    {
                        var diagnostic = new DiagnosticBuilder(Descriptor)
                        {
                            MessageArgs =
                            [
                                method.ReturnType, patchClassData.CommonSymbols.IEnumerable_MethodBase,
#if DEBUG
                                null
#endif
                            ]
                        };

                        var locations = GetMethodReturnTypeNodes(method)
                            .Select(n => n.GetLocation())
                            .ToImmutableArray();

                        return diagnostic.ForAllLocations(locations).CreateAll();
                    }

                    return [];

                })
                .ToImmutableArray();
        }
    }
}
