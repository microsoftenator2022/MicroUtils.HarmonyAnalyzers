using MicroUtils.HarmonyAnalyzers.CodeFixes.MHA020;

namespace MicroUtils.HarmonyAnalyzers.Test.MHA020;

using Verify = PatchClassCodeFixVerifier<MakePatchMethodStatic, MakePatchMethodStatic.Descriptor>;


[TestClass]
public class FixNonStaticPatchMethod
{
    const string targetClass = """
class TargetType
{
    public static void TargetMethod() {}
}
""";

    [TestMethod]
    public async Task TestCodeFix()
    {
        const string testPatch = """
using HarmonyLib;

[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
class Patch
{
    void {|#0:Postfix|}() {}
}

""";
        const string fixedPatch = """
using HarmonyLib;

[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
class Patch
{
    static void {|#0:Postfix|}() {}
}

""";

        await Verify.VerifyCodeFixAsync(
            new TestSources(targetClass, testPatch, fixedPatch),
            Verify.Diagnostic().WithLocation(0));
    }
}
