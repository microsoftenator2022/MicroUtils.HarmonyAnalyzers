namespace MicroUtils.HarmonyAnalyzers.Test;

internal class PatchClassCodeFixVerifier<TCodeFix, TDescriptor> : PatchClassCodeFixVerifier<
    TCodeFix,
    TDescriptor,
    PatchClassCodeFixTest<TCodeFix, TDescriptor>>
    where TCodeFix : PatchClassCodeFixProvider<TDescriptor>, new()
    where TDescriptor : struct, IPatchClassCodeFixDescriptor;

internal class PatchClassCodeFixVerifier<TCodeFix, TDescriptor, TTest> : CodeFixVerifier<
    PatchClassAnalyzer,
    TCodeFix,
    TTest,
    DefaultVerifier>
    where TCodeFix : PatchClassCodeFixProvider<TDescriptor>, new()
    where TDescriptor : struct, IPatchClassCodeFixDescriptor
    where TTest : PatchClassCodeFixTest<TCodeFix, TDescriptor>, new()
{
    public static new DiagnosticResult Diagnostic() =>
        Diagnostic(PatchClassCodeFixProvider<TDescriptor>.Id.ToString())
            .WithMessage(null);

    public static string GetEquivalenceKey(params object[] formatArgs) =>
        PatchClassCodeFixProvider<TDescriptor>.GetEquivalenceKey(formatArgs);

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
        var test = new TTest
        {
            TestCode = source,
            FixedCode = fixedSource
        };

        return VerifyCodeFixAsync(test: test, expected, codeActionKey, disabledDiagnostics);
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
        var test = new TTest();
        test.AddSources(sources);

        return VerifyCodeFixAsync(test, expected, codeActionKey, disabledDiagnostics);
    }

    private static Task VerifyCodeFixAsync(
        TTest test,
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
