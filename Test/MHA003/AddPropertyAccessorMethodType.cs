using MicroUtils.HarmonyAnalyzers.CodeFixes.MHA003;

namespace MicroUtils.HarmonyAnalyzers.Test.MHA003;

using Verify = PatchClassCodeFixVerifier<AddMissingMethodTypeCodeFix, AddMissingMethodTypeCodeFix.Descriptor>;

[TestClass]
public class AddPropertyAccessorMethodType
{
    const string targetClass = """
class TargetType
{
    public object TargetProperty { get => null; set { } }
    public string this[int index] => index.ToString();
}
""";

    const string testPropertyPatch = """
using HarmonyLib;

[HarmonyPatch]
static class Patch
{
    [HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetProperty))]
    static void {|#0:Postfix|}() { }
}
""";

    [TestMethod]
    public async Task AddGetterMethodType()
    {
        const string testFix = """
using HarmonyLib;

[HarmonyPatch]
static class Patch
{
    [HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetProperty))]
    [HarmonyPatch(MethodType.Getter)]
    static void {|#0:Postfix|}() { }
}
""";
        await Verify.VerifyCodeFixAsync(
            new TestSources(targetClass, testPropertyPatch, testFix),
            [Verify.Diagnostic().WithLocation(0), Verify.Diagnostic().WithLocation(0)],
            Verify.GetEquivalenceKey($"MethodType.{PatchTargetMethodType.Getter}"),
            Default.DisabledDiagnostics.Add("MHA005"));
    }

    [TestMethod]
    public async Task AddSetterMethodType()
    {
        const string testFix = """
using HarmonyLib;

[HarmonyPatch]
static class Patch
{
    [HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetProperty))]
    [HarmonyPatch(MethodType.Setter)]
    static void {|#0:Postfix|}() { }
}
""";
        await Verify.VerifyCodeFixAsync(
            new TestSources(targetClass, testPropertyPatch, testFix),
            [Verify.Diagnostic().WithLocation(0), Verify.Diagnostic().WithLocation(0)],
            Verify.GetEquivalenceKey($"MethodType.{PatchTargetMethodType.Setter}"),
            disabledDiagnostics: Default.DisabledDiagnostics.Add("MHA005"));
    }

    [TestMethod]
    public async Task AddIndexerGetterMethodType()
    {
        const string testIndexerPatch = """
using HarmonyLib;

[HarmonyPatch]
static class Patch
{
    [HarmonyPatch(typeof(TargetType), new System.Type[] { typeof(int) })]
    static void {|#0:Postfix|}() { }
}
""";

        const string testFix = """
using HarmonyLib;

[HarmonyPatch]
static class Patch
{
    [HarmonyPatch(typeof(TargetType), new System.Type[] { typeof(int) })]
    [HarmonyPatch(MethodType.Getter)]
    static void {|#0:Postfix|}() { }
}
""";
        await Verify.VerifyCodeFixAsync(
            new TestSources(targetClass, testIndexerPatch, testFix),
            Verify.Diagnostic().WithLocation(0),
            disabledDiagnostics: Default.DisabledDiagnostics.Add("MHA005"));
    }
}
