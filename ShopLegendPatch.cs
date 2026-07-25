using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2CppBattle;
using Il2CppRushShop;

internal sealed class ShopDepotSwapState
{
    private readonly ShopUnit _shop;
    private readonly DepoData _original;
    private bool _restorePending = true;

    public ShopDepotSwapState(ShopUnit shop, DepoData original)
    {
        _shop = shop;
        _original = original;
    }

    public void Restore()
    {
        if (!_restorePending)
            return;

        try
        {
            _shop.EquipDepo = _original;
            _restorePending = false;
        }
        catch (Exception ex)
        {
            QualityBoostMod.L?.Error("[Shop] restore EquipDepo failed: " + ex);
        }
    }
}

internal static class ShopLegendController
{
    private const string CommonKey = "CommonEquip";
    private const string RareKey = "RareEquip";
    private const string EpicKey = "EpicEquip";
    private const string LegendKey = "LegendEquip";
    private static string _lastWarning;
    private static string _lastLogSignature;

    public static ShopDepotSwapState TryBegin(ShopUnit shop)
    {
        if (!QualityBoostMod.Cfg.shopEnabled || shop == null)
            return null;

        DepoData originalDepot = shop.EquipDepo;
        var liveWeights = originalDepot?.LoadWeightDict;
        if (liveWeights == null || liveWeights.Count == 0)
        {
            WarnOnce("商店 EquipDepo 的运行时权重为空，已跳过本次调整。");
            return null;
        }

        Dictionary<string, float> copied = Snapshot(liveWeights);
        if (!TryReadQualityWeights(copied, out QualityWeights original))
        {
            WarnOnce(
                "商店 EquipDepo 缺少 CommonEquip/RareEquip/EpicEquip/LegendEquip，" +
                "已跳过本次调整。");
            return null;
        }

        QualityWeights adjusted =
            LegendQualityMath.ApplyMinimumLegendChance(
                original,
                QualityBoostMod.Cfg.shopLegendChance);
        if (AreEqual(original, adjusted))
            return null;

        var replacementWeights =
            new Il2CppSystem.Collections.Generic.Dictionary<string, float>();
        foreach (KeyValuePair<string, float> pair in copied)
            replacementWeights.Add(pair.Key, pair.Value);

        replacementWeights[CommonKey] = (float)adjusted.Common;
        replacementWeights[RareKey] = (float)adjusted.Rare;
        replacementWeights[EpicKey] = (float)adjusted.Epic;
        replacementWeights[LegendKey] = (float)adjusted.Legend;

        var replacementDepot = new DepoData(originalDepot.Name);
        replacementDepot.Clear();
        replacementDepot.Load(replacementWeights);

        var state = new ShopDepotSwapState(shop, originalDepot);
        try
        {
            shop.EquipDepo = replacementDepot;
            LogApplied(original, adjusted);
            return state;
        }
        catch
        {
            state.Restore();
            throw;
        }
    }

    private static Dictionary<string, float> Snapshot(
        Il2CppSystem.Collections.Generic.Dictionary<string, float> liveWeights)
    {
        var copied = new Dictionary<string, float>(
            liveWeights.Count,
            StringComparer.Ordinal);
        foreach (var pair in liveWeights)
            copied[pair.Key] = pair.Value;
        return copied;
    }

    private static bool TryReadQualityWeights(
        IReadOnlyDictionary<string, float> weights,
        out QualityWeights qualityWeights)
    {
        if (weights.TryGetValue(CommonKey, out float common) &&
            weights.TryGetValue(RareKey, out float rare) &&
            weights.TryGetValue(EpicKey, out float epic) &&
            weights.TryGetValue(LegendKey, out float legend))
        {
            qualityWeights = new QualityWeights(common, rare, epic, legend);
            return true;
        }

        qualityWeights = default;
        return false;
    }

    private static bool AreEqual(QualityWeights left, QualityWeights right)
    {
        return left.Common == right.Common &&
               left.Rare == right.Rare &&
               left.Epic == right.Epic &&
               left.Legend == right.Legend;
    }

    private static void WarnOnce(string warning)
    {
        if (_lastWarning == warning)
            return;

        _lastWarning = warning;
        QualityBoostMod.L?.Warning("[Shop] " + warning);
    }

    private static void LogApplied(
        QualityWeights original,
        QualityWeights adjusted)
    {
        string signature =
            $"{original.Common:R}|{original.Rare:R}|{original.Epic:R}|" +
            $"{original.Legend:R}|{adjusted.Legend:R}";
        if (_lastLogSignature == signature)
            return;

        _lastLogSignature = signature;
        QualityBoostMod.L?.Msg(
            "[Shop] equipment Legend floor applied: " +
            $"original={original.LegendProbability:P1} " +
            $"effective={adjusted.LegendProbability:P1}");
    }
}

internal static class ShopLegendPatchHooks
{
    public static void Prefix(
        ShopUnit shop,
        out ShopDepotSwapState state)
    {
        state = null;
        try
        {
            QualityBoostMod.ReloadCfg();
            state = ShopLegendController.TryBegin(shop);
        }
        catch (Exception ex)
        {
            state?.Restore();
            state = null;
            QualityBoostMod.L?.Error("[Shop] apply failed: " + ex);
        }
    }

    public static void Postfix(ShopDepotSwapState state)
    {
        state?.Restore();
    }

    public static Exception Finalizer(
        Exception exception,
        ShopDepotSwapState state)
    {
        state?.Restore();
        return exception;
    }
}

[HarmonyPatch(typeof(ShopUnit), "RefreshShopItem")]
internal static class ShopRefreshLegendPatch
{
    [HarmonyPriority(Priority.Last)]
    private static void Prefix(
        ShopUnit __instance,
        out ShopDepotSwapState __state)
    {
        ShopLegendPatchHooks.Prefix(__instance, out __state);
    }

    [HarmonyPriority(Priority.First)]
    private static void Postfix(ShopDepotSwapState __state)
    {
        ShopLegendPatchHooks.Postfix(__state);
    }

    [HarmonyPriority(Priority.First)]
    private static Exception Finalizer(
        Exception __exception,
        ShopDepotSwapState __state)
    {
        return ShopLegendPatchHooks.Finalizer(__exception, __state);
    }
}

[HarmonyPatch(typeof(ShopUnit), "RefreshShopItemWithoutLockItem")]
internal static class ShopRefreshWithoutLockLegendPatch
{
    [HarmonyPriority(Priority.Last)]
    private static void Prefix(
        ShopUnit __instance,
        out ShopDepotSwapState __state)
    {
        ShopLegendPatchHooks.Prefix(__instance, out __state);
    }

    [HarmonyPriority(Priority.First)]
    private static void Postfix(ShopDepotSwapState __state)
    {
        ShopLegendPatchHooks.Postfix(__state);
    }

    [HarmonyPriority(Priority.First)]
    private static Exception Finalizer(
        Exception __exception,
        ShopDepotSwapState __state)
    {
        return ShopLegendPatchHooks.Finalizer(__exception, __state);
    }
}
