namespace MicroUtils.HarmonyAnalyzers.Test;

internal class PatchClassAnalyzerVerifier : AnalyzerVerifier<PatchClassAnalyzer, PatchClassAnalyzerTest, DefaultVerifier>
{
    public static Task VerifyAnalyzerAsync(
        string testCode,
        DiagnosticResult expected,
        ImmutableArray<string> disabledDiagnostics = default) =>
        VerifyAnalyzerAsync(testCode, [expected], disabledDiagnostics);

    public static Task VerifyAnalyzerAsync(
        string testCode,
        IEnumerable<DiagnosticResult> expected,
        ImmutableArray<string> disabledDiagnostics = default)
    {
        var test = new PatchClassAnalyzerTest
        {
            TestCode = testCode
        };

        return VerifyAnalyzerAsync(test, expected, disabledDiagnostics);
    }

    public static Task VerifyAnalyzerAsync(
        TestSources sources,
        DiagnosticResult expected,
        ImmutableArray<string> disabledDiagnostics = default) =>
        VerifyAnalyzerAsync(sources, [expected], disabledDiagnostics);

    public static Task VerifyAnalyzerAsync(
        TestSources sources,
        IEnumerable<DiagnosticResult> expected,
        ImmutableArray<string> disabledDiagnostics = default)
    {
        var test = new PatchClassAnalyzerTest
        {
            TargetClassCode = sources.TargetClassSource,
            TestPatchCode = sources.TestPatchSource
        };

        return VerifyAnalyzerAsync(test, expected, disabledDiagnostics);
    }

    private static Task VerifyAnalyzerAsync(
        PatchClassAnalyzerTest test,
        IEnumerable<DiagnosticResult> expected,
        ImmutableArray<string> disabledDiagnostics)
    {
        if (!disabledDiagnostics.IsDefaultOrEmpty)
            test.DisabledDiagnostics.AddRange(disabledDiagnostics);

        test.ExpectedDiagnostics.AddRange(expected);

        return test.RunAsync(CancellationToken.None);
    }
}
