using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis.CodeFixes;

using MicroUtils.HarmonyAnalyzers;

namespace MicroUtils.HarmonyAnalyzers.CodeFixes;

public abstract class PatchClassCodeFixProvider<TCodeFixImpl> : CodeFixProvider
    where TCodeFixImpl : struct, IHarmonyCodeFix
{
    private protected PatchClassCodeFixProvider() { }

    public override FixAllProvider? GetFixAllProvider() => null;
    
    public override ImmutableArray<string> FixableDiagnosticIds => [Id.ToString()];

    readonly static DiagnosticId Id = HarmonyCodeFix.GetDiagnosticId<TCodeFixImpl>();

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        if (!context.Diagnostics.Select(d => d.Id).ContainsAny(this.FixableDiagnosticIds))
            return;

        if (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) is not { } syntaxRoot)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

        if (semanticModel is null)
            return;

        var compilation = semanticModel.Compilation.WithAllMembers();

        if (await context.Document.GetSyntaxTreeAsync(context.CancellationToken).ConfigureAwait(false) is not { } syntaxTree)
            return;

        semanticModel = compilation.GetSemanticModel(syntaxTree, true);

        foreach (var diagnostic in context.Diagnostics.Where(d => Enum.TryParse<DiagnosticId>(d.Id, out var id) && id == Id))
        {
            var actions = HarmonyCodeFix.GetFixActions<TCodeFixImpl>()(
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
