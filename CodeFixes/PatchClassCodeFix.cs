using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

using MicroUtils.HarmonyAnalyzers;

namespace MicroUtils.HarmonyAnalyzers.CodeFixes;

public interface IPatchClassCodeFixDescriptor
{
    DiagnosticId Id { get; }

    string GetEquivalenceKey(params object[] formatArgs);
    string GetTitle(params object[] formatArgs);
}

public abstract class PatchClassCodeFixProvider<TDescriptor> : CodeFixProvider
    where TDescriptor : struct, IPatchClassCodeFixDescriptor
{
    public static DiagnosticId Id => default(TDescriptor).Id;
    public static string GetEquivalenceKey(params object[] formatArgs) => default(TDescriptor).GetEquivalenceKey(formatArgs);
    public static string GetTitle(params object[] formatArgs) => default(TDescriptor).GetTitle(formatArgs);

    private protected PatchClassCodeFixProvider() { }

    public override FixAllProvider? GetFixAllProvider() => null;
    
    public override ImmutableArray<string> FixableDiagnosticIds => [Id.ToString()];

    public abstract IAsyncEnumerable<CodeAction> GetActionsAsync(
        Diagnostic diagnostic,
        Document document,
        SemanticModel semanticModel,
        CancellationToken cancellationToken);

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
            var actions = this.GetActionsAsync(
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
