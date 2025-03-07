namespace MicroUtils.HarmonyAnalyzers.Test.MHA004;

using Verify = PatchClassAnalyzerVerifier;

[TestClass]
public class AmbiguousTargetMethod
{
    const string targetClass = """
class TargetType
{
    public string TargetMethod() => "";
    public string TargetMethod(string s) => "";
}
""";

    [TestMethod]
    public async Task ReportAmbiguousTargetMethod()
    {
        const string testPatch = """
using HarmonyLib;

[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
static class Patch
{
    static void {|#0:Postfix|}() { }
}
""";
        await Verify.VerifyAnalyzerAsync(
            new TestSources(targetClass, testPatch),
            Verify.Diagnostic("MHA004").WithLocation(0));
    }
}
