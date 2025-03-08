namespace MicroUtils.HarmonyAnalyzers.Test.MHA009;

using Verify = PatchClassAnalyzerVerifier;

[TestClass]
public class ValidateTargetMethodReturnType
{
    const string targetClass = """
class TargetType
{
    public string TargetMethod() => "";
}
""";

    [TestMethod]
    public async Task ValidReturnTypes()
    {
        const string testPatch = """
using System.Reflection;

using HarmonyLib;

[HarmonyPatch]
static class Patch
{
    static MethodInfo TargetMethod() => typeof(TargetType).GetMethod(nameof(TargetType.TargetMethod));

    static void Postfix() {}
}
""";

        await Verify.VerifyAnalyzerAsync(
            new TestSources(targetClass, testPatch),
            []);
    }

    [TestMethod]
    public async Task InvalidReturnType()
    {
        const string testPatch = """
using System.Reflection;

using HarmonyLib;

[HarmonyPatch]
static class Patch
{
    static {|#0:string|} TargetMethod() => typeof(TargetType).GetMethod(nameof(TargetType.TargetMethod)).ToString();

    static void Postfix() {}
}
""";

        await Verify.VerifyAnalyzerAsync(
            new TestSources(targetClass, testPatch),
            Verify.Diagnostic("MHA009").WithLocation(0));
    }
}
