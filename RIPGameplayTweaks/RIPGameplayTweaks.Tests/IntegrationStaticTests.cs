using System;
using System.IO;
using System.Linq;

internal static class IntegrationStaticTests
{
    public static void RunAll()
    {
        string sourceRoot = Path.Combine(Directory.GetCurrentDirectory(), "RIPGameplayTweaks");
        string projectSource = File.ReadAllText(Path.Combine(sourceRoot, "RIPGameplayTweaks.csproj"));
        string modSource = File.ReadAllText(Path.Combine(sourceRoot, "RIPGameplayTweaksMod.cs"));
        string yinluSource = File.ReadAllText(Path.Combine(sourceRoot, "YinluSingleMaterialAdvancePatch.cs"));
        string cashSource = File.ReadAllText(Path.Combine(sourceRoot, "CashGainMultiplierPatches.cs"));

        TestAssert.True(modSource.Contains("\"1.1.0\"", StringComparison.Ordinal), "mod version is 1.1.0");
        TestAssert.True(
            projectSource.Contains("<Version>1.1.0</Version>", StringComparison.Ordinal),
            "assembly version is 1.1.0");
        TestAssert.True(modSource.Contains("PatchAll", StringComparison.Ordinal), "mod applies Harmony patches");
        TestAssert.True(
            modSource.Contains("OracleFusionFeature.TryEnable", StringComparison.Ordinal),
            "mod enables oracle fusion feature");
        TestAssert.True(
            modSource.Contains("cashGainMultiplier=1.0", StringComparison.Ordinal),
            "config template includes cash multiplier");

        TestAssert.True(
            yinluSource.Contains("RefreshProgress", StringComparison.Ordinal) &&
            yinluSource.Contains("typeof(float), typeof(float)", StringComparison.Ordinal),
            "yinlu patch targets RefreshProgress(float,float)");
        TestAssert.True(
            cashSource.Contains("ArchiveData.SetData", StringComparison.Ordinal) &&
            cashSource.Contains("BattleUserObj.CacheIntValue", StringComparison.Ordinal) &&
            cashSource.Contains("CommonObj.CacheIntValue", StringComparison.Ordinal),
            "cash patches cover restore, battle and common cache stages");

        var catalog = OracleFusionPatchCatalog.Create();
        TestAssert.Equal(16, catalog.Count, "oracle catalog signature count");
        TestAssert.Equal(
            20,
            catalog.Sum(spec => spec.Replacements.Count),
            "oracle catalog replacement count");
    }
}
