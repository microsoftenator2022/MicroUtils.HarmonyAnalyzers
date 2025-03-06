global using VerifyAnalyzer = Microsoft.CodeAnalysis.Testing.AnalyzerVerifier<
    MicroUtils.HarmonyAnalyzers.PatchClassAnalyzer,
    MicroUtils.HarmonyAnalyzers.Test.PatchClassAnalyzerTest,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

using Microsoft.CodeAnalysis.Testing;

namespace MicroUtils.HarmonyAnalyzers.Test;

internal static class CommonTestProperties
{
    public static readonly PackageIdentity HarmonyPackage = new("Lib.Harmony", "2.3.1.1");

    public static readonly string[] IgnoreDiagnostics = ["DEBUG", "MHI000"];
}
