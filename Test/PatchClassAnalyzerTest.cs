using Microsoft.CodeAnalysis.CSharp.Testing;

namespace MicroUtils.HarmonyAnalyzers.Test;

internal class PatchClassAnalyzerTest : CSharpAnalyzerTest<PatchClassAnalyzer, DefaultVerifier>
{
    public PatchClassAnalyzerTest()
    {
        this.ReferenceAssemblies = ReferenceAssemblies.Default.AddPackages([HarmonyPackage]);
        this.DisabledDiagnostics.AddRange(Default.DisabledDiagnostics);
    }

    const string targetClassSourceName = "TargetClass.cs";

    public string? TargetClassCode
    {
        get;
        set
        {
            _ = this.TestState.Sources.RemoveAll(s => s.filename == targetClassSourceName);

            if (value is not null)
            {
                this.TestState.Sources.Add((targetClassSourceName, value));
            }

            field = value;

        }
    }

    const string patchCodeSourceName = "Patch.cs";

    public string? TestPatchCode
    {
        get;
        set
        {
            _ = this.TestState.Sources.RemoveAll(s => s.filename == patchCodeSourceName);

            if (value is not null)
                this.TestState.Sources.Add((patchCodeSourceName, value));

            field = value;

        }
    }
}