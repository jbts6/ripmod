using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2CppEquipPerforation;
using Il2CppSysCommon;

[HarmonyPatch(
    typeof(EquipPerforationSystem),
    nameof(EquipPerforationSystem.CheckUse),
    new[] { typeof(CommonObj) })]
internal static class PerforationMaterialPatch
{
    private static bool Prefix(
        EquipPerforationSystem __instance,
        CommonObj __0,
        ref bool __result)
    {
        try
        {
            bool hasSlotTarget =
                __instance?.View?.m_PerforationSlotView?.Data != null;
            __result = PerforationMaterialLogic.ShouldReject(
                hasSlotTarget,
                __0?.GetUUID(),
                CollectEquippedUuids(__instance));
            return false;
        }
        catch (Exception exception)
        {
            RIPPerforation100Mod.Logger?.Error(
                "[Perforation] material override failed, falling back to original: " +
                exception);
            return true;
        }
    }

    private static IReadOnlyList<string> CollectEquippedUuids(
        EquipPerforationSystem system)
    {
        var uuids = new List<string>();
        var slotEquips = system?.m_EquipSys?.GetSlotEquip();
        if (slotEquips == null)
            return uuids;

        for (int index = 0; index < slotEquips.Count; index++)
        {
            string uuid = slotEquips[index]?.GetUUID();
            if (!string.IsNullOrEmpty(uuid))
                uuids.Add(uuid);
        }

        return uuids;
    }
}
