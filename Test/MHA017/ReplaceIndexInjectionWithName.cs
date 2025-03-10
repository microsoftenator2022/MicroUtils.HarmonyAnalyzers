using MicroUtils.HarmonyAnalyzers.CodeFixes.MHA017;

namespace MicroUtils.HarmonyAnalyzers.Test.MHA017;

using Verify = PatchClassCodeFixVerifier<
    ReplaceIndexInjectionWithNameCodeFix,
    ReplaceIndexInjectionWithNameCodeFix.Descriptor>;

[TestClass]
public class ReplaceIndexInjectionWithName
{
    const string targetClass = """
class TargetType
{
    public void TargetMethod(int x) {}
}
""";

    [TestMethod]
    public async Task ReplaceIndexInjection()
    {
        const string testPatch = """
using HarmonyLib;

[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
static class Patch
{
    static void Postfix(int {|#0:__0|}) {}
}
""";
        const string fixedPatch = """
using HarmonyLib;

[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
static class Patch
{
    static void Postfix(int x) {}
}
""";

        await Verify.VerifyCodeFixAsync(
            new TestSources(targetClass, testPatch, fixedPatch),
            Verify.Diagnostic().WithLocation(0));
    }
}
