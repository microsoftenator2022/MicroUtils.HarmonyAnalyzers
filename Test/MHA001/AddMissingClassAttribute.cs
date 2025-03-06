using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MicroUtils.HarmonyAnalyzers.CodeFixes;
using MicroUtils.HarmonyAnalyzers.CodeFixes.MHA001;

namespace MicroUtils.HarmonyAnalyzers.Test.MHA001;

using Verify = PatchClassCodeFixVerifier<AddHarmonyPatchAttributeCodeFix>;

[TestClass]
public class MHA001
{
    [TestMethod]
    public async Task AddMissingClassAttribute()
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
            Verify.Diagnostic("MHA001").WithLocation(0).WithMessage(null),
            HarmonyCodeFix.GetEquivalenceKey<AddHarmonyPatchAttribute>(),
            testFix);
    }
}
