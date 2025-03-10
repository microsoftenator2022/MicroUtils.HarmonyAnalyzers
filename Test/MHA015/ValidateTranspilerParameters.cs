namespace MicroUtils.HarmonyAnalyzers.Test.MHA015;

using Verify = PatchClassAnalyzerVerifier;

[TestClass]
public class ValidateTranspilerParameters
{
    const string targetClass = """
class TargetType
{
    public bool TargetMethod() => true;
}
""";

    [TestMethod]
    public async Task ValidParameters()
    {
        const string testPatch = """
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using HarmonyLib;

[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
static class Patch
{
    static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions,
        MethodBase originalMethod,
        ILGenerator ilGenerator) => instructions;
}
""";

        await Verify.VerifyAnalyzerAsync(
            new TestSources(targetClass, testPatch),
            []);
    }

    [TestMethod]
    public async Task InvalidParameter()
    {
        const string testPatch = """
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using HarmonyLib;

[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
static class Patch
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, {|#0:bool __result|}) => instructions;
}
""";

        await Verify.VerifyAnalyzerAsync(
            new TestSources(targetClass, testPatch),
            Verify.Diagnostic("MHA015").WithLocation(0));
    }
}
