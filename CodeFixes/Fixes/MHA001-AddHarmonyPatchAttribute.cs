using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MicroUtils.HarmonyAnalyzers.CodeFixes.MHA001;

using static SyntaxFactory;

internal readonly struct AddHarmonyPatchAttribute : IHarmonyCodeFix
{
    const string Title = "Add HarmonyPatch Attribute";

    public DiagnosticId DiagnosticId => DiagnosticId.MHA001;

    public async IAsyncEnumerable<CodeAction> GetActionsAsync(
        Diagnostic diagnostic,
        Document document,
        SemanticModel sm,
        [EnumeratorCancellation] CancellationToken ct)
    {
        if (diagnostic.Location is not { } location ||
            await document.FindSyntaxNodeAsync<ClassDeclarationSyntax>(location, ct).ConfigureAwait(false) is not { } cds)
            yield break;

        yield return CodeAction.Create(
            Title,
            ct => AddHarmonyPatchAttributeAsync(document, cds, sm, ct),
            equivalenceKey: Title);
    }

    private static async Task<Document> AddHarmonyPatchAttributeAsync(
        Document document,
        ClassDeclarationSyntax cds,
        SemanticModel sm,
        CancellationToken ct)
    {
        if (sm.Compilation.GetType(
            HarmonyConstants.Namespace_HarmonyLib,
            HarmonyConstants.Attribute_HarmonyLib_HarmonyPatch, ct) is not { } patchAttributeType)
            return document;

        var newCds = cds.AddAttributeLists(
            AttributeList(
                SeparatedList(
                [
                    Attribute(
                        IdentifierName(patchAttributeType.ToMinimalDisplayString(sm, cds.SpanStart))
                    )
                ])
            )
        );

        if ((await document.GetSyntaxRootAsync(ct).ConfigureAwait(false))?.ReplaceNode(cds, newCds) is not { } newRoot)
            return document;

        return document.WithSyntaxRoot(newRoot);
    }
}
