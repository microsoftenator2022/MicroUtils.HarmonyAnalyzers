using MicroUtils.HarmonyAnalyzers.CodeFixes.MHA002;

namespace MicroUtils.HarmonyAnalyzers.Test.MHA002;

using Verify = PatchClassCodeFixVerifier<AddPatchTypeAttributeCodeFix, AddPatchTypeAttributeCodeFix.Descriptor>;

[TestClass]
public partial class AddMissingPatchMethodTypeAttribute
{
    [TestMethod]
    public async Task AddPostfixAttributeToPassthroughMethod()
    {
        var test = """
using HarmonyLib;

static class TargetClass
{
    public static string TargetMethod() => "";
}

[HarmonyPatch]
static class PatchClass
{
    [HarmonyPatch(typeof(TargetClass), nameof(TargetClass.TargetMethod))]
    static string {|#0:PatchMethod|}(string _) => "";
}
""";

        var testfix = """
using HarmonyLib;

static class TargetClass
{
    public static string TargetMethod() => "";
}

[HarmonyPatch]
static class PatchClass
{
    [HarmonyPatch(typeof(TargetClass), nameof(TargetClass.TargetMethod))]
    [HarmonyPostfix]
    static string {|#0:PatchMethod|}(string _) => "";
}
""";
        await Verify.VerifyCodeFixAsync(
            test,
            Verify.Diagnostic().WithLocation(0),
            testfix,
            GetEquivalenceKey(HarmonyPatchType.Postfix));
    }
}
