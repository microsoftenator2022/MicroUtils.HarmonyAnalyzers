using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MicroUtils.HarmonyAnalyzers.Rules;

using static DiagnosticId;

internal readonly struct AssignmentToNonRefResultArgument : IPatchMethodRule
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        nameof(MHA008),
        "Assignment to non-ref patch method argument",
        "Assignment to non-ref argument '{0}'",
        nameof(RuleCategory.PatchMethod),
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    DiagnosticDescriptor IPatchRule.Descriptor => Descriptor;

    private static IEnumerable<Diagnostic> CheckInternal(
        SemanticModel semanticModel,
        PatchMethodData methodData,
        CancellationToken ct)
    {
        var nonRefParameters = methodData.PatchMethod.Parameters.Where(p => p.RefKind is not RefKind.Ref and not RefKind.Out).ToImmutableArray();

        if (nonRefParameters.Length < 1)
            yield break;

        var assignments = methodData.PatchMethod.DeclaringSyntaxReferences.SelectMany(n => n.GetSyntax().DescendantNodes(_ => true))
            .OfType<AssignmentExpressionSyntax>()
            .Select(node => (node, symbol: semanticModel.GetSymbolInfo(node.Left, ct).Symbol))
            .Where(n => n.symbol is not null && nonRefParameters.Contains(n.symbol, SymbolEqualityComparer.Default));

        foreach (var (node, symbol) in assignments)
        {
            if (ct.IsCancellationRequested)
                yield break;

            foreach (var d in methodData.CreateDiagnostics(
                Descriptor,
                primaryLocations: [node.Left.GetLocation()],
                additionalLocations: symbol?.Locations ?? default))
                yield return d;
        }
    }

    public ImmutableArray<Diagnostic> Check(
        PatchMethodData methodData,
        SemanticModel semanticModel,
        CancellationToken ct) => CheckInternal(semanticModel, methodData, ct).ToImmutableArray();
}
