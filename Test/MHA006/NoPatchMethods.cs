namespace MicroUtils.HarmonyAnalyzers.Test.MHA006;

using Verify = PatchClassAnalyzerVerifier;

[TestClass]
public class NoPatchMethods
{
    [TestMethod]
    public async Task ReportNoPatchMethods()
    {
        const string testPatchClass = """
using HarmonyLib;

[HarmonyPatch]
static class {|#0:Patch|}
{
    static void PatchMethod() {}
}
""";

        await Verify.VerifyAnalyzerAsync(
            testPatchClass,
            Verify.Diagnostic("MHA006").WithLocation(0).WithMessage(null));
    }
}
