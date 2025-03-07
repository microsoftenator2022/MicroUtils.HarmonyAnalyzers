namespace MicroUtils.HarmonyAnalyzers.Test;

internal readonly record struct TestSources(string TargetClassSource, string TestPatchSource)
{
    public readonly string? FixedPatchSource { get; init; }

    public TestSources(string targetClassSource, string testPatchSource, string fixedPatchSource) : this(targetClassSource, testPatchSource)
    {
        this.FixedPatchSource = fixedPatchSource;
    }
}
