using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;

namespace MicroUtils.HarmonyAnalyzers.CodeFixes;
public interface IHarmonyCodeFix
{
    IAsyncEnumerable<CodeAction> GetActionsAsync(
        Diagnostic diagnostic,
        Document document,
        SemanticModel semanticModel,
        CancellationToken cancellationToken);

    DiagnosticId DiagnosticId { get; }

    string GetEquivalenceKey(params object[] formatArgs);
    string GetTitle(params object[] formatArgs);
}

public static class HarmonyCodeFix
{
    public delegate IAsyncEnumerable<CodeAction> GetActionsAsynx(
        Diagnostic diagnostic,
        Document document,
        SemanticModel semanticModel,
        CancellationToken cancellationToken);

    internal static (DiagnosticId, GetActionsAsynx) Get<TCodeFix>() where TCodeFix : struct, IHarmonyCodeFix =>
        (default(TCodeFix).DiagnosticId, default(TCodeFix).GetActionsAsync);

    public static string GetDiagnosticId<TCodeFix>() where TCodeFix : struct, IHarmonyCodeFix =>
        default(TCodeFix).DiagnosticId.ToString();

    public static string GetEquivalenceKey<TCodeFix>(params object[] formatArgs)
        where TCodeFix : struct, IHarmonyCodeFix =>
        default(TCodeFix).GetEquivalenceKey(formatArgs);
}