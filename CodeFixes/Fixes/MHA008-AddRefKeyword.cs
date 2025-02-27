using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace MicroUtils.HarmonyAnalyzers.CodeFixes.MHA008;

internal readonly struct AddRefKeyword : IHarmonyCodeFix
{
    const string Title = "Add ref keyword";

    public DiagnosticId DiagnosticId => DiagnosticId.MHA008;

    public async IAsyncEnumerable<CodeAction> GetActionsAsync(
        Diagnostic diagnostic,
        Document document,
        SemanticModel sm,
        [EnumeratorCancellation] CancellationToken ct)
    {
        if (diagnostic.AdditionalLocations.FirstOrDefault() is not { } paramLocation ||
            await document.FindSyntaxNodeAsync<ParameterSyntax>(paramLocation, ct).ConfigureAwait(false) is not { } ps)
            yield break;

        yield return CodeAction.Create(Title, ct => AddRefKeywordAsync(document, ps, ct), Title);
    }

    private static async Task<Document> AddRefKeywordAsync(Document document, ParameterSyntax ps, CancellationToken ct)
    {
        var newPs = ps.AddModifiers(SyntaxFactory.Token(SyntaxKind.RefKeyword));

        if ((await document.GetSyntaxRootAsync(ct).ConfigureAwait(false))?.ReplaceNode(ps, newPs) is not { } newRoot)
            return document;

        return document.WithSyntaxRoot(newRoot);
    }
}
