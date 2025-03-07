using MicroUtils.HarmonyAnalyzers.CodeFixes.MHA002;

namespace MicroUtils.HarmonyAnalyzers.Test.MHA002;

using Verify = PatchClassCodeFixVerifier<AddPatchTypeAttributeCodeFix, AddPatchTypeAttribute>;

public partial class AddMissingPatchMethodTypeAttribute
{
    static string GetEquivalenceKey(HarmonyPatchType patchType) =>
        Verify.GetEquivalenceKey(HarmonyHelpers.GetPatchTypeAttributeName(patchType));
}
