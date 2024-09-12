using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MicroUtils.HarmonyAnalyzers.CodeFixes.MHA016;
internal static class UseOutForPrefixStateInjection
{
    const string Title = "Use out for __state injection";

    internal static CodeAction GetAction(Document document, ParameterSyntax ps)
    {
        return CodeAction.Create(Title, ct => SetOutKeywordAsync(document, ps, ct));
    }

    private static async Task<Document> SetOutKeywordAsync(Document document, ParameterSyntax ps, CancellationToken ct)
    {
        var newModifiers = 
            (ps.Modifiers.FirstOrDefault(m => m.Kind() is SyntaxKind.RefKeyword) is { } token ? 
                ps.Modifiers.Remove(token) :
                ps.Modifiers)
            .Add(SyntaxFactory.Token(SyntaxKind.OutKeyword));

        var newPs = ps.WithModifiers(newModifiers);

        if ((await document.GetSyntaxRootAsync(ct))?.ReplaceNode(ps, newPs) is not { } newRoot)
            return document;

        return document.WithSyntaxRoot(newRoot);
    }
}
