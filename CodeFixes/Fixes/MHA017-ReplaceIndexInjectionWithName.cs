using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MicroUtils.HarmonyAnalyzers.CodeFixes.MHA017;
internal static class ReplaceIndexInjectionWithName
{
    internal static CodeAction? GetAction(Document document, Diagnostic diagnostic, ParameterSyntax ps)
    {
        if (!diagnostic.Properties.TryGetValue("ParameterName", out var name) || name is null)
            return null;
        
        return CodeAction.Create(
            $"Replace '{ps.Identifier}' with '{name}'",
            ct => ReplaceParameterNameAsync(document, ps, name, ct));
    }

    private static async Task<Document> ReplaceParameterNameAsync(Document document, ParameterSyntax ps, string parameterName, CancellationToken ct)
    {
        var newPs = ps.WithIdentifier(SyntaxFactory.Identifier(parameterName));

        if ((await document.GetSyntaxRootAsync(ct))?.ReplaceNode(ps, newPs) is not { } newRoot)
            return document;

        return document.WithSyntaxRoot(newRoot);
    }
}
