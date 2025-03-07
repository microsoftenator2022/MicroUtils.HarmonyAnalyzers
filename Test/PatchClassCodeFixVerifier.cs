using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Testing;

namespace MicroUtils.HarmonyAnalyzers.Test;

internal class PatchClassCodeFixVerifier<TCodeFix, TCodeFixImpl> : CodeFixVerifier<
    PatchClassAnalyzer,
    TCodeFix,
    PatchClassCodeFixTest<TCodeFix>,
    DefaultVerifier>
    where TCodeFix : PatchClassCodeFixProvider<TCodeFixImpl>, new()
    where TCodeFixImpl : struct, IHarmonyCodeFix
{
    public static class Default
    {
        public static DiagnosticResult Diagnostic() =>
            PatchClassCodeFixVerifier<TCodeFix, TCodeFixImpl>
                .Diagnostic(HarmonyCodeFix.GetDiagnosticId<TCodeFixImpl>().ToString())
                .WithLocation(0)
                .WithMessage(null);
    }

    public static string GetEquivalenceKey(params object[] formatArgs) =>
        HarmonyCodeFix.GetEquivalenceKey<TCodeFixImpl>(formatArgs);

    public static Task VerifyCodeFixAsync(
        string source,
        string fixedSource,
        string? codeActionKey = null,
        ImmutableArray<string> disabledDiagnostics = default)
        => VerifyCodeFixAsync(source, Default.Diagnostic(), fixedSource, codeActionKey, disabledDiagnostics);

    public static Task VerifyCodeFixAsync(
        string source,
        DiagnosticResult expected,
        string fixedSource,
        string? codeActionKey = null,
        ImmutableArray<string> disabledDiagnostics = default)
        => VerifyCodeFixAsync(source, [expected], fixedSource, codeActionKey, disabledDiagnostics);

    public static Task VerifyCodeFixAsync(
        string source,
        IEnumerable<DiagnosticResult> expected,
        string fixedSource,
        string? codeActionKey = null,
        ImmutableArray<string> disabledDiagnostics = default)
    {
        var test = new PatchClassCodeFixTest<TCodeFix>
        {
            TestCode = source,
            FixedCode = fixedSource
        };

        if (!disabledDiagnostics.IsDefaultOrEmpty)
        {
            test.DisabledDiagnostics.AddRange(disabledDiagnostics);
        }

        if (codeActionKey is not null)
            test.CodeActionEquivalenceKey = codeActionKey;

        test.ExpectedDiagnostics.AddRange(expected);
        
        return test.RunAsync(CancellationToken.None);
    }
}
