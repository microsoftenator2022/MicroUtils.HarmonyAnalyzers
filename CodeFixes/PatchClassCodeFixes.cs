using System;
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

using MicroUtils.HarmonyAnalyzers.CodeFixes.MHA001;
using MicroUtils.HarmonyAnalyzers.CodeFixes.MHA002;
using MicroUtils.HarmonyAnalyzers.CodeFixes.MHA003;
using MicroUtils.HarmonyAnalyzers.CodeFixes.MHA008;
using MicroUtils.HarmonyAnalyzers.CodeFixes.MHA012;
using MicroUtils.HarmonyAnalyzers.CodeFixes.MHA016;
using MicroUtils.HarmonyAnalyzers.CodeFixes.MHA017;
using MicroUtils.HarmonyAnalyzers.CodeFixes.MHA018;

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

    static readonly ImmutableDictionary<DiagnosticId, HarmonyCodeFix.GetActionsAsynx> CodeFixes =
        new []
        {
            HarmonyCodeFix.Get<AddHarmonyPatchAttribute>(),
            HarmonyCodeFix.Get<AddPatchTypeAttribute>(),
            HarmonyCodeFix.Get<AddMissingMethodType>(),
            HarmonyCodeFix.Get<AddRefKeyword>(),
            HarmonyCodeFix.Get<RemoveResultInjection>(),
            HarmonyCodeFix.Get<UseOutForPrefixStateInjection>(),
            HarmonyCodeFix.Get<ReplaceIndexInjectionWithName>(),
            HarmonyCodeFix.Get<FixMethodSignature>()

        }
        .Select(Util.ToKeyValuePair)
        .ToImmutableDictionary();

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        if (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) is not { } syntaxRoot)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

        if (semanticModel is null)
            return;

        var compilation = semanticModel.Compilation.WithAllMembers();

        if (await context.Document.GetSyntaxTreeAsync(context.CancellationToken).ConfigureAwait(false) is not { } syntaxTree)
            return;

        semanticModel = compilation.GetSemanticModel(syntaxTree, true);

        foreach (var diagnostic in context.Diagnostics)
        {
            if (!Enum.TryParse<DiagnosticId>(diagnostic.Id, out var id))
                continue;

            if (CodeFixes.TryGetValue(id, out var getActionsAsync))
            {
                var actions = getActionsAsync(
                    diagnostic,
                    context.Document,
                    semanticModel,
                    context.CancellationToken)
                    .ConfigureAwait(false);

                await foreach (var action in actions)
                {
                    context.RegisterCodeFix(action, diagnostic);
                }
            }
        }
    }
}
