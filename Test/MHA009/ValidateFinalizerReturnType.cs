namespace MicroUtils.HarmonyAnalyzers.Test.MHA009;

using Verify = PatchClassAnalyzerVerifier;

[TestClass]
public class ValidateFinalizerReturnType
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
using System;

using HarmonyLib;

[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
static class Patch
{
    static void Finalizer() {}
    static Exception Finalizer(Exception __exception) => null;
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
using HarmonyLib;

[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
static class Patch
{
    static {|#0:string|} Finalizer() => "";
}
""";

        await Verify.VerifyAnalyzerAsync(
            new TestSources(targetClass, testPatch),
            Verify.Diagnostic("MHA009").WithLocation(0));
    }
}
