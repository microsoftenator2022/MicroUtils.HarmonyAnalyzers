using MicroUtils.HarmonyAnalyzers.CodeFixes.MHA002;

namespace MicroUtils.HarmonyAnalyzers.Test.MHA002;

using Verify = PatchClassCodeFixVerifier<AddPatchTypeAttributeCodeFix, AddPatchTypeAttributeCodeFix.Descriptor>;

public partial class AddMissingPatchMethodTypeAttribute
{
    const string part1 = """
using HarmonyLib;

[HarmonyPatch]
static class PatchClass
{
    [HarmonyPatch(typeof(TargetClass), nameof(TargetClass.TargetMethod))]
""";

    const string part2 = """
    static void {|#0:PatchMethod|}() {}
}
""";

    static readonly string testPatch = part1 + Environment.NewLine + part2;

    [TestClass]
    public class AddPrefixOrPostfixOrFinalizerAttribute
    {
        [TestMethod]
        public async Task AddPrefixAttribute()
        {
            var prefixPatch = string.Join(Environment.NewLine, part1, "    [HarmonyPrefix]", part2);

            await Verify.VerifyCodeFixAsync(
                new TestSources(targetClass, testPatch, prefixPatch),
                Verify.Diagnostic().WithLocation(0),
                GetEquivalenceKey(HarmonyPatchType.Prefix));
        }

        [TestMethod]
        public async Task AddPostfixAttribute()
        {
            var postfixPatch = string.Join(Environment.NewLine, part1, "    [HarmonyPostfix]", part2);

            await Verify.VerifyCodeFixAsync(
                new TestSources(targetClass, testPatch, postfixPatch),
                Verify.Diagnostic().WithLocation(0),
                GetEquivalenceKey(HarmonyPatchType.Postfix));
        }

        [TestMethod]
        public async Task AddFinalizerAttribute()
        {
            var finalizerPatch = string.Join(Environment.NewLine, part1, "    [HarmonyFinalizer]", part2);

            await Verify.VerifyCodeFixAsync(
                new TestSources(targetClass, testPatch, finalizerPatch),
                Verify.Diagnostic().WithLocation(0),
                GetEquivalenceKey(HarmonyPatchType.Finalizer));
        }
    }
}
