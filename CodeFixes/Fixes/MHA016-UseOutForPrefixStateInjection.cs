using System;
using System.Collections.Generic;
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

using MicroUtils.HarmonyAnalyzers.CodeFixes.Fixes;

namespace MicroUtils.HarmonyAnalyzers.CodeFixes.MHA016;

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class UseOutForPrefixStateInjectionCodeFix : PatchClassCodeFixProvider<UseOutForPrefixStateInjection> { }

public readonly struct UseOutForPrefixStateInjection : IHarmonyCodeFix
{
    const string Title = "Use out for __state injection";

    public DiagnosticId DiagnosticId => DiagnosticId.MHA016;

    public string GetTitle(params object[] _) => Title;
    public string GetEquivalenceKey(params object[] formatArgs) => this.GetTitle(formatArgs);

    async IAsyncEnumerable<CodeAction> IHarmonyCodeFix.GetActionsAsync(
        Diagnostic diagnostic,
        Document document,
        SemanticModel sm,
        [EnumeratorCancellation] CancellationToken ct)
    {
        if (diagnostic.Location is not { } location ||
            await document.FindSyntaxNodeAsync<ParameterSyntax>(location, ct).ConfigureAwait(false) is not { } ps)
            yield break;

        yield return CodeAction.Create(Title, ct => SetOutKeywordAsync(document, ps, ct));
    }

    private static async Task<Document> SetOutKeywordAsync(Document document, ParameterSyntax ps, CancellationToken ct)
    {
        var newModifiers = 
            (ps.Modifiers.FirstOrDefault(m => m.Kind() is SyntaxKind.RefKeyword) is { } token ? 
                ps.Modifiers.Remove(token) :
                ps.Modifiers)
            .Add(SyntaxFactory.Token(SyntaxKind.OutKeyword));

        var newPs = ps.WithModifiers(newModifiers);

        if ((await document.GetSyntaxRootAsync(ct).ConfigureAwait(false))?.ReplaceNode(ps, newPs) is not { } newRoot)
            return document;

        return document.WithSyntaxRoot(newRoot);
    }
}
