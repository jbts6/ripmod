using System;
using System.Collections.Generic;

internal static class MultiplierConfigTests
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
    }

    private static double Parse(string raw, double previous)
    {
        return QualityBoostConfigValueParser.ParseMultiplierOrKeep(
            raw,
            previous,
            "testMultiplier",
            _ => { });
    }

    private static void InvalidMultiplierWarnsWithKey()
    {
        var warnings = new List<string>();
        QualityBoostConfigValueParser.ParseMultiplierOrKeep(
            "-1",
            1.0,
            "tributeAttributeMultiplier",
            warnings.Add);

        if (warnings.Count != 1 ||
            !warnings[0].Contains("tributeAttributeMultiplier", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("invalid multiplier warning includes key");
        }
    }
}
