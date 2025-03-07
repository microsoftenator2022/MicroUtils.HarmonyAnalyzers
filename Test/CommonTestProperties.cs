global using System;
global using System.Collections.Generic;
global using System.Collections.Immutable;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;

global using Microsoft.CodeAnalysis.Testing;
global using Microsoft.VisualStudio.TestTools.UnitTesting;

global using MicroUtils.HarmonyAnalyzers;
global using MicroUtils.HarmonyAnalyzers.CodeFixes;

global using static MicroUtils.HarmonyAnalyzers.Test.CommonTestProperties;
global using static MicroUtils.HarmonyAnalyzers.HarmonyConstants;

namespace MicroUtils.HarmonyAnalyzers.Test;

internal static class CommonTestProperties
{
    public static readonly PackageIdentity HarmonyPackage = new("Lib.Harmony", "2.3.1.1");

    public static class Default
    {
        public static readonly ImmutableArray<string> DisabledDiagnostics = ["DEBUG", "MHI000"];
    }
}
