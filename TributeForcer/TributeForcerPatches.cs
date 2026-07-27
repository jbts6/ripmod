using System;
using System.Collections.Generic;
using Il2CppBattle;
using Il2CppRushUser;
using Il2CppStage;
using HarmonyLib;

[HarmonyPatch(typeof(UserLevelUnit), "UI_PickAttrRefresh_Keyboard_Data")]
internal static class TributeForcerRefreshPatch
{
    private static readonly HashSet<string> ForcedIds = new HashSet<string>(StringComparer.Ordinal);
    private static bool _appliedThisRefresh;

    public static void SetForcedIds(IEnumerable<string> ids)
    {
        ForcedIds.Clear();
        if (ids != null)
        {
            foreach (string id in ids)
                ForcedIds.Add(id);
        }
    }

    public static void Clear() => ForcedIds.Clear();

    public static bool HasForcedIds => ForcedIds.Count > 0;

    [HarmonyPriority(Priority.Last)]
    static void Prefix(UserLevelUnit __instance)
    {
        try
        {
            if (!TributeForcerMod.Config.Enabled || ForcedIds.Count == 0)
                return;

            ApplyForcedWeights(__instance);
            _appliedThisRefresh = true;
        }
        catch (Exception ex)
        {
            TributeForcerMod.Logger?.Error("[TributeForcer] refresh prefix err: " + ex.Message);
        }
    }

    static void Postfix()
    {
        if (_appliedThisRefresh)
        {
            _appliedThisRefresh = false;
            ForcedIds.Clear();
            TributeForcerMod.Logger?.Msg("[TributeForcer] 强制贡品已应用，等待下次刷新生效。");
        }
    }

    private static void ApplyForcedWeights(UserLevelUnit userLevel)
    {
        string depotName = userLevel?.CurAttrDepoName;
        var stage = StageMgr.CurNZStage;
        if (stage == null || string.IsNullOrEmpty(depotName))
            return;

        if (!stage.TryGetDepo(depotName, out DepoData depo) || depo == null)
        {
            TributeForcerMod.Logger?.Warning("[TributeForcer] 无法解析当前仓库 " + depotName);
            return;
        }

        var weights = depo.DefaultWeightDict;
        if (weights == null || weights.Count == 0)
            return;

        var keys = new List<string>();
        foreach (var pair in weights)
            keys.Add(pair.Key);

        int matchCount = 0;
        foreach (string key in keys)
        {
            if (ForcedIds.Contains(key))
            {
                weights[key] = 1000f;
                matchCount++;
            }
            else
            {
                weights[key] = 0f;
            }
        }

        TributeForcerMod.Logger?.Msg(
            $"[TributeForcer] 强制刷出: 仓库={depotName} 命中={matchCount}/{ForcedIds.Count} 总项={keys.Count}");
    }
}
