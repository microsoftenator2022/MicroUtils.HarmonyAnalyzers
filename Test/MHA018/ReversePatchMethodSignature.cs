using MicroUtils.HarmonyAnalyzers.CodeFixes.MHA018;

namespace MicroUtils.HarmonyAnalyzers.Test.MHA018;

using Verify = PatchClassCodeFixVerifier<FixMethodSignatureCodeFix, FixMethodSignatureCodeFix.Descriptor>;

[TestClass]
public class ReversePatchMethodSignature
{
    const string targetClass = """
class TargetType
{
    public string TargetMethod(int i) => i.ToString();
}
""";

    [TestMethod]
    public async Task ValidSignature()
    {
        const string testPatch = """
using System;

using HarmonyLib;

[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
static class Patch
{
    [HarmonyReversePatch]
    static string ReversePatch(TargetType instance, int i)
    {
        throw new NotImplementedException("STUB");
    }
}
""";
        await Verify.VerifyCodeFixAsync(
            new TestSources(targetClass, testPatch, testPatch),
            []);
    }

    [TestMethod]
    public async Task IgnoreReverseTranspiler()
    {
        const string testPatch = """
using System;
using System.Collections.Generic;

using HarmonyLib;

[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
static class Patch
{
    [HarmonyReversePatch]
    static string ReversePatch(TargetType instance, int i)
    {
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => instructions;

        _ = Transpiler(null!);

        return null!;
    }
}
""";

        await Verify.VerifyCodeFixAsync(
            new TestSources(targetClass, testPatch, testPatch),
            []);
    }

    [TestMethod]
    public async Task InvalidSignature()
    {
        const string testPatch = """
using System;

using HarmonyLib;

[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
static class Patch
{
    [HarmonyReversePatch]
    static string {|#0:ReversePatch|}(TargetType instance)
    {
        throw new NotImplementedException("STUB");
    }
}
""";

        const string fixedPatch = """
using System;

using HarmonyLib;

[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
static class Patch
{
    [HarmonyReversePatch]
    static string ReversePatch(TargetType instance, int i)
    {
        throw new NotImplementedException("STUB");
    }
}
""";

        await Verify.VerifyCodeFixAsync(
            new TestSources(targetClass, testPatch, fixedPatch),
            Verify.Diagnostic().WithLocation(0));
    }
}
