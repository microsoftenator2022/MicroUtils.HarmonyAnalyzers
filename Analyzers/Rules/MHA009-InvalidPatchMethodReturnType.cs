﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MicroUtils.HarmonyAnalyzers.Rules;

using static DiagnosticId;
using static HarmonyConstants.HarmonyPatchType;

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
        "Patch method has invalid return type '{0}'. Valid return types: {1}.",
        nameof(RuleCategory.PatchMethod),
        DiagnosticSeverity.Warning,
        true);

    private static IEnumerable<Diagnostic> CheckPatchMethodInternal(
        //SyntaxNodeAnalysisContext context,
        Compilation compilation,
        PatchMethodData methodData,
        INamedTypeSymbol IEnumerableTType,
        CancellationToken ct)
    {
        if (methodData.PatchType is null)
            yield break;

        var voidType = compilation.GetTypeByMetadataName(typeof(void).ToString());
        var boolType = compilation.GetTypeByMetadataName(typeof(bool).ToString());
        var CodeInstrctionType = HarmonyHelpers.GetHarmonyCodeInstructionType(compilation, ct);
        var ExceptionType = compilation.GetTypeByMetadataName(typeof(Exception).ToString());

        if (voidType is null ||
            boolType is null ||
            CodeInstrctionType is null ||
            ExceptionType is null)
        {
#if DEBUG
            yield return Diagnostic.Create(
                PatchClassAnalyzer.DebugMessage,
                methodData.PatchMethod.Locations[0],
                $"void = {voidType}, " +
                $"bool = {boolType}, " +
                $"CodeInstruction = {CodeInstrctionType}, " +
                $"Exception = {ExceptionType}");
#endif
            yield break;
        }

        if (methodData.PatchType is not { } patchType)
            yield break;

        var validReturnTypes = HarmonyHelpers.ValidReturnTypes(patchType, compilation, ct, methodData.TargetMethod);

        if ((methodData.TargetMethod is not null || !methodData.PatchMethod.MayBePassthroughPostfix(methodData.TargetMethod, compilation)) &&
            !validReturnTypes.Any(t => compilation.ClassifyConversion(methodData.PatchMethod.ReturnType, t).IsImplicit))
        {
            var locations = methodData.PatchMethod.DeclaringSyntaxReferences
                .Select(s => s.GetSyntax() as MethodDeclarationSyntax)
                .NotNull()
                .Select(s => s.ReturnType.GetLocation())
                .ToImmutableArray();

            foreach (var d in methodData.CreateDiagnostics(
                descriptor: Descriptor,
                primaryLocations: methodData.PatchMethod.DeclaringSyntaxReferences
                    .Select(sr => sr.GetSyntax())
                    .OfType<MethodDeclarationSyntax>()
                    .Select(mds => mds.ReturnType.GetLocation()).ToImmutableArray(),
                messageArgs: [methodData.PatchMethod.ReturnType, string.Join(", ", validReturnTypes)]))
            {
                yield return d;
            }
        }
    }

    internal static ImmutableArray<Diagnostic> CheckPatchMethod(
        Compilation compilation,
        PatchMethodData methodData,
        INamedTypeSymbol IEnumerableTType,
        CancellationToken ct) => CheckPatchMethodInternal(compilation, methodData, IEnumerableTType, ct).ToImmutableArray();

    internal static ImmutableArray<Diagnostic> CheckTargetMethod(
        //SyntaxNodeAnalysisContext context,
        Compilation compilation,
        IMethodSymbol method,
        INamedTypeSymbol MethodBaseType)
    {
        if (!compilation.ClassifyConversion(method.ReturnType, MethodBaseType).IsImplicit)
        {
            var diagnostic = new DiagnosticBuilder(Descriptor)
            {
                MessageArgs = [method.ReturnType, MethodBaseType]
            };

            return diagnostic.ForAllLocations(method.Locations).CreateAll();

            //context.ReportDiagnostic(Diagnostic.Create(
            //    descriptor: Descriptor,
            //    location: method.Locations[0],
            //    messageArgs: [method.ReturnType, MethodBaseType]));
        }

        return [];
    }

    internal static ImmutableArray<Diagnostic> CheckTargetMethods(
        //SyntaxNodeAnalysisContext context,
        Compilation compilation,
        IMethodSymbol method,
        INamedTypeSymbol IEnumerableMethodBaseType)
    {
        if (!compilation.ClassifyConversion(method.ReturnType, IEnumerableMethodBaseType).IsImplicit)
        {
            var diagnostic = new DiagnosticBuilder(Descriptor)
            {
                MessageArgs = [method.ReturnType, IEnumerableMethodBaseType]
            };

            return diagnostic.ForAllLocations(method.Locations).CreateAll();

            //context.ReportDiagnostic(Diagnostic.Create(
            //    descriptor: Descriptor,
            //    location: method.Locations[0],
            //    messageArgs: [method.ReturnType, IEnumerableMethodBaseType]));
        }

        return [];
    }
}
