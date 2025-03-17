using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MicroUtils.HarmonyAnalyzers.CodeFixes.MHA008;

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class AddRefKeywordCodeFix : PatchClassCodeFixProvider<AddRefKeywordCodeFix.Descriptor>
{
    const string Title = "Add ref keyword";

    public readonly struct Descriptor : IPatchClassCodeFixDescriptor
    {
        public DiagnosticId Id => DiagnosticId.MHA008;

        public string GetTitle(params object[] _) => Title;
        public string GetEquivalenceKey(params object[] formatArgs) => this.GetTitle(formatArgs);
    }

    public override async IAsyncEnumerable<CodeAction> GetActionsAsync(
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
