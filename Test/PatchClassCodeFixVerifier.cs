namespace MicroUtils.HarmonyAnalyzers.Test;

internal class PatchClassCodeFixVerifier<TCodeFix, TCodeFixImpl> : CodeFixVerifier<
    PatchClassAnalyzer,
    TCodeFix,
    PatchClassCodeFixTest<TCodeFix>,
    DefaultVerifier>
    where TCodeFix : PatchClassCodeFixProvider<TCodeFixImpl>, new()
    where TCodeFixImpl : struct, IHarmonyCodeFix
{
    public static new DiagnosticResult Diagnostic() =>
        Diagnostic(HarmonyCodeFix.GetDiagnosticId<TCodeFixImpl>().ToString())
            .WithMessage(null);

    public static string GetEquivalenceKey(params object[] formatArgs) =>
        HarmonyCodeFix.GetEquivalenceKey<TCodeFixImpl>(formatArgs);

    public static Task VerifyCodeFixAsync(
        string source,
        DiagnosticResult expected,
        string fixedSource,
        string? codeActionKey = null,
        ImmutableArray<string> disabledDiagnostics = default) =>
        VerifyCodeFixAsync(source, [expected], fixedSource, codeActionKey, disabledDiagnostics);

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

        return VerifyCodeFixAsync(test, expected, codeActionKey, disabledDiagnostics);
    }

    public static Task VerifyCodeFixAsync(
        TestSources sources,
        DiagnosticResult expected,
        string? codeActionKey = null,
        ImmutableArray<string> disabledDiagnostics = default) =>
        VerifyCodeFixAsync(sources, [expected], codeActionKey, disabledDiagnostics);

    public static Task VerifyCodeFixAsync(
        TestSources sources,
        IEnumerable<DiagnosticResult> expected,
        string? codeActionKey = null,
        ImmutableArray<string> disabledDiagnostics = default)
    {
        var test = new PatchClassCodeFixTest<TCodeFix>();
        test.AddSources(sources);

        return VerifyCodeFixAsync(test, expected, codeActionKey, disabledDiagnostics);
    }

    private static Task VerifyCodeFixAsync(
        PatchClassCodeFixTest<TCodeFix> test,
        IEnumerable<DiagnosticResult> expected,
        string? codeActionKey,
        ImmutableArray<string> disabledDiagnostics)
    {

        if (!disabledDiagnostics.IsDefaultOrEmpty)
            test.DisabledDiagnostics.AddRange(disabledDiagnostics);

        if (codeActionKey is not null)
            test.CodeActionEquivalenceKey = codeActionKey;

        test.ExpectedDiagnostics.AddRange(expected);

        return test.RunAsync(CancellationToken.None);
    }
}
