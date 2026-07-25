using System;
using System.Collections.Generic;

internal static class Program
{
    private const double Tolerance = 0.000001;

    private static int Main()
    {
        try
        {
            AssertNear(1.5, Parse("1.5", 1.0), "valid multiplier");
            AssertNear(1.0, Parse("not-a-number", 1.0), "invalid keeps previous");
            AssertNear(1.0, Parse("NaN", 1.0), "NaN keeps previous");
            AssertNear(1.0, Parse("Infinity", 1.0), "Infinity keeps previous");
            AssertNear(1.0, Parse("-1", 1.0), "negative keeps previous");
            AssertNear(1.0, Parse("101", 1.0), "over-limit keeps previous");
            InvalidMultiplierWarnsWithKey();
            ConfigParsesKnownKeysAndIgnoresUnknowns();
            AssertNear(15f, TributeMultiplierMath.Apply(10f, 1.5), "runtime scale");
            RuntimeScaleRejectsOverflow();
            AssertEqual("攻击+15%", TributeValueFormatter.Apply("攻击+10%", 1.5), "integer text");
            AssertEqual("速度-3.75", TributeValueFormatter.Apply("速度-2.5", 1.5), "decimal text");
            AssertEqual("", TributeValueFormatter.Apply("", 1.5), "empty text");
            AssertEqual("无数值", TributeValueFormatter.Apply("无数值", 1.5), "text without numbers");

            Console.WriteLine("ALL TESTS PASSED");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static double Parse(string raw, double previous)
    {
        return GameplayTweaksConfigValueParser.ParseMultiplierOrKeep(raw, previous, "testMultiplier", _ => { });
    }

    private static void InvalidMultiplierWarnsWithKey()
    {
        var warnings = new List<string>();
        GameplayTweaksConfigValueParser.ParseMultiplierOrKeep("-1", 1.0, "tributeMultiplier", warnings.Add);

        if (warnings.Count != 1 || !warnings[0].Contains("tributeMultiplier", StringComparison.Ordinal))
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

        if (config.AbsorbEnabled)
        {
            throw new InvalidOperationException("absorbEnabled=0 disables absorb");
        }

        AssertNear(2.25, config.TributeAttributeMultiplier, "config multiplier");
    }

    private static void RuntimeScaleRejectsOverflow()
    {
        try
        {
            TributeMultiplierMath.Apply(float.MaxValue, 2.0);
        }
        catch (ArgumentOutOfRangeException)
        {
            return;
        }

        throw new InvalidOperationException("runtime scale overflow throws");
    }

    private static void AssertNear(double expected, double actual, string description)
    {
        if (Math.Abs(expected - actual) > Tolerance)
        {
            throw new InvalidOperationException(description + ": expected " + expected + ", got " + actual);
        }
    }

    private static void AssertEqual(string expected, string actual, string description)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(description + ": expected '" + expected + "', got '" + actual + "'");
        }
    }
}
