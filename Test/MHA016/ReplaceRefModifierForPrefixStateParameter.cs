using MicroUtils.HarmonyAnalyzers.CodeFixes.MHA016;

namespace MicroUtils.HarmonyAnalyzers.Test.MHA016;

using Verify = PatchClassCodeFixVerifier<
    UseOutForPrefixStateInjectionCodeFix,
    UseOutForPrefixStateInjectionCodeFix.Descriptor>;

[TestClass]
public class ReplaceRefModifierForPrefixStateParameter
{
    const string targetClass = """
class TargetType
{
    public void TargetMethod() {}
}
""";

    [TestMethod]
    public async Task ReplaceRefParameter()
    {
        const string testPatch = """
using HarmonyLib;

[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
static class Patch
{
    static void Prefix({|#0:ref|} int __state) { __state = 0; }
}
""";

        const string fixedPatch = """
using HarmonyLib;

[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
static class Patch
{
    static void Prefix(out int __state) { __state = 0; }
}
""";

        await Verify.VerifyCodeFixAsync(
            new TestSources(targetClass, testPatch, fixedPatch),
            Verify.Diagnostic().WithLocation(0));

    }
}
