using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MicroUtils.HarmonyAnalyzers.CodeFixes.MHA012;

internal static class RemoveResultInjection
{
    const string Title = "Remove __result parameter";

    internal static CodeAction GetAction(Document document, ParameterSyntax ps)
    {
        return CodeAction.Create(
            Title,
            ct => RemoveParameterAsync(document, ps, ct),
            equivalenceKey: Title);
    }

    private static async Task<Document> RemoveParameterAsync(Document document, ParameterSyntax ps, CancellationToken ct)
    {
        if ((await document.GetSyntaxRootAsync(ct))?.RemoveNode(ps, SyntaxRemoveOptions.KeepNoTrivia) is not { } newRoot)
            return document;

        return document.WithSyntaxRoot(newRoot);
    }
}
