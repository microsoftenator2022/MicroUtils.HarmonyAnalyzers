using MicroUtils.HarmonyAnalyzers.CodeFixes.MHA012;

namespace MicroUtils.HarmonyAnalyzers.Test.MHA012;

using Verify = PatchClassCodeFixVerifier<RemoveResultInjectionCodeFix, RemoveResultInjectionCodeFix.Descriptor>;

[TestClass]
public class UnnecessaryResultInjectionInPostfix
{
    const string targetClass = """
class TargetType
{
    public static string TargetMethod() => "";
}
""";

    [TestMethod]
     public async Task RemoveResultParameter()
    {
        const string testPatch = """
using HarmonyLib;

[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
static class Patch
{
    static string Postfix(string s, string {|#0:__result|}) => s;
}
""";

        const string fixedPatch = """
using HarmonyLib;

[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
static class Patch
{
    static string Postfix(string s) => s;
}
""";

        await Verify.VerifyCodeFixAsync(
            new TestSources(targetClass, testPatch, fixedPatch),
            Verify.Diagnostic().WithLocation(0));
    }
}
