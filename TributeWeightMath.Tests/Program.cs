using System;
using System.Collections.Generic;

internal static class Program
{
    private const double Tolerance = 0.000001;

    private static int Main()
    {
        try
        {
            CalculateMultiplierMatchesPoolFormula();
            CalculateProbabilityMatchesScaledWeights();
            ConfiguredChanceTakesPriorityOverDirectMultiplier();
            InvalidProbabilityIsRejected();
            ProbabilityTargetRejectsPoolWithoutOtherWeight();
            ScaleWeightsChangesOnlyTargetEntries();
            MinimumLegendChanceRaisesLowerProbability();
            MinimumLegendChancePreservesHigherOriginalProbability();
            MinimumLegendChanceSupportsOneHundredPercent();
            MinimumLegendChancePreservesLowerQualityRatios();
            InvalidQualityWeightsAreRejected();
            YinluOverrideMapReturnsOnlyAdjustedQualityWeights();
            InvalidConfigNumberKeepsPreviousValueAndWarns();
            ValidConfigNumberUsesInvariantCultureWithoutWarning();
            TrueShangshangCatalogContainsOnlyVerifiedPoolEntries();
            LegendPoolNameMatchingCoversRuntimeAliases();
            RepeatedRefreshUsesOriginalBaselineInsteadOfCompounding();
            ExternalWeightChangeRefreshesBaseline();
            ExternalNonTargetChangePreservesOurTargetBaseline();
            AlternatingPoolsReuseEachPoolBaselineInsteadOfCompounding();
            SharedDictionaryIdentityReusesTheSameBaseline();
            ChangingStageContextDropsObsoleteBaselines();
            Console.WriteLine("ALL TESTS PASSED");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("TEST FAILED: " + ex.Message);
            return 1;
        }
    }

    private static void CalculateMultiplierMatchesPoolFormula()
    {
        double actual = TributeWeightMath.CalculateMultiplier(0.60, 6400.0, 6752.0);
        AssertNear(1.5825, actual, nameof(CalculateMultiplierMatchesPoolFormula));
    }

    private static void CalculateProbabilityMatchesScaledWeights()
    {
        double actual = TributeWeightMath.CalculateProbability(2.0, 6400.0, 6752.0);
        double expected = 12800.0 / 19552.0;
        AssertNear(expected, actual, nameof(CalculateProbabilityMatchesScaledWeights));
    }

    private static void InvalidProbabilityIsRejected()
    {
        AssertThrows<ArgumentOutOfRangeException>(() =>
            TributeWeightMath.CalculateMultiplier(1.0, 6400.0, 6752.0));
    }

    private static void ProbabilityTargetRejectsPoolWithoutOtherWeight()
    {
        AssertThrows<ArgumentOutOfRangeException>(() =>
            TributeWeightMath.CalculateMultiplier(0.60, 6400.0, 0.0));
    }

    private static void ConfiguredChanceTakesPriorityOverDirectMultiplier()
    {
        double actual = TributeWeightMath.ResolveMultiplier(0.60, 99.0, 6400.0, 6752.0);
        AssertNear(1.5825, actual, nameof(ConfiguredChanceTakesPriorityOverDirectMultiplier));

        actual = TributeWeightMath.ResolveMultiplier(0.0, 2.25, 6400.0, 6752.0);
        AssertNear(2.25, actual, "zero target chance uses direct multiplier");
    }

    private static void ScaleWeightsChangesOnlyTargetEntries()
    {
        var original = new Dictionary<string, float>
        {
            ["upper-upper-a"] = 160f,
            ["upper"] = 100f,
            ["upper-upper-b"] = 80f
        };
        var targets = new HashSet<string>(StringComparer.Ordinal)
        {
            "upper-upper-a",
            "upper-upper-b"
        };

        Dictionary<string, float> scaled = TributeWeightMath.ScaleWeights(original, targets, 2.5);

        AssertNear(400.0, scaled["upper-upper-a"], "first target weight");
        AssertNear(200.0, scaled["upper-upper-b"], "second target weight");
        AssertNear(100.0, scaled["upper"], "non-target weight");
        AssertNear(160.0, original["upper-upper-a"], "input dictionary remains unchanged");
    }

