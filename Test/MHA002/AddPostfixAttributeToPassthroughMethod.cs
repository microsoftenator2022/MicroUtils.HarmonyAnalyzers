using MicroUtils.HarmonyAnalyzers.CodeFixes.MHA002;

namespace MicroUtils.HarmonyAnalyzers.Test.MHA002;

using Verify = PatchClassCodeFixVerifier<AddPatchTypeAttributeCodeFix, AddPatchTypeAttributeCodeFix.Descriptor>;

[TestClass]
public partial class AddMissingPatchMethodTypeAttribute
{

    [TestMethod]
    public async Task AddPostfixAttributeToPassthroughMethod()
    {
        var testPatch = """
using HarmonyLib;

[HarmonyPatch]
static class PatchClass
{
    [HarmonyPatch(typeof(TargetClass), nameof(TargetClass.TargetMethod))]
    static string {|#0:PatchMethod|}(string _) => "";
}
""";

        var fixedPatch = """
using HarmonyLib;

[HarmonyPatch]
static class PatchClass
{
    [HarmonyPatch(typeof(TargetClass), nameof(TargetClass.TargetMethod))]
    [HarmonyPostfix]
    static string {|#0:PatchMethod|}(string _) => "";
}
""";
        await Verify.VerifyCodeFixAsync(
            new TestSources(targetClass, testPatch, fixedPatch),
            Verify.Diagnostic().WithLocation(0),
            GetEquivalenceKey(HarmonyPatchType.Postfix));
    }
}
