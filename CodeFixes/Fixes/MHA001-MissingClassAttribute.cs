﻿using System;
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

using static SyntaxFactory;

internal class MissingClassAttribute
{
    const string Title = "Add HarmonyPatch Attribute";

    internal static CodeAction AddHarmonyPatchAttribute(Document document, ClassDeclarationSyntax node) =>
        CodeAction.Create(
            Title,
            ct => AddHarmonyPatchAttribute(document, node, ct),
            equivalenceKey: Title);

    private static async Task<Document> AddHarmonyPatchAttribute(Document document, ClassDeclarationSyntax cds, CancellationToken ct)
    {
        if (await document.GetSemanticModelAsync(ct) is not { } sm)
            return document;

        if (sm.Compilation.GetType(HarmonyConstants.Namespace_HarmonyLib, HarmonyConstants.Attribute_HarmonyLib_HarmonyPatch, ct) is not { } patchAttributeType)
            return document;

        var newCds = cds.AddAttributeLists(
            AttributeList(
                SeparatedList(
                [
                    Attribute(
                        IdentifierName(patchAttributeType.Name)
                    )
                ])
            )
        );

        if ((await document.GetSyntaxRootAsync(ct))?.ReplaceNode(cds, newCds) is not { } newRoot)
            return document;

        return document.WithSyntaxRoot(newRoot);
    }
}
