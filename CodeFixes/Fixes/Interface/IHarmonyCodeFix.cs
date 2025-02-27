using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;

namespace MicroUtils.HarmonyAnalyzers.CodeFixes;
internal interface IHarmonyCodeFix
{
    IAsyncEnumerable<CodeAction> GetActionsAsync(
        Diagnostic diagnostic,
        Document document,
        SemanticModel semanticModel,
        CancellationToken cancellationToken);

    DiagnosticId DiagnosticId { get; }
}

static class HarmonyCodeFix
{
    public delegate IAsyncEnumerable<CodeAction> GetActionsAsynx(
        Diagnostic diagnostic,
        Document document,
        SemanticModel semanticModel,
        CancellationToken cancellationToken);

    public static (DiagnosticId, GetActionsAsynx) Get<TCodeFix>() where TCodeFix : struct, IHarmonyCodeFix =>
        (default(TCodeFix).DiagnosticId, default(TCodeFix).GetActionsAsync);
}