    private static void MinimumLegendChanceRaisesLowerProbability()
    {
        var original = new QualityWeights(0.20, 0.35, 0.33, 0.12);

        QualityWeights adjusted =
            LegendQualityMath.ApplyMinimumLegendChance(original, 0.70);

        AssertNear(1.0, adjusted.Total, "raised total weight");
        AssertNear(0.70, adjusted.LegendProbability, "raised legend probability");
    }

    private static void MinimumLegendChancePreservesHigherOriginalProbability()
    {
        var original = new QualityWeights(0.05, 0.10, 0.05, 0.80);

        QualityWeights adjusted =
            LegendQualityMath.ApplyMinimumLegendChance(original, 0.70);

        AssertNear(original.Common, adjusted.Common, "preserved common weight");
        AssertNear(original.Rare, adjusted.Rare, "preserved rare weight");
        AssertNear(original.Epic, adjusted.Epic, "preserved epic weight");
        AssertNear(original.Legend, adjusted.Legend, "preserved legend weight");
    }

    private static void MinimumLegendChanceSupportsOneHundredPercent()
    {
        var original = new QualityWeights(0.35, 0.37, 0.03, 0.10);

        QualityWeights adjusted =
            LegendQualityMath.ApplyMinimumLegendChance(original, 1.0);

        AssertNear(0.0, adjusted.Common, "100 percent common weight");
        AssertNear(0.0, adjusted.Rare, "100 percent rare weight");
        AssertNear(0.0, adjusted.Epic, "100 percent epic weight");
        AssertNear(original.Total, adjusted.Legend, "100 percent legend weight");
        AssertNear(1.0, adjusted.LegendProbability, "100 percent legend probability");
    }

    private static void MinimumLegendChancePreservesLowerQualityRatios()
    {
        var original = new QualityWeights(0.20, 0.35, 0.33, 0.12);

        QualityWeights adjusted =
            LegendQualityMath.ApplyMinimumLegendChance(original, 0.70);

        AssertNear(
            original.Common / original.Rare,
            adjusted.Common / adjusted.Rare,
            "common to rare ratio");
        AssertNear(
            original.Rare / original.Epic,
            adjusted.Rare / adjusted.Epic,
            "rare to epic ratio");
    }

    private static void InvalidQualityWeightsAreRejected()
    {
        AssertThrows<ArgumentOutOfRangeException>(() =>
            LegendQualityMath.ApplyMinimumLegendChance(
                new QualityWeights(-0.1, 0.5, 0.5, 0.1),
                0.70));
        AssertThrows<ArgumentOutOfRangeException>(() =>
            LegendQualityMath.ApplyMinimumLegendChance(
                new QualityWeights(0.0, 0.0, 0.0, 0.0),
                0.70));
        AssertThrows<ArgumentOutOfRangeException>(() =>
            LegendQualityMath.ApplyMinimumLegendChance(
                new QualityWeights(0.2, 0.3, 0.4, 0.1),
                1.01));
    }

    private static void YinluOverrideMapReturnsOnlyAdjustedQualityWeights()
    {
        QualityWeights adjusted = LegendQualityMath.ApplyMinimumLegendChance(
            new QualityWeights(0.20, 0.35, 0.33, 0.12),
            0.70);
        var map = new YinluQualityOverrideMap(adjusted);

        AssertMapValue(map, "CommonProp", adjusted.Common);
        AssertMapValue(map, "RareProp", adjusted.Rare);
        AssertMapValue(map, "EpicProp", adjusted.Epic);
        AssertMapValue(map, "LegendProp", adjusted.Legend);

        if (map.TryGetValue("UnrelatedProp", out _))
            throw new InvalidOperationException("unrelated key was overridden");
    }

    private static void AssertMapValue(
        YinluQualityOverrideMap map,
        string key,
        double expected)
    {
        if (!map.TryGetValue(key, out float actual))
            throw new InvalidOperationException($"missing Yinlu override for {key}");
        AssertNear(expected, actual, $"Yinlu override {key}");
    }

    private static void InvalidConfigNumberKeepsPreviousValueAndWarns()
    {
        string warning = null;

        double actual = QualityBoostConfigValueParser.ParseDoubleOrKeep(
            "not-a-number",
            0.70,
            "yinluLegendChance",
            message => warning = message);

        AssertNear(0.70, actual, "invalid config keeps previous value");
        if (warning == null ||
            !warning.Contains("yinluLegendChance") ||
            !warning.Contains("not-a-number"))
        {
            throw new InvalidOperationException(
                "invalid config value did not produce a useful warning");
        }

        warning = null;
        actual = QualityBoostConfigValueParser.ParseDoubleOrKeep(
            "NaN",
            0.70,
            "shopLegendChance",
            message => warning = message);
        AssertNear(0.70, actual, "NaN config keeps previous value");
        if (warning == null)
            throw new InvalidOperationException("NaN config did not warn");
    }

