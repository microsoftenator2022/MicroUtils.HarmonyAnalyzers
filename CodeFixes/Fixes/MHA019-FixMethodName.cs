using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MicroUtils.HarmonyAnalyzers.CodeFixes.MHA019;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class FixMethodName : PatchClassCodeFixProvider<FixMethodName.Descriptor>
{
    public readonly struct Descriptor : IPatchClassCodeFixDescriptor
    {
        public DiagnosticId Id => DiagnosticId.MHA019;
        public string GetTitle(params object[] _) => "Fix method name";

        public string GetEquivalenceKey(params object[] formatArgs) => this.GetTitle(formatArgs);
    }

    public override async IAsyncEnumerable<CodeAction> GetActionsAsync(
        Diagnostic diagnostic,
        Document document,
        SemanticModel sm,
        [EnumeratorCancellation] CancellationToken ct)
    {
        if (diagnostic.Location is not { } location ||
            !diagnostic.Properties.TryGetValue(nameof(PatchMethodData.PatchType), out var patchType) ||
            patchType is null ||
            await document.FindSyntaxNodeAsync<MethodDeclarationSyntax>(location, ct).ConfigureAwait(false) is not { } mds)
            yield break;

        yield return CodeAction.Create(
            GetTitle(),
            ct => FixMethodNameAsync(document, mds, patchType, ct),
            equivalenceKey: GetEquivalenceKey());
    }

    static async Task<Document> FixMethodNameAsync(
        Document document,
        MethodDeclarationSyntax mds,
        string patchType,
        CancellationToken ct)
    {
        if ((await document.GetSyntaxRootAsync(ct).ConfigureAwait(false))
            ?.ReplaceNode(mds, mds.WithIdentifier(Identifier(default, patchType, default))) is { } newRoot)
            return document.WithSyntaxRoot(newRoot);

        return document;
    }
}
