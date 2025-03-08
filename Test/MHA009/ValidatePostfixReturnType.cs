namespace MicroUtils.HarmonyAnalyzers.Test.MHA009;

using Verify = PatchClassAnalyzerVerifier;

[TestClass]
public class ValidatePostfixReturnType
{
    const string targetClass = """
class TargetType
{
    public string TargetMethod() => "";
}
""";

    [TestMethod]
    public async Task ValidReturnTypes()
    {
        const string testPatch = """
using HarmonyLib;

[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
static class Patch
{
    [HarmonyPostfix]
    static void VoidReturn() {}

    [HarmonyPostfix]
    static string StringReturn(string _) => "";
}
""";

        await Verify.VerifyAnalyzerAsync(
            new TestSources(targetClass, testPatch),
            []);
    }

    [TestMethod]
    public async Task InvalidReturnType()
    {
        const string testPatch = """
using HarmonyLib;

[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
static class Patch
{
    static {|#0:bool|} Postfix() => true;
}
""";

        await Verify.VerifyAnalyzerAsync(
            new TestSources(targetClass, testPatch),
            Verify.Diagnostic("MHA009").WithLocation(0));
    }
}
