﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class PatchClassCodeFixes : CodeFixProvider
{
    public override FixAllProvider? GetFixAllProvider() => null;

    public override ImmutableArray<string> FixableDiagnosticIds =>
    [
        nameof(MHA001),
        nameof(MHA002),
        nameof(MHA003),
        nameof(MHA008),
        nameof(MHA012),
        nameof(MHA016),
        nameof(MHA017),
        nameof(MHA018)
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
                case DiagnosticId.MHA001:
                {
                    if (syntax.FindNode(diagnostic.Location.SourceSpan) is not ClassDeclarationSyntax cds) continue;

                    context.RegisterCodeFix(MHA001.AddHarmonyPatchAttribute.GetAction(context.Document, cds), diagnostic);
                    break;
                }

                case DiagnosticId.MHA002:
                {
                    if (syntax.FindNode(diagnostic.Location.SourceSpan) is not MethodDeclarationSyntax mds) continue;

                    foreach (var action in await MHA002.AddPatchTypeAttribute.GetActions(context, diagnostic, mds)
                        .ConfigureAwait(false))
                    {
                        if (context.CancellationToken.IsCancellationRequested) return;

                        context.RegisterCodeFix(action, diagnostic);
                    }
                    break;
                }

                case DiagnosticId.MHA003:
                {
                    if (syntax.FindNode(diagnostic.Location.SourceSpan) is not MethodDeclarationSyntax mds) continue;

                    if (await MHA003.AddMissingMethodType
                        .GetActionAsync(context.Document, mds, diagnostic, context.CancellationToken)
                        .ConfigureAwait(false) is { } action)
                        context.RegisterCodeFix(action, diagnostic);

                    break;
                }

                case DiagnosticId.MHA008:
                {
                    if (diagnostic.AdditionalLocations.FirstOrDefault() is not { } paramLocation ||
                        syntax.FindNode(paramLocation.SourceSpan) is not ParameterSyntax ps) continue;

                    context.RegisterCodeFix(MHA008.AddRefKeyword.GetAction(context.Document, ps), diagnostic);

                    break;
                }

                case DiagnosticId.MHA012:
                {
                    if (diagnostic.Location is not { } paramLocation ||
                        syntax.FindNode(paramLocation.SourceSpan) is not ParameterSyntax ps) continue;

                    context.RegisterCodeFix(MHA012.RemoveResultInjection.GetAction(context.Document, ps), diagnostic);

                    break;
                }

                case DiagnosticId.MHA016:
                {
                    if (diagnostic.Location is not { } paramLocation ||
                        syntax.FindNode(paramLocation.SourceSpan) is not ParameterSyntax ps) continue;

                    context.RegisterCodeFix(MHA016.UseOutForPrefixStateInjection.GetAction(context.Document, ps), diagnostic);

                    break;
                }

                case DiagnosticId.MHA017:
                {
                    if (diagnostic.Location is not { } paramLocation ||
                        syntax.FindNode(paramLocation.SourceSpan) is not ParameterSyntax ps) continue;

                    if (MHA017.ReplaceIndexInjectionWithName.GetAction(context.Document, diagnostic, ps) is not { } action)
                        continue;

                    context.RegisterCodeFix(action, diagnostic);

                    break;
                }

                case DiagnosticId.MHA018:
                {
                    if (diagnostic.Location is not { } methodLocation ||
                        syntax.FindNode(methodLocation.SourceSpan) is not MethodDeclarationSyntax mds) continue;

                    if (await MHA018.FixMethodSignature
                            .GetActionAsync(context.Document, diagnostic, mds, context.CancellationToken)
                            .ConfigureAwait(false) is { } action)
                    {
                        context.RegisterCodeFix(action, diagnostic);
                    }
                    break;
                }
            }
        }
    }
}
