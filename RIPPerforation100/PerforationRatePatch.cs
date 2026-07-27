using System;
using HarmonyLib;
using Il2CppSysCommon;

[HarmonyPatch(
    typeof(CommonObj),
    nameof(CommonObj.FetchFloatValue),
    new[] { typeof(string), typeof(float) })]
internal static class PerforationRatePatch
{
    [HarmonyPriority(Priority.Last)]
    private static void Postfix(string __0, ref float __result)
    {
        try
        {
            if (PerforationRateLogic.TryGetForcedRate(__0, out float forced))
                __result = forced;
        }
        catch (Exception exception)
        {
            RIPPerforation100Mod.Logger?.Error(
                "[Perforation] rate override failed: " + exception);
        }
    }
}
