using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;

namespace MicroUtils.HarmonyAnalyzers.Rules;

using static DiagnosticId;

internal static class InvalidTranspilerParameter
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        nameof(MHA015),
        "Invalid transpiler method parameter",
        "Invalid transpiler method parameter '{0}`",
        nameof(RuleCategory.PatchMethod),
        DiagnosticSeverity.Warning,
        true);

    private static IEnumerable<IEnumerable<Diagnostic>> CheckInternal(
        PatchMethodData methodData,
        //Compilation compilation,
        CancellationToken ct)
    {
        if (methodData.PatchType is not HarmonyConstants.HarmonyPatchType.Transpiler)
            yield break;

        var compilation = methodData.Compilation;

        //var IEnumerableTType = compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1");
        //var CodeInstructionType = HarmonyHelpers.GetHarmonyCodeInstructionType(compilation, ct);
        var MethodBaseType = compilation.GetTypeByMetadataName(typeof(MethodBase).GetMetadataName());
        var ILGeneratorType = compilation.GetTypeByMetadataName("System.Reflection.Emit.ILGenerator");
        var IEnumerableCodeInstructionType = HarmonyHelpers.GetIEnumerableCodeInstructionType(compilation, ct);

        if (MethodBaseType is null ||
            //IEnumerableTType is null ||
            //CodeInstructionType is null ||
            IEnumerableCodeInstructionType is null ||
            ILGeneratorType is null)
            yield break;

        //var IEnumerableCodeInstructionType = IEnumerableTType.Construct(CodeInstructionType);

        ImmutableArray<ITypeSymbol> validParameterTypes = [IEnumerableCodeInstructionType, MethodBaseType, ILGeneratorType];

        foreach (var p in methodData.PatchMethod.Parameters.Where(p => !validParameterTypes.Contains(p.Type, SymbolEqualityComparer.Default)))
        {
            if (ct.IsCancellationRequested)
                yield break;

            yield return methodData.CreateDiagnostics(Descriptor, p.Locations, messageArgs: [p]);
        }
    }

    internal static ImmutableArray<Diagnostic> Check(
        PatchMethodData methodData,
        //Compilation compilation,
        CancellationToken ct) =>
        CheckInternal(methodData, ct).Concat().ToImmutableArray();
}
