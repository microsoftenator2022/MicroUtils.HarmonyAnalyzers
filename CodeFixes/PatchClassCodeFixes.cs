using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MicroUtils.HarmonyAnalyzers.CodeFixes;

using static DiagnosticId;
using static SyntaxFactory;

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class PatchClassCodeFixes : CodeFixProvider
{
    public override FixAllProvider GetFixAllProvider() => null!;

    public override ImmutableArray<string> FixableDiagnosticIds =>
    [
        nameof(MHA001)
    ];

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        //var sm = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

        var syntax = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        if (syntax is null) return;

        foreach (var diagnostic in context.Diagnostics)
        {
            if (!Enum.TryParse<DiagnosticId>(diagnostic.Id, out var id))
                continue;

            switch (id)
            {
                case MHA001:
                    if (syntax.FindNode(diagnostic.Location.SourceSpan) is not ClassDeclarationSyntax node) continue;

                    context.RegisterCodeFix(CodeAction.Create(
                        Title,
                        ct => this.FixMHA001Async(context.Document, node, ct),
                        equivalenceKey: Title),
                        diagnostic);
                    break;

                default:
                    break;
            }
        }
    }

    const string Title = "Add HarmonyPatch Attribute";

    private async Task<Document> FixMHA001Async(Document document, ClassDeclarationSyntax cds, CancellationToken ct)
    {
        if (await document.GetSemanticModelAsync(ct) is not { } sm)
            return document;

        if (sm.Compilation.GetType(HarmonyConstants.Namespace_HarmonyLib, HarmonyConstants.Attribute_HarmonyLib_HarmonyPatch, ct) is not { } patchAttributeType)
            return document;

        var newCds = cds.WithAttributeLists(
            List(
            [
                AttributeList(
                    SeparatedList(
                    [
                        Attribute(
                            IdentifierName(patchAttributeType.Name)
                        )
                    ])
                )
            ]));

        if ((await document.GetSyntaxRootAsync(ct))?.ReplaceNode(cds, newCds) is not { } newRoot)
            return document;

        return document.WithSyntaxRoot(newRoot);
    }
}
