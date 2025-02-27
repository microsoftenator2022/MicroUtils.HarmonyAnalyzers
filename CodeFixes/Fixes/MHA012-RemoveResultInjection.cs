using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MicroUtils.HarmonyAnalyzers.CodeFixes.MHA012;

internal readonly struct RemoveResultInjection : IHarmonyCodeFix
{
    const string Title = "Remove __result parameter";

    public DiagnosticId DiagnosticId => DiagnosticId.MHA012;

    public async IAsyncEnumerable<CodeAction> GetActionsAsync(
        Diagnostic diagnostic,
        Document document,
        SemanticModel sm,
        [EnumeratorCancellation] CancellationToken ct)
    {
        if (diagnostic.Location is not { } location ||
            await document.FindSyntaxNodeAsync<ParameterSyntax>(location, ct).ConfigureAwait(false) is not { } ps)
            yield break;

        yield return CodeAction.Create(
            Title,
            ct => RemoveParameterAsync(document, ps, ct),
            equivalenceKey: Title);
    }

    //internal static CodeAction GetAction(Document document, ParameterSyntax ps)
    //{
    //    return CodeAction.Create(
    //        Title,
    //        ct => RemoveParameterAsync(document, ps, ct),
    //        equivalenceKey: Title);
    //}

    private static async Task<Document> RemoveParameterAsync(Document document, ParameterSyntax ps, CancellationToken ct)
    {
        if ((await document.GetSyntaxRootAsync(ct).ConfigureAwait(false))?.RemoveNode(ps, SyntaxRemoveOptions.KeepNoTrivia) is not { } newRoot)
            return document;

        return document.WithSyntaxRoot(newRoot);
    }
}