    private static void ValidConfigNumberUsesInvariantCultureWithoutWarning()
    {
        int warningCount = 0;

        double actual = QualityBoostConfigValueParser.ParseDoubleOrKeep(
            "0.85",
            0.70,
            "shopLegendChance",
            _ => warningCount++);

        AssertNear(0.85, actual, "valid config value");
        if (warningCount != 0)
            throw new InvalidOperationException("valid config value produced a warning");
    }

    private static void TrueShangshangCatalogContainsOnlyVerifiedPoolEntries()
    {
        ISet<string> ids = TributeWeightCatalog.TrueShangshangIds;
        if (ids.Count != 40)
            throw new InvalidOperationException($"catalog count: expected 40, actual {ids.Count}");
        if (!ids.Contains("Tribute_Rare_Tr003"))
            throw new InvalidOperationException("catalog is missing Tribute_Rare_Tr003");
        if (!ids.Contains("Tribute_Lengendary_Tr001"))
            throw new InvalidOperationException("catalog is missing the game's Lengendary spelling");
        if (ids.Contains("Tribute_Rare_Tr004"))
            throw new InvalidOperationException("catalog includes an unverified entry");
    }

    private static void LegendPoolNameMatchingCoversRuntimeAliases()
    {
        if (!TributeWeightCatalog.IsLegendWeightPool("DropTributeLegend"))
            throw new InvalidOperationException("base legend pool was not recognized");
        if (!TributeWeightCatalog.IsLegendWeightPool("DropTributeLegendLv1"))
            throw new InvalidOperationException("verified level-one legend pool was not recognized");
        if (TributeWeightCatalog.IsLegendWeightPool("DropTributeLegend3"))
            throw new InvalidOperationException("unverified numbered legend pool was recognized");
        if (!TributeWeightCatalog.IsLegendWeightPool("RareTribute"))
            throw new InvalidOperationException("runtime alias RareTribute was not recognized");
        if (TributeWeightCatalog.IsLegendWeightPool("DropTributeEpic"))
            throw new InvalidOperationException("unrelated pool was recognized");
    }

    private static void RepeatedRefreshUsesOriginalBaselineInsteadOfCompounding()
    {
        var cache = new TributeWeightBaselineCache();
        var originalLive = new Dictionary<string, float>
        {
            ["target"] = 100f,
            ["other"] = 80f
        };
        IReadOnlyDictionary<string, float> first = cache.GetBaseline(
            new IntPtr(1), originalLive);
        AssertNear(100.0, first["target"], "first baseline target");

        var expectedAfterApply = new Dictionary<string, float>
        {
            ["target"] = 250f,
            ["other"] = 80f
        };
        cache.RecordExpected(new IntPtr(1), expectedAfterApply);

        IReadOnlyDictionary<string, float> second = cache.GetBaseline(
            new IntPtr(1), expectedAfterApply);
        AssertNear(100.0, second["target"], "repeated refresh baseline target");
    }

    private static void ExternalWeightChangeRefreshesBaseline()
    {
        var cache = new TributeWeightBaselineCache();
        var originalLive = new Dictionary<string, float>
        {
            ["target"] = 100f,
            ["other"] = 80f
        };
        cache.GetBaseline(new IntPtr(1), originalLive);
        cache.RecordExpected(new IntPtr(1), new Dictionary<string, float>
        {
            ["target"] = 200f,
            ["other"] = 80f
        });

        var externallyChanged = new Dictionary<string, float>
        {
            ["target"] = 150f,
            ["other"] = 120f
        };
        IReadOnlyDictionary<string, float> refreshed = cache.GetBaseline(
            new IntPtr(1), externallyChanged);

        AssertNear(150.0, refreshed["target"], "external target change becomes new baseline");
        AssertNear(120.0, refreshed["other"], "external other change becomes new baseline");
    }

