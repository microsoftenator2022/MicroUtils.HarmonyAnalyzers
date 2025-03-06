using System;
using System.Collections.Generic;
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

namespace MicroUtils.HarmonyAnalyzers.CodeFixes.MHA017;

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class ReplaceIndexInjectionWithNameCodeFix : PatchClassCodeFixProvider<ReplaceIndexInjectionWithName> { }

public readonly struct ReplaceIndexInjectionWithName : IHarmonyCodeFix
{
    public DiagnosticId DiagnosticId => DiagnosticId.MHA017;

    public string GetTitle(params object[] formatArgs) => string.Format("Replace '{0}' with '{1}'", formatArgs);
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

        if (!diagnostic.Properties.TryGetValue("ParameterName", out var name) || name is null)
            yield break;

        var title = this.GetTitle(ps.Identifier, name);

        yield return CodeAction.Create(
            /*$"Replace '{ps.Identifier}' with '{name}'",*/
            title,
            ct => ReplaceParameterNameAsync(document, ps, name, ct),
            equivalenceKey: title);
    }

    private static async Task<Document> ReplaceParameterNameAsync(Document document, ParameterSyntax ps, string parameterName, CancellationToken ct)
    {
        var newPs = ps.WithIdentifier(SyntaxFactory.Identifier(parameterName));

        if ((await document.GetSyntaxRootAsync(ct).ConfigureAwait(false))?.ReplaceNode(ps, newPs) is not { } newRoot)
            return document;

        return document.WithSyntaxRoot(newRoot);
    }
}
