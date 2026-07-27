using System;
using System.Collections.Generic;
using System.IO;
using Il2CppBattle;
using Il2CppRushUser;
using Il2CppStage;
using HarmonyLib;

[HarmonyPatch(typeof(UserLevelUnit), "UI_PickAttrRefresh_Keyboard_Data")]
internal static class TributeDebugDumpPatch
{
    private static bool _dumped;

    static void Prefix(UserLevelUnit __instance)
    {
        if (_dumped) return;
        _dumped = true;

        try
        {
            string depotName = __instance?.CurAttrDepoName;
            var stage = StageMgr.CurNZStage;
            if (stage == null || string.IsNullOrEmpty(depotName)) return;

            if (!stage.TryGetDepo(depotName, out DepoData depo) || depo == null) return;
            var weights = depo.DefaultWeightDict;
            if (weights == null) return;

            var lines = new List<string> {
                $"# Depot: {depotName}",
                $"# Count: {weights.Count}",
                "# Format: ID | Weight | Rarity"
            };

            foreach (var pair in weights)
            {
                string rarity = TributeCatalog.ClassifyRarity(pair.Key);
                lines.Add($"{pair.Key} | {pair.Value} | {rarity}");
            }

            string path = Path.Combine(
                Path.GetDirectoryName(typeof(TributeForcerMod).Assembly.Location) ?? "",
                "..", "UserData", "TributeDump.txt");
            File.WriteAllLines(path, lines);
            TributeForcerMod.Logger?.Msg("[TributeForcer] 贡品列表已 dump 到 " + path);
        }
        catch (Exception ex)
        {
            TributeForcerMod.Logger?.Error("[TributeForcer] dump err: " + ex.Message);
        }
    }
}