    private static void ExternalNonTargetChangePreservesOurTargetBaseline()
    {
        var cache = new TributeWeightBaselineCache();
        cache.GetBaseline(new IntPtr(1), new Dictionary<string, float>
        {
            ["target"] = 100f,
            ["other"] = 80f
        });
        cache.RecordExpected(new IntPtr(1), new Dictionary<string, float>
        {
            ["target"] = 250f,
            ["other"] = 80f
        });

        IReadOnlyDictionary<string, float> merged = cache.GetBaseline(
            new IntPtr(1),
            new Dictionary<string, float>
            {
                ["target"] = 250f,
                ["other"] = 120f
            });

        AssertNear(100.0, merged["target"], "unchanged applied target keeps original baseline");
        AssertNear(120.0, merged["other"], "externally changed other becomes new baseline");
    }

    private static void AlternatingPoolsReuseEachPoolBaselineInsteadOfCompounding()
    {
        var cache = new TributeWeightBaselineCache();
        var poolAOriginal = new Dictionary<string, float>
        {
            ["target"] = 100f,
            ["other"] = 80f
        };
        var poolBOriginal = new Dictionary<string, float>
        {
            ["target"] = 60f,
            ["other"] = 40f
        };

        cache.GetBaseline(new IntPtr(1), poolAOriginal);
        cache.RecordExpected(new IntPtr(1), new Dictionary<string, float>
        {
            ["target"] = 250f,
            ["other"] = 80f
        });

        cache.GetBaseline(new IntPtr(2), poolBOriginal);
        cache.RecordExpected(new IntPtr(2), new Dictionary<string, float>
        {
            ["target"] = 150f,
            ["other"] = 40f
        });

        IReadOnlyDictionary<string, float> poolAAgain = cache.GetBaseline(
            new IntPtr(1),
            new Dictionary<string, float>
            {
                ["target"] = 250f,
                ["other"] = 80f
            });

        AssertNear(100.0, poolAAgain["target"], "alternating pool A baseline target");
        AssertNear(80.0, poolAAgain["other"], "alternating pool A baseline other");
    }

    private static void SharedDictionaryIdentityReusesTheSameBaseline()
    {
        var cache = new TributeWeightBaselineCache();
        var original = new Dictionary<string, float>
        {
            ["target"] = 100f,
            ["other"] = 80f
        };

        cache.GetBaseline(new IntPtr(1), original);
        cache.RecordExpected(new IntPtr(1), new Dictionary<string, float>
        {
            ["target"] = 250f,
            ["other"] = 80f
        });

        IReadOnlyDictionary<string, float> sharedBaseline = cache.GetBaseline(
            new IntPtr(1),
            new Dictionary<string, float>
            {
                ["target"] = 250f,
                ["other"] = 80f
            });

        AssertNear(100.0, sharedBaseline["target"], "shared dictionary baseline target");
        AssertNear(80.0, sharedBaseline["other"], "shared dictionary baseline other");
    }

    private static void ChangingStageContextDropsObsoleteBaselines()
    {
        var cache = new TributeWeightBaselineCache();
        cache.BeginContext(new IntPtr(10));
        cache.GetBaseline(new IntPtr(1), new Dictionary<string, float>
        {
            ["target"] = 100f,
            ["other"] = 80f
        });
        cache.RecordExpected(new IntPtr(1), new Dictionary<string, float>
        {
            ["target"] = 250f,
            ["other"] = 80f
        });

        cache.BeginContext(new IntPtr(10));
        IReadOnlyDictionary<string, float> sameContextBaseline = cache.GetBaseline(
            new IntPtr(1),
            new Dictionary<string, float>
            {
                ["target"] = 250f,
                ["other"] = 80f
            });
        AssertNear(100.0, sameContextBaseline["target"], "same context preserves baseline");

        cache.BeginContext(new IntPtr(20));
        IReadOnlyDictionary<string, float> newContextBaseline = cache.GetBaseline(
            new IntPtr(1),
            new Dictionary<string, float>
            {
                ["target"] = 250f,
                ["other"] = 80f
            });

        AssertNear(250.0, newContextBaseline["target"], "new context establishes fresh baseline");
    }

    private static void AssertNear(double expected, double actual, string name)
    {
        if (Math.Abs(expected - actual) > Tolerance)
            throw new InvalidOperationException($"{name}: expected {expected}, actual {actual}");
    }

    private static void AssertThrows<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }

        throw new InvalidOperationException($"expected exception {typeof(T).Name}");
    }
}
