using MicroUtils.HarmonyAnalyzers.CodeFixes.MHA002;

namespace MicroUtils.HarmonyAnalyzers.Test.MHA002;

using Verify = PatchClassCodeFixVerifier<AddPatchTypeAttributeCodeFix, AddPatchTypeAttributeCodeFix.Descriptor>;

public partial class AddMissingPatchMethodTypeAttribute
{

    [TestMethod]
    public async Task AddPrefixAttributeToBoolReturnMethod()
    {
        var test = """
using HarmonyLib;

[HarmonyPatch]
static class PatchClass
{
    [HarmonyPatch(typeof(TargetClass), nameof(TargetClass.TargetMethod))]
    static bool {|#0:PatchMethod|}() => true;
}
""";

        var testfix = """
using HarmonyLib;

[HarmonyPatch]
static class PatchClass
{
    [HarmonyPatch(typeof(TargetClass), nameof(TargetClass.TargetMethod))]
    [HarmonyPrefix]
    static bool {|#0:PatchMethod|}() => true;
}
""";
        await Verify.VerifyCodeFixAsync(
            new TestSources(targetClass, test, testfix),
            Verify.Diagnostic().WithLocation(0),
            GetEquivalenceKey(HarmonyPatchType.Prefix));
    }
}
