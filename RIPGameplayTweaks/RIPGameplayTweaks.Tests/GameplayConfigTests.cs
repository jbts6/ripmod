using System;
using System.Collections.Generic;

internal static class GameplayConfigTests
{
    public static void RunAll()
    {
        TestAssert.Near(1.5, Parse("1.5", 1.0), "valid multiplier");
        TestAssert.Near(1.0, Parse("not-a-number", 1.0), "invalid keeps previous");
        TestAssert.Near(1.0, Parse("NaN", 1.0), "NaN keeps previous");
        TestAssert.Near(1.0, Parse("Infinity", 1.0), "Infinity keeps previous");
        TestAssert.Near(1.0, Parse("-1", 1.0), "negative keeps previous");
        TestAssert.Near(1.0, Parse("0", 1.0), "zero keeps previous");
        TestAssert.Near(1.0, Parse("101", 1.0), "over-limit keeps previous");
        InvalidMultiplierWarnsWithKey();
        ConfigParsesKnownKeysAndIgnoresUnknowns();
        ConfigParsesMixedCaseKeys();
        ConfigKeepsPreviousMultiplierAfterInvalidReload();
        ConfigParsesCashGainMultiplier();
    }

    private static double Parse(string raw, double previous)
    {
        return GameplayTweaksConfigValueParser.ParseMultiplierOrKeep(
            raw,
            previous,
            "testMultiplier",
            _ => { });
    }

    private static void InvalidMultiplierWarnsWithKey()
    {
        var warnings = new List<string>();
        GameplayTweaksConfigValueParser.ParseMultiplierOrKeep(
            "-1",
            1.0,
            "tributeMultiplier",
            warnings.Add);

        if (warnings.Count != 1 ||
            !warnings[0].Contains("tributeMultiplier", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("invalid multiplier warning includes key");
        }
    }

    private static void ConfigParsesKnownKeysAndIgnoresUnknowns()
    {
        var config = GameplayTweaksConfig.ParseLines(
            new[]
            {
                "",
                "# 配置注释",
                "absorbEnabled=0",
                "tributeAttributeMultiplier=2.25",
                "unknownKey=ignored"
            },
            new GameplayTweaksConfig(),
            _ => { });

        TestAssert.False(config.AbsorbEnabled, "absorbEnabled=0 disables absorb");
        TestAssert.Near(2.25, config.TributeAttributeMultiplier, "config multiplier");
    }

    private static void ConfigParsesMixedCaseKeys()
    {
        var config = GameplayTweaksConfig.ParseLines(
            new[]
            {
                "AbSoRbEnAbLeD=0",
                "TrIbUtEaTtRiBuTeMuLtIpLiEr=2.25"
            },
            new GameplayTweaksConfig(),
            _ => { });

        TestAssert.False(config.AbsorbEnabled, "mixed-case absorbEnabled parses");
        TestAssert.Near(2.25, config.TributeAttributeMultiplier, "mixed-case tribute multiplier parses");
    }

    private static void ConfigKeepsPreviousMultiplierAfterInvalidReload()
    {
        var warnings = new List<string>();
        GameplayTweaksConfig config = new GameplayTweaksConfig();
        config = GameplayTweaksConfig.ParseLines(
            new[] { "tributeAttributeMultiplier=2.25" },
            config,
            warnings.Add);
        config = GameplayTweaksConfig.ParseLines(
            new[] { "tributeAttributeMultiplier=invalid" },
            config,
            warnings.Add);

        TestAssert.Near(2.25, config.TributeAttributeMultiplier, "invalid reload keeps previous multiplier");
        TestAssert.Equal(1, warnings.Count, "invalid reload writes one warning");
        if (!warnings[0].Contains("tributeAttributeMultiplier", StringComparison.Ordinal))
            throw new InvalidOperationException("invalid reload warning includes multiplier key");
    }

    private static void ConfigParsesCashGainMultiplier()
    {
        GameplayTweaksConfig config = GameplayTweaksConfig.ParseLines(
            new[] { "cashGainMultiplier=2.5" },
            new GameplayTweaksConfig(),
            _ => { });

        TestAssert.Near(2.5, config.CashGainMultiplier, "cash gain multiplier");
    }
}
