using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
            ConfigParsesMixedCaseKeys();
            ConfigKeepsPreviousMultiplierAfterInvalidReload();
            AbsorbGateRejectsOverlappingOperations();
            AbsorbGateAllowsOneConcurrentBegin();
            AbsorbCoordinatorIgnoresDuplicateAndLateCallbacks();
            AbsorbCoordinatorCancelReleasesOnlyMatchingOperation();
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

        AssertFalse(config.AbsorbEnabled, "mixed-case absorbEnabled parses");
        AssertNear(2.25, config.TributeAttributeMultiplier, "mixed-case tribute multiplier parses");
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

        AssertNear(2.25, config.TributeAttributeMultiplier, "invalid reload keeps previous multiplier");
        AssertEqual(1, warnings.Count, "invalid reload writes one warning");
        if (!warnings[0].Contains("tributeAttributeMultiplier", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("invalid reload warning includes multiplier key");
        }
    }

    private static void AbsorbGateRejectsOverlappingOperations()
    {
        var gate = new AbsorbOperationGate();
        AssertTrue(gate.TryBegin(), "first absorb begins");
        AssertFalse(gate.TryBegin(), "overlapping absorb rejected");
        gate.Complete();
        AssertTrue(gate.TryBegin(), "new absorb allowed after completion");
        gate.Complete();
        gate.Complete();
        AssertFalse(gate.IsPending, "completion is idempotent");
    }

    private static void AbsorbGateAllowsOneConcurrentBegin()
    {
        var gate = new AbsorbOperationGate();
        int started = 0;
        Parallel.For(0, 128, _ =>
        {
            if (gate.TryBegin())
                Interlocked.Increment(ref started);
        });

        AssertEqual(1, started, "only one concurrent absorb begins");
        gate.Complete();
        AssertFalse(gate.IsPending, "concurrent gate completion releases operation");
    }

    private static void AbsorbCoordinatorIgnoresDuplicateAndLateCallbacks()
    {
        var coordinator = new AbsorbOperationCoordinator<string>();
        var settlements = new List<string>();

        AssertTrue(coordinator.TryBegin("first", out AbsorbOperation<string> first), "first operation begins");
        AssertTrue(coordinator.TryComplete(first, value => settlements.Add(value)), "first callback settles");
        AssertFalse(coordinator.TryComplete(first, value => settlements.Add(value)), "duplicate callback ignored");

        AssertTrue(coordinator.TryBegin("second", out AbsorbOperation<string> second), "second operation begins");
        AssertFalse(coordinator.TryComplete(first, value => settlements.Add(value)), "late old callback ignored");
        AssertTrue(coordinator.IsPending, "late old callback keeps second operation pending");
        AssertTrue(coordinator.TryComplete(second, value => settlements.Add(value)), "second callback settles");

        AssertEqual("first,second", string.Join(",", settlements), "callbacks settle their own operations once");
        AssertFalse(coordinator.IsPending, "second completion releases gate");
    }

    private static void AbsorbCoordinatorCancelReleasesOnlyMatchingOperation()
    {
        var coordinator = new AbsorbOperationCoordinator<string>();
        AssertTrue(coordinator.TryBegin("failed", out AbsorbOperation<string> failed), "failed operation begins");
        coordinator.Cancel(failed);
        AssertFalse(coordinator.IsPending, "sync exception cleanup releases failed operation");

        AssertTrue(coordinator.TryBegin("active", out AbsorbOperation<string> active), "new operation begins after cleanup");
        coordinator.Cancel(failed);
        AssertTrue(coordinator.IsPending, "old cleanup cannot release new operation");
        coordinator.Cancel(active);
        AssertFalse(coordinator.IsPending, "matching cleanup releases active operation");
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

    private static void AssertEqual(int expected, int actual, string description)
    {
        if (expected != actual)
        {
            throw new InvalidOperationException(description + ": expected " + expected + ", got " + actual);
        }
    }

    private static void AssertTrue(bool value, string description)
    {
        if (!value)
            throw new InvalidOperationException(description + ": expected true");
    }

    private static void AssertFalse(bool value, string description)
    {
        if (value)
            throw new InvalidOperationException(description + ": expected false");
    }
}
