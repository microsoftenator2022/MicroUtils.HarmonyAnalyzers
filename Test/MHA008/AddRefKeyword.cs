using Microsoft.CodeAnalysis;

using MicroUtils.HarmonyAnalyzers.CodeFixes.MHA008;

namespace MicroUtils.HarmonyAnalyzers.Test.MHA008;

using Verify = PatchClassCodeFixVerifier<AddRefKeywordCodeFix, AddRefKeyword>;

[TestClass]
public class AddRefKeywordToInjectionParameter
{
    const string targetClass = """
class TargetType
{
    public void TargetMethod(string s) {}
}
""";

    [TestMethod]
    public async Task AddRefKeyword()
    {
        const string testPatch = """
using HarmonyLib;

[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
static class Patch
{
    static void Postfix(string {|#0:s|})
    {
        {|#1:s|} = "";
    }
}
""";

        const string testFix = """
using HarmonyLib;

[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
static class Patch
{
    static void Postfix(ref string {|#0:s|})
    {
        {|#1:s|} = "";
    }
}
""";

        await Verify.VerifyCodeFixAsync(
            new TestSources(targetClass, testPatch, testFix),
            Verify.Diagnostic().WithLocation(1).WithLocation(0));
    }
}
