using MicroUtils.HarmonyAnalyzers.CodeFixes.MHA002;

namespace MicroUtils.HarmonyAnalyzers.Test.MHA002;

using Verify = PatchClassCodeFixVerifier<AddPatchTypeAttributeCodeFix, AddPatchTypeAttributeCodeFix.Descriptor>;

public partial class AddMissingPatchMethodTypeAttribute
{
    [TestMethod]
    public async Task AddTranspilerAttribute()
    {
        var testPatch = """
using System.Collections.Generic;

using HarmonyLib;

[HarmonyPatch]
static class PatchClass
{
    [HarmonyPatch(typeof(TargetClass), nameof(TargetClass.TargetMethod))]
    static IEnumerable<CodeInstruction> {|#0:PatchMethod|}(IEnumerable<CodeInstruction> instructions) => instructions;
}
""";

        var fixedPatch = """
using System.Collections.Generic;

using HarmonyLib;

[HarmonyPatch]
static class PatchClass
{
    [HarmonyPatch(typeof(TargetClass), nameof(TargetClass.TargetMethod))]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> {|#0:PatchMethod|}(IEnumerable<CodeInstruction> instructions) => instructions;
}
""";
        await Verify.VerifyCodeFixAsync(
            new TestSources(targetClass, testPatch, fixedPatch),
            Verify.Diagnostic().WithLocation(0),
            disabledDiagnostics: Default.DisabledDiagnostics.Add("MHA013"));
    }
}
