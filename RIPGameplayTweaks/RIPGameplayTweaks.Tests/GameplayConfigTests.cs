using System;
using System.Collections.Generic;

internal static class GameplayConfigTests
{
    public static void RunAll()
    {
        ConfigParsesAbsorbOnly();
        ConfigParsesMixedCaseAbsorb();
        ConfigIgnoresMigratedMultiplierKeys();
        ConfigKeepsPreviousAbsorbAfterInvalid();
    }

    private static void ConfigParsesAbsorbOnly()
    {
        var config = GameplayTweaksConfig.ParseLines(
            new[]
            {
                "",
                "# 配置注释",
                "absorbEnabled=0",
                "unknownKey=ignored"
            },
            new GameplayTweaksConfig(),
            _ => { });

        TestAssert.False(config.AbsorbEnabled, "absorbEnabled=0 disables absorb");
    }

    private static void ConfigParsesMixedCaseAbsorb()
    {
        var config = GameplayTweaksConfig.ParseLines(
            new[] { "AbSoRbEnAbLeD=0" },
            new GameplayTweaksConfig(),
            _ => { });

        TestAssert.False(config.AbsorbEnabled, "mixed-case absorbEnabled parses");
    }

    private static void ConfigIgnoresMigratedMultiplierKeys()
    {
        var config = GameplayTweaksConfig.ParseLines(
            new[]
            {
                "absorbEnabled=1",
                "tributeAttributeMultiplier=9.9",
                "cashGainMultiplier=9.9"
            },
            new GameplayTweaksConfig(),
            _ => { });

        TestAssert.True(config.AbsorbEnabled, "absorb still parses with migrated keys present");
    }

    private static void ConfigKeepsPreviousAbsorbAfterInvalid()
    {
        var warnings = new List<string>();
        GameplayTweaksConfig config = GameplayTweaksConfig.ParseLines(
            new[] { "absorbEnabled=0" },
            new GameplayTweaksConfig(),
            warnings.Add);
        config = GameplayTweaksConfig.ParseLines(
            new[] { "absorbEnabled=maybe" },
            config,
            warnings.Add);

        TestAssert.False(config.AbsorbEnabled, "invalid absorb keeps previous");
        TestAssert.Equal(1, warnings.Count, "invalid absorb writes one warning");
    }
}
