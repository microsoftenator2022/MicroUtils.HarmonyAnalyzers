using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MicroUtils.HarmonyAnalyzers.CodeFixes.MHA020;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class MakePatchMethodStatic : PatchClassCodeFixProvider<MakePatchMethodStatic.Descriptor>
{
    public readonly struct Descriptor : IPatchClassCodeFixDescriptor
    {
        public DiagnosticId Id => DiagnosticId.MHA020;
        public string GetTitle(params object[] formatArgs) => string.Format("Make {0} static", formatArgs);

        public string GetEquivalenceKey(params object[] formatArgs) => this.GetTitle(formatArgs);
    }

    public async override IAsyncEnumerable<CodeAction> GetActionsAsync(
        Diagnostic diagnostic,
        Document document,
        SemanticModel _,
        [EnumeratorCancellation] CancellationToken ct)
    {
        if (diagnostic.Location is not { } location ||
            await document.FindSyntaxNodeAsync<MethodDeclarationSyntax>(location, ct).ConfigureAwait(false) is not { } mds)
            yield break;

        var title = GetTitle(mds.Identifier.ToString());
        
        yield return CodeAction.Create(
            title,
            ct => FixMethodAsync(document, mds, ct),
            equivalenceKey: title, CodeActionPriority.High);
    }

    async Task<Document> FixMethodAsync(
        Document document,
        MethodDeclarationSyntax mds,
        CancellationToken ct)
    {
        if (await document.GetSyntaxRootAsync(ct).ConfigureAwait(false) is not { } sr)
            return document;

        return document.WithSyntaxRoot(sr.ReplaceNode(mds,
            mds.WithoutLeadingTrivia()
            .AddModifiers(Token(SyntaxKind.StaticKeyword).WithTrailingTrivia(Whitespace(" ")))
            .WithLeadingTrivia(mds.GetLeadingTrivia())));
    }
}
