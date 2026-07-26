using System;
using HarmonyLib;

[HarmonyPatch(
    typeof(Il2CppRushTalent.TalentSys),
    nameof(Il2CppRushTalent.TalentSys.CalTalentValueInternal),
    new[] { typeof(string), typeof(float), typeof(string), typeof(bool) })]
internal static class TributeRuntimeMultiplierPatch
{
    private static void Prefix(ref float __1)
    {
        try
        {
            QualityBoostMod.ReloadCfg();
            double multiplier = QualityBoostMod.Cfg.tributeAttributeMultiplier;
            if (multiplier != 1.0)
                __1 = TributeMultiplierMath.Apply(__1, multiplier);
        }
        catch (Exception exception)
        {
            QualityBoostMod.L?.Error("[Tribute] runtime multiplier failed: " + exception);
        }
    }
}

[HarmonyPatch(
    typeof(Il2CppRushTalent.ViewTalentDetails),
    nameof(Il2CppRushTalent.ViewTalentDetails.GetValue),
    new[]
    {
        typeof(Il2CppRushTalent.TalentAttrSubObj),
        typeof(Il2CppRushTalent.TalentObj)
    })]
internal static class TributeDisplayMultiplierPatch
{
    private static void Postfix(ref string __result)
    {
        try
        {
            QualityBoostMod.ReloadCfg();
            __result = TributeValueFormatter.Apply(
                __result,
                QualityBoostMod.Cfg.tributeAttributeMultiplier);
        }
        catch (Exception exception)
        {
            QualityBoostMod.L?.Error("[Tribute] display multiplier failed: " + exception);
        }
    }
}
