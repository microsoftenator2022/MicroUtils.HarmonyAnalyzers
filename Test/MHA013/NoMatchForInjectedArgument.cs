namespace MicroUtils.HarmonyAnalyzers.Test.MHA013;

using Verify = PatchClassAnalyzerVerifier;

[TestClass]
public class NoMatchForInjectedArgument
{
    const string targetClass = """
class TargetType
{
    public void TargetMethod(string s) {}
}
""";

    [TestMethod]
    public async Task ValidInjectedArgument()
    {
        const string testPatch = """
using HarmonyLib;

[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
static class Patch
{
    static void Postfix(string s) {}
}
""";

        await Verify.VerifyAnalyzerAsync(
            new TestSources(targetClass, testPatch),
            []);
    }

    [TestMethod]
    public async Task InvalidInjectedArgument()
    {
        const string testPatch = """
using HarmonyLib;

[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
static class Patch
{
    static void Postfix(string {|#0:str|}) {}
}
""";

        await Verify.VerifyAnalyzerAsync(
            new TestSources(targetClass, testPatch),
            Verify.Diagnostic("MHA013").WithLocation(0));
    }
}
