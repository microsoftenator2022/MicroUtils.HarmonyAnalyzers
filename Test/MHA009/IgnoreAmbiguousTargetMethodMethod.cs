namespace MicroUtils.HarmonyAnalyzers.Test.MHA009;

using Verify = PatchClassAnalyzerVerifier;

[TestClass]
public class IgnoreAmbiguousTargetMethodMethod
{
    const string targetClass = """
class TargetType
{
    public void TargetMethod() {}
    public string TargetProperty => "";
}
""";

    [TestMethod]
    public async Task DontReportIfPatchTypeConflict()
    {
        const string testPatch = """
using System.Collections.Generic;
using System.Reflection;

using HarmonyLib;

[{|#0:HarmonyPatch|}]
static class {|#1:Patch|}
{
    [{|#2:HarmonyTargetMethods|}]
    static IEnumerable<MethodInfo> {|#3:TargetMethod|}() => System.Array.Empty<MethodInfo>();

    static void Postfix() {}
}
""";

        await Verify.VerifyAnalyzerAsync(new TestSources(targetClass, testPatch), [], disabledDiagnostics: ["MHA007"]);
    }
}
