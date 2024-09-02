using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MicroUtils.HarmonyAnalyzers.Rules;

using static DiagnosticId;

internal class AssignmentToNonRefResultArgument
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        nameof(MHA008),
        "Assignment to non-ref patch method argument",
        "Assignment to non-ref argument '{0}'",
        nameof(RuleCategory.PatchMethod),
        DiagnosticSeverity.Warning,
        true);

    internal static void Check(
        SyntaxNodeAnalysisContext context,
        PatchMethodData methodData)
    {
        var nonRefParameters = methodData.PatchMethod.Parameters.Where(p => p.RefKind is not RefKind.Ref).ToImmutableArray();

        if (nonRefParameters.Length < 1)
            return;

        var assignments = methodData.PatchMethod.DeclaringSyntaxReferences.SelectMany(n => n.GetSyntax().DescendantNodes(_ => true))
            .OfType<AssignmentExpressionSyntax>()
            .Select(node => (node, symbol: context.SemanticModel.GetSymbolInfo(node.Left, context.CancellationToken).Symbol))
            .Where(n => n.symbol is not null && nonRefParameters.Contains(n.symbol, SymbolEqualityComparer.Default));

        foreach (var (node, symbol) in assignments)
        {
            if (context.CancellationToken.IsCancellationRequested)
                return;

            context.ReportDiagnostic(Diagnostic.Create(
                descriptor: Descriptor,
                location: node.Left.GetLocation(),
                messageArgs: [symbol]));
        }
    }
}
