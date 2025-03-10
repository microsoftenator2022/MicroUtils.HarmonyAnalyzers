namespace MicroUtils.HarmonyAnalyzers.Test.MHA011;

using Verify = PatchClassAnalyzerVerifier;

[TestClass]
public class PatchTypeAttributeConflict
{
    const string targetClass = """
class TargetType
{
    public void TargetMethod() {}
}
""";

    [TestMethod]
    public async Task ReportConflictingPatchTypes()
    {
        const string testPatch = """
using HarmonyLib;

[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
static class Patch
{
    [{|#0:HarmonyPrefix|}]
    [{|#1:HarmonyFinalizer|}]
    [{|#2:HarmonyTranspiler|}]
    [{|#3:HarmonyReversePatch|}]
    static void {|#4:Postfix|}() {}
}
""";

        await Verify.VerifyAnalyzerAsync(
            new TestSources(targetClass, testPatch),
            [
                Verify.Diagnostic("MHA011").WithLocation(0),
                Verify.Diagnostic("MHA011").WithLocation(1),
                Verify.Diagnostic("MHA011").WithLocation(2),
                Verify.Diagnostic("MHA011").WithLocation(3),
                Verify.Diagnostic("MHA011").WithLocation(4)
            ]);
    }
}
