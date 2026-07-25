using System;
using System.Collections.Generic;
using Il2CppBattle;
using Il2CppRushUser;

internal static class TributeWeightController
{
    private static readonly TributeWeightBaselineCache BaselineCache =
        new TributeWeightBaselineCache();

    private static IntPtr _lastLogPoolId;
    private static string _lastLogSignature;
    private static IntPtr _lastWarnedPoolId;
    private static string _lastDiagnosticSignature;

    public static void ApplyForRefresh(UserLevelUnit userLevel)
    {
        try
        {
            string depotName = userLevel?.CurAttrDepoName;
            var stage = StageMgr.CurNZStage;
            if (stage == null || string.IsNullOrEmpty(depotName))
                return;

            BaselineCache.BeginContext(stage.Pointer);

            if (!stage.TryGetDepo(depotName, out DepoData depo) || depo == null)
            {
                QualityBoostMod.L?.Warning($"[TributeWeight] 无法解析当前池 {depotName}，已跳过本次权重调整。");
                return;
            }

            Apply(depo, depotName);
        }
        catch (Exception ex)
        {
            QualityBoostMod.L?.Error("[TributeWeight] 解析当前池失败: " + ex);
        }
    }

    private static void Apply(DepoData depo, string requestedDepotName)
    {
        try
        {
            if (depo == null)
                return;

            string depotName = depo.Name;
            var liveWeights = depo.DefaultWeightDict;
            bool isLegendPool = TributeWeightCatalog.IsLegendWeightPool(depotName);
            LogDiagnosticOnce(requestedDepotName, depotName, depo, liveWeights, isLegendPool);

            if (!isLegendPool)
                return;

            if (liveWeights == null || liveWeights.Count == 0)
                return;

            IntPtr poolId = liveWeights.Pointer;
            Dictionary<string, float> liveSnapshot = Snapshot(liveWeights);
            IReadOnlyDictionary<string, float> original = BaselineCache.GetBaseline(
                poolId,
                liveSnapshot);
            CalculateBaseWeights(original, out double targetWeight, out double otherWeight, out int targetCount);
            if (targetCount == 0 || targetWeight <= 0.0)
            {
                WarnNoTargetsOnce(poolId, depotName);
                return;
            }

            double desiredChance = QualityBoostMod.Cfg.enabled
                ? QualityBoostMod.Cfg.shangshangChance
                : 0.0;
            double directMultiplier = QualityBoostMod.Cfg.enabled
                ? QualityBoostMod.Cfg.shangshangMultiplier
                : 1.0;
            double multiplier = TributeWeightMath.ResolveMultiplier(
                desiredChance,
                directMultiplier,
                targetWeight,
                otherWeight);
            double probability = TributeWeightMath.CalculateProbability(
                multiplier,
                targetWeight,
                otherWeight);

            Dictionary<string, float> scaled = TributeWeightMath.ScaleWeights(
                original,
                TributeWeightCatalog.TrueShangshangIds,
                multiplier);
            ApplyTargetWeights(liveWeights, original, scaled);
            BaselineCache.RecordExpected(poolId, scaled);
            LogAppliedOnce(
                poolId,
                depotName,
                targetCount,
                targetWeight,
                otherWeight,
                multiplier,
                probability);
        }
        catch (Exception ex)
        {
            QualityBoostMod.L?.Error("[TributeWeight] 调整失败: " + ex);
        }
    }

    private static void LogDiagnosticOnce(
        string requestedDepotName,
        string actualDepotName,
        DepoData depo,
        Il2CppSystem.Collections.Generic.Dictionary<string, float> defaultWeights,
        bool isLegendPool)
    {
        int defaultCount = defaultWeights?.Count ?? -1;
        int loadCount = depo.LoadWeightDict?.Count ?? -1;
        string decision = !isLegendPool
            ? "skip-name"
            : defaultCount <= 0 ? "skip-default-empty" : "apply";
        string signature =
            $"{requestedDepotName}|{actualDepotName}|{depo.Pointer}|{defaultCount}|{loadCount}|{decision}";
        if (_lastDiagnosticSignature == signature)
            return;

        _lastDiagnosticSignature = signature;
        QualityBoostMod.L?.Msg(
            $"[TributeWeightDiag] requested={requestedDepotName ?? "<null>"} " +
            $"actual={actualDepotName ?? "<null>"} depo=0x{depo.Pointer.ToInt64():X} " +
            $"defaultCount={defaultCount} loadCount={loadCount} decision={decision}");
    }

    private static Dictionary<string, float> Snapshot(
        Il2CppSystem.Collections.Generic.Dictionary<string, float> liveWeights)
    {
        var snapshot = new Dictionary<string, float>(liveWeights.Count, StringComparer.Ordinal);
        foreach (var pair in liveWeights)
            snapshot[pair.Key] = pair.Value;
        return snapshot;
    }

    private static void CalculateBaseWeights(
        IReadOnlyDictionary<string, float> original,
        out double targetWeight,
        out double otherWeight,
        out int targetCount)
    {
        targetWeight = 0.0;
        otherWeight = 0.0;
        targetCount = 0;

        foreach (KeyValuePair<string, float> pair in original)
        {
            if (TributeWeightCatalog.TrueShangshangIds.Contains(pair.Key))
            {
                targetWeight += pair.Value;
                targetCount++;
            }
            else
            {
                otherWeight += pair.Value;
            }
        }
    }

    private static void ApplyTargetWeights(
        Il2CppSystem.Collections.Generic.Dictionary<string, float> liveWeights,
        IReadOnlyDictionary<string, float> original,
        Dictionary<string, float> scaled)
    {
        foreach (string id in TributeWeightCatalog.TrueShangshangIds)
        {
            if (original.ContainsKey(id) && scaled.TryGetValue(id, out float value))
                liveWeights[id] = value;
        }
    }

    private static void WarnNoTargetsOnce(IntPtr poolId, string depotName)
    {
        if (_lastWarnedPoolId == poolId)
            return;

        _lastWarnedPoolId = poolId;
        QualityBoostMod.L?.Warning($"[TributeWeight] 池 {depotName} 未找到已验证的真上上签配置，已跳过。");
    }

    private static void LogAppliedOnce(
        IntPtr poolId,
        string depotName,
        int targetCount,
        double targetWeight,
        double otherWeight,
        double multiplier,
        double probability)
    {
        string signature = $"{depotName}|{targetCount}|{targetWeight:F3}|{otherWeight:F3}|{multiplier:F6}";
        if (_lastLogPoolId == poolId && _lastLogSignature == signature)
            return;

        _lastLogPoolId = poolId;
        _lastLogSignature = signature;
        QualityBoostMod.L?.Msg(
            $"[TributeWeight] pool={depotName} matched={targetCount} " +
            $"baseUpperUpper={targetWeight:F1} adjustedUpperUpper={targetWeight * multiplier:F1} " +
            $"other={otherWeight:F1} multiplier={multiplier:F4} effectiveBaseChance={probability:P2}");
    }
}
