global using static MicroUtils.HarmonyAnalyzers.Test.CommonTestProperties;


namespace MicroUtils.HarmonyAnalyzers.Test;

internal static class CommonTestProperties
{
    public static readonly PackageIdentity HarmonyPackage = new("Lib.Harmony", "2.3.1.1");

    public static class Default
    {
        public static readonly ImmutableArray<string> DisabledDiagnostics = ["DEBUG", "MHI000"];
    }
}
