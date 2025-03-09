using MicroUtils.HarmonyAnalyzers.CodeFixes.MHA002;

namespace MicroUtils.HarmonyAnalyzers.Test.MHA002;

using Verify = PatchClassCodeFixVerifier<AddPatchTypeAttributeCodeFix, AddPatchTypeAttributeCodeFix.Descriptor>;

public partial class AddMissingPatchMethodTypeAttribute
{
    const string part1 = """
using HarmonyLib;

static class TargetClass
{
    public static string TargetMethod() => "";
}

[HarmonyPatch]
static class PatchClass
{
    [HarmonyPatch(typeof(TargetClass), nameof(TargetClass.TargetMethod))]
""";

    const string part2 = """
    static void {|#0:PatchMethod|}() {}
}
""";

    static readonly string test = part1 + Environment.NewLine + part2;

    [TestClass]
    public class AddPrefixOrPostfixOrFinalizerAttribute
    {
        [TestMethod]
        public async Task AddPrefixAttribute()
        {
            var prefix = string.Join(Environment.NewLine, part1, "    [HarmonyPrefix]", part2);

            await Verify.VerifyCodeFixAsync(
                test,
                Verify.Diagnostic().WithLocation(0),
                prefix,
                GetEquivalenceKey(HarmonyPatchType.Prefix));
        }

        [TestMethod]
        public async Task AddPostfixAttribute()
        {
            var postfix = string.Join(Environment.NewLine, part1, "    [HarmonyPostfix]", part2);

            await Verify.VerifyCodeFixAsync(
                test,
                Verify.Diagnostic().WithLocation(0),
                postfix,
                GetEquivalenceKey(HarmonyPatchType.Postfix));
        }

        [TestMethod]
        public async Task AddFinalizerAttribute()
        {
            var finalizer = string.Join(Environment.NewLine, part1, "    [HarmonyFinalizer]", part2);

            await Verify.VerifyCodeFixAsync(
                test,
                Verify.Diagnostic().WithLocation(0),
                finalizer,
                GetEquivalenceKey(HarmonyPatchType.Finalizer));
        }
    }
}
