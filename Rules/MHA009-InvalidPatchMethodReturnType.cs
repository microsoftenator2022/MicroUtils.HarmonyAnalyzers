﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MicroUtils.HarmonyAnalyzers.Rules;

using static Constants.HarmonyPatchType;

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
        "MHA009",
        "Invalid return type",
        "Method '{0}' has invalid return type '{1}'. Valid return types: {2}.",
        "PatchMethod",
        DiagnosticSeverity.Warning,
        true);

    static INamedTypeSymbol? GetIEnumerableType(Compilation compilation) =>
        compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1");

    internal static void CheckPatchMethod(
        SyntaxNodeAnalysisContext context,
        PatchMethodData methodData)
    {
        if (methodData.PatchType is null)
            return;

        var voidType = context.Compilation.GetTypeByMetadataName(typeof(void).ToString());
        var boolType = context.Compilation.GetTypeByMetadataName(typeof(bool).ToString());
        var IEnumerableT = GetIEnumerableType(context.Compilation);
        var CodeInstrctionType = HarmonyTypeHelpers.GetHarmonyCodeInstructionType(context.Compilation, context.CancellationToken);
        var ExceptionType = context.Compilation.GetTypeByMetadataName(typeof(Exception).ToString());

        if (voidType is null ||
            boolType is null ||
            IEnumerableT is null ||
            CodeInstrctionType is null ||
            ExceptionType is null)
        {
#if DEBUG
            context.ReportDiagnostic(Diagnostic.Create(
                PatchClassAnalyzer.DebugMessage,
                methodData.PatchMethod.Locations[0],
                $"void = {voidType}, " +
                $"bool = {boolType}, " +
                $"IEnumerable<T> = {IEnumerableT}, " +
                $"CodeInstruction = {CodeInstrctionType}, " +
                $"Exception = {ExceptionType}"));
#endif
            return;
        }

        ImmutableArray<ITypeSymbol> validReturnTypes = methodData.PatchType switch
        {
            Prefix => [voidType, boolType],
            Postfix => new [] { voidType, methodData.TargetMethod?.ReturnType }.NotNull().ToImmutableArray(),
            Transpiler => [IEnumerableT.Construct(CodeInstrctionType)],
            Finalizer => [voidType, ExceptionType],
            _ => []
        };

        if ((methodData.TargetMethod is not null || methodData.PatchType is not Postfix) &&
            !validReturnTypes.Any(t => context.Compilation.ClassifyConversion(methodData.PatchMethod.ReturnType, t).IsImplicit))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                descriptor: Descriptor,
                location: methodData.PatchMethod.Locations[0],
                additionalLocations: methodData.PatchMethod.Locations.Skip(1),
                messageArgs: [methodData.PatchMethod, methodData.PatchMethod.ReturnType, string.Join(", ", validReturnTypes)]));
        }
    }

    internal static void CheckTargetMethod(
        SyntaxNodeAnalysisContext context,
        IMethodSymbol method)
    {
        var MethodBaseType = context.Compilation.GetTypeByMetadataName(typeof(MethodBase).ToString());

        if (MethodBaseType is null)
        {
            return;
        }

        if (!context.Compilation.ClassifyConversion(method.ReturnType, MethodBaseType).IsImplicit)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                descriptor: Descriptor,
                location: method.Locations[0],
                additionalLocations: method.Locations.Skip(1),
                messageArgs: [method, method.ReturnType, MethodBaseType]));
        }
    }

    internal static void CheckTargetMethods(
    SyntaxNodeAnalysisContext context,
    IMethodSymbol method)
    {
        var MethodBaseType = context.Compilation.GetTypeByMetadataName(typeof(MethodBase).ToString());

        if (MethodBaseType is null)
        {
            return;
        }

        var IEnumerableTargetMethodType = GetIEnumerableType(context.Compilation)?.Construct(MethodBaseType);
        
        if (IEnumerableTargetMethodType is null)
        {
            return;
        }

        if (!context.Compilation.ClassifyConversion(method.ReturnType, IEnumerableTargetMethodType).IsImplicit)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                descriptor: Descriptor,
                location: method.Locations[0],
                additionalLocations: method.Locations.Skip(1),
                messageArgs: [method, method.ReturnType, IEnumerableTargetMethodType]));
        }
    }
}
