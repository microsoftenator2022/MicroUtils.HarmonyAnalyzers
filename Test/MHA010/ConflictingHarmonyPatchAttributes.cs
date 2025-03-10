namespace MicroUtils.HarmonyAnalyzers.Test.MHA010;

using Verify = PatchClassAnalyzerVerifier;

[TestClass]
public class ConflictingHarmonyPatchAttributes
{
    const string targetClass = """
class TargetType
{
    public string TargetMethod() => "";
    public int TargetProperty => 0;
}
""";

    [TestMethod]
    public async Task ReportPathMethodAttributesConflict()
    {
        const string testPatch = """
using HarmonyLib;

[{|#0:HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))|}]
static class Patch
{
    [{|#1:HarmonyPatch(nameof(TargetType.TargetProperty), MethodType.Getter)|}]
    static void Postfix() {}
}
""";

        await Verify.VerifyAnalyzerAsync(
            new TestSources(targetClass, testPatch),
            [
                Verify.Diagnostic("MHA010").WithLocation(0),
                Verify.Diagnostic("MHA010").WithLocation(1)
            ]);
    }
}
