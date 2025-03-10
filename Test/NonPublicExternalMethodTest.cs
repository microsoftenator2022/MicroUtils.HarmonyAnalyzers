namespace MicroUtils.HarmonyAnalyzers.Test;

using Verify = PatchClassAnalyzerVerifier;

[TestClass]
public class NonPublicExternalMethodTest
{
    [TestMethod]
    public async Task CanSeeNonPublicExternalMetadata()
    {
        const string testPatch = """
using HarmonyLib;

[HarmonyPatch(typeof(Traverse), "Resolve")]
static class Patch
{
    static void Postfix() {}
}
""";

        await Verify.VerifyAnalyzerAsync(testPatch, []);
    }
}
