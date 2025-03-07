namespace MicroUtils.HarmonyAnalyzers.Test.MHA007;

using Verify = PatchClassAnalyzerVerifier;

[TestClass]
public class HarmonyTargetConflict
{
    const string targetClass = """
class TargetType
{
    public void TargetMethod() {}
    public string TargetProperty => "";
}
""";

    [TestMethod]
    public async Task ReportHarmonyPatchTargetPropertyConflict()
    {
        const string testPatch = """
using System.Collections.Generic;
using System.Reflection;

using HarmonyLib;

[{|#0:HarmonyPatch(typeof(TargetType))|}]
static class {|#1:Patch|}
{
    static IEnumerable<MethodInfo> {|#2:TargetMethods|}() => System.Array.Empty<MethodInfo>();

    [{|#3:HarmonyPatch(nameof(TargetType.TargetMethod))|}]
    static void Postfix() {}
}
""";
        await Verify.VerifyAnalyzerAsync(
            new TestSources(targetClass, testPatch),
            [
                Verify.Diagnostic("MHA007").WithLocation(0).WithMessage(null),
                Verify.Diagnostic("MHA007").WithLocation(1).WithMessage(null),
                Verify.Diagnostic("MHA007").WithLocation(2).WithMessage(null),
                Verify.Diagnostic("MHA007").WithLocation(3).WithMessage(null)
            ]);
    }
}
