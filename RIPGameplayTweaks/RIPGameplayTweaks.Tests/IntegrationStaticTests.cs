using System;
using System.IO;

internal static class IntegrationStaticTests
{
    public static void RunAll()
    {
        string sourceRoot = Path.Combine(Directory.GetCurrentDirectory(), "RIPGameplayTweaks");
        if (!Directory.Exists(sourceRoot))
            sourceRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), ".."));

        string projectSource = File.ReadAllText(Path.Combine(sourceRoot, "RIPGameplayTweaks.csproj"));
        string modSource = File.ReadAllText(Path.Combine(sourceRoot, "RIPGameplayTweaksMod.cs"));
        string configSource = File.ReadAllText(Path.Combine(sourceRoot, "GameplayTweaksConfig.cs"));

        TestAssert.True(modSource.Contains("\"1.2.0\"", StringComparison.Ordinal), "mod version is 1.2.0");
        TestAssert.True(
            projectSource.Contains("<Version>1.2.0</Version>", StringComparison.Ordinal),
            "assembly version is 1.2.0");
        TestAssert.True(modSource.Contains("GlobalDropAbsorber", StringComparison.Ordinal), "mod keeps absorb");
        TestAssert.True(
            modSource.Contains("absorbEnabled", StringComparison.Ordinal),
            "config logs absorbEnabled");
        TestAssert.False(
            modSource.Contains("OracleFusionFeature", StringComparison.Ordinal),
            "tweaks no longer enables oracle fusion");
        TestAssert.False(
            modSource.Contains("cashGainMultiplier", StringComparison.Ordinal),
            "tweaks no longer owns cash multiplier");
        TestAssert.False(
            configSource.Contains("TributeAttributeMultiplier", StringComparison.Ordinal) ||
            configSource.Contains("CashGainMultiplier", StringComparison.Ordinal),
            "tweaks config is absorb-only");
        TestAssert.False(
            File.Exists(Path.Combine(sourceRoot, "OracleFusionFeature.cs")) ||
            File.Exists(Path.Combine(sourceRoot, "CashGainMath.cs")) ||
            File.Exists(Path.Combine(sourceRoot, "YinluAdvanceDecision.cs")),
            "tweaks source tree has no oracle/cash/yinlu logic files");
    }
}
