namespace MicroUtils.HarmonyAnalyzers.Test.MHA013;

using Verify = PatchClassAnalyzerVerifier;

[TestClass]
public class NoMatchForInjectedArgument
{
    const string targetClass = """
class ParentType {
    private static bool staticParentField;
    private bool parentField;
}

class TargetType : ParentType
{
    private bool field;
    
    public void TargetMethod(string s) {}
}
""";

    [TestMethod]
    public async Task ValidInjectedArgument()
    {
        const string testPatch = """
using HarmonyLib;

[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
static class Patch
{
    static void Postfix(string s) {}
}
""";

        await Verify.VerifyAnalyzerAsync(
            new TestSources(targetClass, testPatch),
            []);
    }

    [TestMethod]
    public async Task InvalidInjectedArgument()
    {
        const string testPatch = """
using HarmonyLib;

[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
static class Patch
{
    static void Postfix(string {|#0:str|}) {}
}
""";

        await Verify.VerifyAnalyzerAsync(
            new TestSources(targetClass, testPatch),
            Verify.Diagnostic("MHA013").WithLocation(0));
    }
    
    [TestMethod]
    public async Task ValidInjectedField()
    {
        const string testPatch = """
using HarmonyLib;

[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
static class Patch
{
    static void Postfix(string s, ref bool ___field) {}
}
""";

        await Verify.VerifyAnalyzerAsync(
            new TestSources(targetClass, testPatch), []);
    }    
    
    [TestMethod]
    public async Task ValidInjectedParentField()
    {
        const string testPatch = """
using HarmonyLib;

[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
static class Patch
{
    static void Postfix(string s, ref bool ___parentField, ref bool ___staticParentField) {}
}
""";

        await Verify.VerifyAnalyzerAsync(
            new TestSources(targetClass, testPatch), []);
    }
}
