using MicroUtils.HarmonyAnalyzers.CodeFixes.MHA003;

namespace MicroUtils.HarmonyAnalyzers.Test.MHA003;

using Verify = PatchClassCodeFixVerifier<AddMissingMethodTypeCodeFix, AddMissingMethodTypeCodeFix.Descriptor>;

[TestClass]
public class AddConstructorMethodType
{
    const string targetClass = """
class TargetType
{
    public TargetType(int i) {}
}
""";

    [TestMethod]
    public async Task AddNonStaticConstructorMethodType()
    {
        const string testPatch = """
using HarmonyLib;

[HarmonyPatch]
static class Patch
{
    [HarmonyPatch(typeof(TargetType), new System.Type[] { typeof(int) })]
    static void {|#0:Postfix|}() {}
}
""";

        const string testFix = """
using HarmonyLib;

[HarmonyPatch]
static class Patch
{
    [HarmonyPatch(typeof(TargetType), new System.Type[] { typeof(int) })]
    [HarmonyPatch(MethodType.Constructor)]
    static void {|#0:Postfix|}() {}
}
""";

        await Verify.VerifyCodeFixAsync(
            new TestSources(targetClass, testPatch, testFix),
            Verify.Diagnostic().WithLocation(0),
            disabledDiagnostics: Default.DisabledDiagnostics.Add("MHA005"));
    }


//    [TestMethod]
//    public async Task AddStaticConstructorMethodType()
//    {
//        const string testPatch = """
//using HarmonyLib;

//[HarmonyPatch]
//static class Patch
//{
//    [HarmonyPatch(typeof(TargetType), new System.Type[0])]
//    static void {|#0:Postfix|}() {}
//}
//""";

//        const string testFix = """
//using HarmonyLib;

//[HarmonyPatch]
//static class Patch
//{
//    [HarmonyPatch(typeof(TargetType), new System.Type[0])]
//    [HarmonyPatch(MethodType.StaticConstructor)]
//    static void {|#0:Postfix|}() {}
//}
//""";

//        await Verify.VerifyCodeFixAsync(
//            new TestSources(targetClass, testPatch, testFix),
//            disabledDiagnostics: Default.DisableDiagnostics.Add("MHA005"));
//    }
}
