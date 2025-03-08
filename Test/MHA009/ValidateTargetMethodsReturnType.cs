namespace MicroUtils.HarmonyAnalyzers.Test.MHA009;

using Verify = PatchClassAnalyzerVerifier;

[TestClass]
public class ValidateTargetMethodsReturnType
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
    static MethodInfo[] TargetMethods() => new MethodInfo[] { typeof(TargetType).GetMethod(nameof(TargetType.TargetMethod)) };

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
    static {|#0:MethodInfo|} TargetMethods() => typeof(TargetType).GetMethod(nameof(TargetType.TargetMethod));

    static void Postfix() {}
}
""";

        await Verify.VerifyAnalyzerAsync(
            new TestSources(targetClass, testPatch),
            Verify.Diagnostic("MHA009").WithLocation(0));
    }
}
