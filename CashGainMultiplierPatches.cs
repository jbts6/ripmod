using System;
using HarmonyLib;

[HarmonyPatch(typeof(Il2CppLocalData.ArchiveData), nameof(Il2CppLocalData.ArchiveData.SetData))]
internal static class CashGainArchiveRestorePatch
{
    private static void Prefix()
    {
        CashGainContext.BeginRestoreScope();
    }

    private static Exception Finalizer(Exception __exception)
    {
        CashGainContext.EndRestoreScope();
        return __exception;
    }
}

[HarmonyPatch(
    typeof(Il2CppBattle.BattleUserObj),
    nameof(Il2CppBattle.BattleUserObj.CacheIntValue),
    new[] { typeof(string), typeof(int) })]
internal static class CashGainBattleCachePatch
{
    private static void Prefix(
        Il2CppBattle.BattleUserObj __instance,
        string __0)
    {
        try
        {
            CashGainContext.EndAcquisition();
            if (!CashGainKey.IsCashKey(__0))
                return;

            QualityBoostMod.ReloadCfg();
            int oldValue = __instance.FetchIntValue(CashGainKey.Name, 0);
            CashGainContext.TryBeginAcquisition(
                __instance.Pointer,
                CashGainKey.Name,
                oldValue,
                QualityBoostMod.Cfg.cashGainMultiplier);
        }
        catch (Exception exception)
        {
            QualityBoostMod.L?.Error(
                "[CashGain] battle cache context failed; keeping original value: " + exception);
            CashGainContext.EndAcquisition();
        }
    }

    private static Exception Finalizer(Exception __exception)
    {
        CashGainContext.EndAcquisition();
        return __exception;
    }
}

[HarmonyPatch(
    typeof(Il2CppSysCommon.CommonObj),
    nameof(Il2CppSysCommon.CommonObj.CacheIntValue),
    new[] { typeof(string), typeof(int) })]
internal static class CashGainCommonCachePatch
{
    private static void Prefix(
        Il2CppSysCommon.CommonObj __instance,
        string __0,
        ref int __1)
    {
        try
        {
            if (!CashGainKey.IsCashKey(__0))
                return;

            int scaledValue;
            if (CashGainContext.TryScale(
                    __instance.Pointer,
                    CashGainKey.Name,
                    __1,
                    out scaledValue))
            {
                __1 = scaledValue;
            }
        }
        catch (Exception exception)
        {
            QualityBoostMod.L?.Error(
                "[CashGain] common cache scaling failed; keeping original value: " + exception);
        }
    }
}
