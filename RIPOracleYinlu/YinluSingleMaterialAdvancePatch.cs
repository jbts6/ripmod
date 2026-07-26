using System;
using HarmonyLib;

[HarmonyPatch(
    typeof(Il2CppEquipEnhance.EquipEnhanceSystem),
    "RefreshProgress",
    new[] { typeof(float), typeof(float) })]
internal static class YinluSingleMaterialAdvancePatch
{
    [HarmonyPriority(Priority.Last)]
    private static void Prefix(
        Il2CppEquipEnhance.EquipEnhanceSystem __instance,
        ref float __0,
        ref float __1)
    {
        try
        {
            int selectedCount = __instance?.m_CurrentSelectedList?.Count ?? 0;
            YinluAdvanceDecision decision = YinluAdvanceDecision.Evaluate(
                selectedCount,
                __instance?.m_CurExp ?? 0f);

            if (decision.State == YinluAdvanceState.Empty)
            {
                __0 = decision.CurrentExp;
                __1 = decision.RequiredExp;
                return;
            }

            if (decision.State == YinluAdvanceState.Ready)
            {
                __instance.m_CurUpLvExp = decision.RequiredExp;
                __0 = 1f;
                __1 = 1f;
                return;
            }

            RIPOracleYinluMod.Logger?.Error(
                "[YinluAdvance] unexpected selected material count=" + selectedCount +
                "; keeping original progress rules.");
        }
        catch (Exception exception)
        {
            RIPOracleYinluMod.Logger?.Error(
                "[YinluAdvance] progress patch failed; keeping original behavior: " + exception);
        }
    }
}
