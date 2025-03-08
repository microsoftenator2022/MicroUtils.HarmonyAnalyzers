using MicroUtils.HarmonyAnalyzers.CodeFixes.MHA001;

namespace MicroUtils.HarmonyAnalyzers.Test.MHA001;

using Verify = PatchClassCodeFixVerifier<AddHarmonyPatchAttributeCodeFix, AddHarmonyPatchAttribute>;

[TestClass]
public class AddMissingClassAttribute
{
    const string targetClass = """
static class TargetType
{
    public static void TargetMethod() {}
}
""";

    [TestMethod]
    public async Task AddMissingAttributeToClass()
    {
        var test = """
using HarmonyLib;

static class {|#0:TypeName|}
{
    [HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
    static void {|#1:Postfix|}() { }
}
""";

        var testFix = """
using HarmonyLib;

[HarmonyPatch]
static class {|#0:TypeName|}
{
    [HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
    static void {|#1:Postfix|}() { }
}
""";
        await Verify.VerifyCodeFixAsync(
            new TestSources(targetClass, test, testFix),
            Verify.Diagnostic().WithLocation(0),
            Verify.GetEquivalenceKey());
    }
}
