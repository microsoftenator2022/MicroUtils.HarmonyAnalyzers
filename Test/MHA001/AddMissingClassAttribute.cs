using MicroUtils.HarmonyAnalyzers.CodeFixes.MHA001;

namespace MicroUtils.HarmonyAnalyzers.Test.MHA001;

using Verify = PatchClassCodeFixVerifier<AddHarmonyPatchAttributeCodeFix, AddHarmonyPatchAttribute>;

[TestClass]
public class AddMissingClassAttribute
{
    [TestMethod]
    public async Task AddMissingAttributeToClass()
    {
        var test = """
using HarmonyLib;

static class TargetType
{
    public static void TargetMethod() {}
}

static class {|#0:TypeName|}
{
    [HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
    static void {|#1:Postfix|}() { }
}
""";

        var testFix = """
using HarmonyLib;

static class TargetType
{
    public static void TargetMethod() {}
}

[HarmonyPatch]
static class {|#0:TypeName|}
{
    [HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
    static void {|#1:Postfix|}() { }
}
""";
        await Verify.VerifyCodeFixAsync(
            test,
            testFix,
            Verify.GetEquivalenceKey());
    }
}
