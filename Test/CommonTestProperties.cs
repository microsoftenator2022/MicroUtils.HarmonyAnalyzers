global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading.Tasks;

global using Microsoft.VisualStudio.TestTools.UnitTesting;

global using MicroUtils.HarmonyAnalyzers;
global using MicroUtils.HarmonyAnalyzers.CodeFixes;

global using VerifyAnalyzer = Microsoft.CodeAnalysis.Testing.AnalyzerVerifier<
    MicroUtils.HarmonyAnalyzers.PatchClassAnalyzer,
    MicroUtils.HarmonyAnalyzers.Test.PatchClassAnalyzerTest,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

global using static MicroUtils.HarmonyAnalyzers.Test.CommonTestProperties;
global using static MicroUtils.HarmonyAnalyzers.HarmonyConstants;

using System.Collections.Immutable;

using Microsoft.CodeAnalysis.Testing;

namespace MicroUtils.HarmonyAnalyzers.Test;

internal static class CommonTestProperties
{
    public static readonly PackageIdentity HarmonyPackage = new("Lib.Harmony", "2.3.1.1");

    public static class Default
    {
        public static readonly ImmutableArray<string> DisableDiagnostics = ["DEBUG", "MHI000"];
    }
}
