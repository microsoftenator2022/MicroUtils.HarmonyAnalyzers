using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MicroUtils.HarmonyAnalyzers.CodeFixes.MHA017;
internal readonly struct ReplaceIndexInjectionWithName : IHarmonyCodeFix
{
    public DiagnosticId DiagnosticId => DiagnosticId.MHA017;

    public async IAsyncEnumerable<CodeAction> GetActionsAsync(
        Diagnostic diagnostic,
        Document document,
        SemanticModel sm,
        [EnumeratorCancellation] CancellationToken ct)
    {
        if (diagnostic.Location is not { } location ||
            await document.FindSyntaxNodeAsync<ParameterSyntax>(location, ct).ConfigureAwait(false) is not { } ps)
            yield break;

        if (!diagnostic.Properties.TryGetValue("ParameterName", out var name) || name is null)
            yield break;

        yield return CodeAction.Create(
            $"Replace '{ps.Identifier}' with '{name}'",
            ct => ReplaceParameterNameAsync(document, ps, name, ct));
    }

    //internal static CodeAction? GetAction(Document document, Diagnostic diagnostic, ParameterSyntax ps)
    //{
    //    if (!diagnostic.Properties.TryGetValue("ParameterName", out var name) || name is null)
    //        return null;

    //    return CodeAction.Create(
    //        $"Replace '{ps.Identifier}' with '{name}'",
    //        ct => ReplaceParameterNameAsync(document, ps, name, ct));
    //}

    private static async Task<Document> ReplaceParameterNameAsync(Document document, ParameterSyntax ps, string parameterName, CancellationToken ct)
    {
        var newPs = ps.WithIdentifier(SyntaxFactory.Identifier(parameterName));

        if ((await document.GetSyntaxRootAsync(ct).ConfigureAwait(false))?.ReplaceNode(ps, newPs) is not { } newRoot)
            return document;

        return document.WithSyntaxRoot(newRoot);
    }
}
