using MicroUtils.HarmonyAnalyzers.CodeFixes.MHA019;

namespace MicroUtils.HarmonyAnalyzers.Test.MHA019;

using Verify = PatchClassCodeFixVerifier<FixMethodName, FixMethodName.Descriptor>;

[TestClass]
public class FixPatchTypeMethodName
{
    const string targetClass = """
class TargetType
{
    public void TargetMethod() {}
}
""";

    [TestMethod]
    public async Task FixInvalidPatchMethodName()
    {
        const string testPatch = """
using HarmonyLib;

[HarmonyPatch]
static class Patch
{
    [HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
    static void {|#0:PostFix|}() {}
}
""";

        const string fixedPatch = """
using HarmonyLib;

[HarmonyPatch]
static class Patch
{
    [HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
    static void {|#0:Postfix|}() {}
}
""";

        await Verify.VerifyCodeFixAsync(
            new TestSources(targetClass, testPatch, fixedPatch),
            Verify.Diagnostic().WithLocation(0),
            disabledDiagnostics: ["MHA002"]);
    }
}
