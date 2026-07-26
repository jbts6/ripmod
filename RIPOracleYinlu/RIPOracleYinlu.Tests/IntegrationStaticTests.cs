using System;
using System.IO;
using System.Linq;

internal static class IntegrationStaticTests
{
    public static void RunAll()
    {
        string sourceRoot = Path.Combine(Directory.GetCurrentDirectory(), "RIPOracleYinlu");
        if (!Directory.Exists(sourceRoot))
            sourceRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), ".."));

        string projectSource = File.ReadAllText(Path.Combine(sourceRoot, "RIPOracleYinlu.csproj"));
        string modSource = File.ReadAllText(Path.Combine(sourceRoot, "RIPOracleYinluMod.cs"));
        string yinluSource = File.ReadAllText(Path.Combine(sourceRoot, "YinluSingleMaterialAdvancePatch.cs"));

        TestAssert.True(modSource.Contains("\"1.0.0\"", StringComparison.Ordinal), "mod version is 1.0.0");
        TestAssert.True(
            projectSource.Contains("<Version>1.0.0</Version>", StringComparison.Ordinal),
            "assembly version is 1.0.0");
        TestAssert.True(modSource.Contains("PatchAll", StringComparison.Ordinal), "mod applies Harmony patches");
        TestAssert.True(
            modSource.Contains("OracleFusionFeature.TryEnable", StringComparison.Ordinal),
            "mod enables oracle fusion feature");
        TestAssert.True(
            yinluSource.Contains("RefreshProgress", StringComparison.Ordinal) &&
            yinluSource.Contains("typeof(float), typeof(float)", StringComparison.Ordinal),
            "yinlu patch targets RefreshProgress(float,float)");
        TestAssert.False(
            modSource.Contains("cashGainMultiplier", StringComparison.Ordinal) ||
            modSource.Contains("absorbEnabled", StringComparison.Ordinal),
            "oracle mod does not own cash or absorb config");

        var catalog = OracleFusionPatchCatalog.Create();
        TestAssert.Equal(17, catalog.Count, "oracle catalog signature count");
        TestAssert.Equal(
            21,
            catalog.Sum(spec => spec.Replacements.Count),
            "oracle catalog replacement count");
    }
}
