using MicroUtils.HarmonyAnalyzers.CodeFixes.MHA002;

namespace MicroUtils.HarmonyAnalyzers.Test.MHA002;

using Verify = PatchClassCodeFixVerifier<AddPatchTypeAttributeCodeFix, AddPatchTypeAttribute>;

public partial class AddMissingPatchMethodTypeAttribute
{
    [TestMethod]
    public async Task AddTranspilerAttribute()
    {
        var test = """
using System.Collections.Generic;

using HarmonyLib;

static class TargetClass
{
    public static string TargetMethod() => "";
}

[HarmonyPatch]
static class PatchClass
{
    [HarmonyPatch(typeof(TargetClass), nameof(TargetClass.TargetMethod))]
    static IEnumerable<CodeInstruction> {|#0:PatchMethod|}(IEnumerable<CodeInstruction> instructions) => instructions;
}
""";

        var testfix = """
using System.Collections.Generic;

using HarmonyLib;

static class TargetClass
{
    public static string TargetMethod() => "";
}

[HarmonyPatch]
static class PatchClass
{
    [HarmonyPatch(typeof(TargetClass), nameof(TargetClass.TargetMethod))]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> {|#0:PatchMethod|}(IEnumerable<CodeInstruction> instructions) => instructions;
}
""";
        await Verify.VerifyCodeFixAsync(
            test,
            testfix,
            disabledDiagnostics: Default.DisabledDiagnostics.Add("MHA013"));
    }
}
