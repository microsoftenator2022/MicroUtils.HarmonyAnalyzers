using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MicroUtils.HarmonyAnalyzers.Rules;

using static DiagnosticId;

internal readonly struct InvalidTranspilerParameter : IPatchMethodRule
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        nameof(MHA015),
        "Invalid transpiler method parameter",
        "Invalid transpiler method parameter '{0}`",
        nameof(RuleCategory.PatchMethod),
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    DiagnosticDescriptor IPatchRule.Descriptor => Descriptor;

    private static IEnumerable<IEnumerable<Diagnostic>> CheckInternal(
        PatchMethodData methodData,
        CancellationToken ct)
    {
        if (methodData.PatchType is not HarmonyConstants.HarmonyPatchType.Transpiler)
            yield break;

        var compilation = methodData.Compilation;

        var MethodBaseType = typeof(MethodBase).ToNamedTypeSymbol(compilation);
        var ILGeneratorType = compilation.GetTypeByMetadataName("System.Reflection.Emit.ILGenerator");
        var IEnumerableCodeInstructionType = HarmonyHelpers.GetIEnumerableCodeInstructionType(compilation, ct);

        if (MethodBaseType is null ||
            IEnumerableCodeInstructionType is null ||
            ILGeneratorType is null)
            yield break;

        ImmutableArray<ITypeSymbol> validParameterTypes = [IEnumerableCodeInstructionType, MethodBaseType, ILGeneratorType];

        foreach (var p in methodData.PatchMethod.Parameters.Where(p => !validParameterTypes.Contains(p.Type, SymbolEqualityComparer.Default)))
        {
            if (ct.IsCancellationRequested)
                yield break;

            var locations =
                p.DeclaringSyntaxReferences
                    .Select(sr => sr.GetSyntax(ct))
                    .OfType<ParameterSyntax>()
                    .Select(n => n.GetLocation())
                    .ToImmutableArray();

            if (locations.IsDefaultOrEmpty)
                locations = p.Locations;

            yield return methodData.CreateDiagnostics(Descriptor, locations, messageArgs: [p]);
        }
    }

    public ImmutableArray<Diagnostic> Check(
        PatchMethodData methodData,
        SemanticModel _1,
        CancellationToken ct) =>
        CheckInternal(methodData, ct).Concat().ToImmutableArray();
}
