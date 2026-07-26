using System;
using System.IO;

internal static class IntegrationStaticTests
{
    public static void RunAll()
    {
        string root = Directory.GetCurrentDirectory();
        string modPath = Path.Combine(root, "QualityBoostMod.cs");
        if (!File.Exists(modPath))
            modPath = Path.GetFullPath(Path.Combine(root, "..", "QualityBoostMod.cs"));

        string rootDir = Path.GetDirectoryName(modPath);
        string modSource = File.ReadAllText(modPath);
        string cashSource = File.ReadAllText(Path.Combine(rootDir, "CashGainMultiplierPatches.cs"));
        string tributeSource = File.ReadAllText(Path.Combine(rootDir, "TributeMultiplierPatches.cs"));

        TestAssert.True(modSource.Contains("\"1.3.0\"", StringComparison.Ordinal), "mod version is 1.3.0");
        TestAssert.True(
            modSource.Contains("tributeAttributeMultiplier", StringComparison.Ordinal) &&
            modSource.Contains("cashGainMultiplier", StringComparison.Ordinal),
            "QualityBoost owns both multiplier config keys");
        TestAssert.True(
            cashSource.Contains("QualityBoostMod.Cfg.cashGainMultiplier", StringComparison.Ordinal),
            "cash patches read QualityBoost config");
        TestAssert.True(
            tributeSource.Contains("QualityBoostMod.Cfg.tributeAttributeMultiplier", StringComparison.Ordinal),
            "tribute patches read QualityBoost config");
        TestAssert.True(
            cashSource.Contains("ArchiveData.SetData", StringComparison.Ordinal) &&
            cashSource.Contains("BattleUserObj.CacheIntValue", StringComparison.Ordinal) &&
            cashSource.Contains("CommonObj.CacheIntValue", StringComparison.Ordinal),
            "cash patches cover restore, battle and common cache stages");
    }
}
