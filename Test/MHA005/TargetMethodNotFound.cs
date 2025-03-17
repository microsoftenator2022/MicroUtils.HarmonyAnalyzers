namespace MicroUtils.HarmonyAnalyzers.Test.MHA005;

using Verify = PatchClassAnalyzerVerifier;

[TestClass]
public class TargetMethodNotFound
{
    const string targetClass = """
class TargetType
{
    public string TargetMethod() => "";
}
""";

    [TestMethod]
    public async Task ReportTargetMethodNotFound()
    {
        const string testPatch = """
using HarmonyLib;

[HarmonyPatch(typeof(TargetType))]
static class Patch
{
    [HarmonyPostfix]
    static void {|#0:PostfixMethod|}() {}
}
""";

        await Verify.VerifyAnalyzerAsync(
            new TestSources(targetClass, testPatch),
            Verify.Diagnostic("MHA005").WithLocation(0));
    }

    [TestMethod]
    public async Task DoNotReportForSuccessfulMatch()
    {
        const string testPatch = """
using HarmonyLib;

[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
static class Patch
{
    [HarmonyPostfix]
    static void {|#0:PostfixMethod|}() {}
}
""";
        
        await Verify.VerifyAnalyzerAsync(
            new TestSources(targetClass, testPatch),
            []);

    }
}
