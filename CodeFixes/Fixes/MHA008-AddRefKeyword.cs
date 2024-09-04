using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace MicroUtils.HarmonyAnalyzers.CodeFixes.MHA008;

internal static class AddRefKeyword
{
    const string Title = "Add ref keyword";

    internal static CodeAction GetAction(Document document, ParameterSyntax ps)
    {
        return CodeAction.Create(Title, ct => AddRefKeywordAsync(document, ps, ct), Title);
    }

    private static async Task<Document> AddRefKeywordAsync(Document document, ParameterSyntax ps, CancellationToken ct)
    {
        var newPs = ps.AddModifiers(SyntaxFactory.Token(SyntaxKind.RefKeyword));

        if ((await document.GetSyntaxRootAsync(ct))?.ReplaceNode(ps, newPs) is not { } newRoot)
            return document;

        return document.WithSyntaxRoot(newRoot);
    }
}
