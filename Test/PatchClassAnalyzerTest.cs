using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace MicroUtils.HarmonyAnalyzers.Test;

using static CommonTestProperties;

internal class PatchClassAnalyzerTest : CSharpAnalyzerTest<PatchClassAnalyzer, DefaultVerifier>
{
    public PatchClassAnalyzerTest()
    {
        this.ReferenceAssemblies = ReferenceAssemblies.Default.AddPackages([HarmonyPackage]);
        this.DisabledDiagnostics.AddRange(IgnoreDiagnostics);
    }
}