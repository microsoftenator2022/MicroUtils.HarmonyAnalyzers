namespace MicroUtils.HarmonyAnalyzers.Test.MHA014;

using Verify = PatchClassAnalyzerVerifier;

[TestClass]
public class ValidateInjectionParameterTypes
{
    const string targetClass = """
class TargetType
{
    public string TargetMethod() => "";
}
""";

    [TestMethod]
    public async Task ValidInjections()
    {
        const string testPatch = """
using System;
using System.Reflection;

using HarmonyLib;

[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
static class Patch
{
    static void Prefix(object[] __args, TargetType __instance, MethodBase __originalMethod, string __result, bool __runOriginal) {}
    static void Finalizer(Exception __exception) {}
}
""";

        await Verify.VerifyAnalyzerAsync(
            new TestSources(targetClass, testPatch), []);
    }

    [TestMethod]
    public async Task InvalidInjections()
    {
        const string testPatch = """
using System;
using System.Collections.Generic;
using System.Reflection;

using HarmonyLib;

[HarmonyPatch(typeof(TargetType), nameof(TargetType.TargetMethod))]
class Patch
{
    static void Prefix({|#0:List<object>|} __args, {|#1:Patch|} __instance, {|#2:string|} __originalMethod, {|#3:int|} __result, {|#4:MethodInfo|} __runOriginal) {}
    static void Finalizer({|#5:NotImplementedException|} __exception) {}
}
""";
        await Verify.VerifyAnalyzerAsync(
            new TestSources(targetClass, testPatch),
            [
                Verify.Diagnostic("MHA014").WithLocation(0),
                Verify.Diagnostic("MHA014").WithLocation(1),
                Verify.Diagnostic("MHA014").WithLocation(2),
                Verify.Diagnostic("MHA014").WithLocation(3),
                Verify.Diagnostic("MHA014").WithLocation(4),
                Verify.Diagnostic("MHA014").WithLocation(5)
            ]);
    }
}
