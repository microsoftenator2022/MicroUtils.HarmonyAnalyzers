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

using static DiagnosticId;
using static SyntaxFactory;

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class PatchClassCodeFixes : CodeFixProvider
{
    public override FixAllProvider GetFixAllProvider() => null!;

    public override ImmutableArray<string> FixableDiagnosticIds =>
    [
        nameof(MHA001),
        nameof(MHA002)
    ];

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var syntax = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        if (syntax is null) return;

        foreach (var diagnostic in context.Diagnostics)
        {
            if (!Enum.TryParse<DiagnosticId>(diagnostic.Id, out var id))
                continue;

            switch (id)
            {
                case MHA001:
                    if (syntax.FindNode(diagnostic.Location.SourceSpan) is not ClassDeclarationSyntax cds) continue;

                    context.RegisterCodeFix(MissingClassAttribute.AddHarmonyPatchAttribute(context.Document, cds), diagnostic);
                    break;

                case MHA002:
                    if (syntax.FindNode(diagnostic.Location.SourceSpan) is not MethodDeclarationSyntax mds) continue;

                    foreach (var action in await MissingPatchTypeAttribute.GetFixes(context, diagnostic, mds))
                    {
                        context.RegisterCodeFix(action, diagnostic);
                    }
                    break;

                default:
                    break;
            }
        }
    }
}
