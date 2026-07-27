using System;
using System.Collections.Generic;
using Il2CppSysCommon;
using Il2CppNZUI;
using HarmonyLib;

public static class TributeNameResolver
{
    private static readonly Dictionary<string, string> _names =
        new Dictionary<string, string>(StringComparer.Ordinal);

    public static string Resolve(string tributeId)
    {
        if (string.IsNullOrEmpty(tributeId))
            return null;
        return _names.TryGetValue(tributeId, out string name) ? name : null;
    }

    public static IReadOnlyDictionary<string, string> Snapshot() => _names;

    internal static void Register(string tributeId, string chineseName)
    {
        if (string.IsNullOrEmpty(tributeId) || string.IsNullOrEmpty(chineseName))
            return;
        lock (_names)
        {
            _names[tributeId] = chineseName;
        }
    }
}

[HarmonyPatch(typeof(NZUIHandle), "GetTunnelStrData")]
internal static class TributeNameCapturePatch
{
    static void Postfix(
        NZUIHandle __instance,
        Il2CppSysCommon.CommonObj commonObj,
        string tableName,
        string tunnelName,
        ref string value,
        bool __result)
    {
        try
        {
            if (!__result || commonObj == null || string.IsNullOrEmpty(value))
                return;

            string cnfId = commonObj.GetCnfID();
            if (string.IsNullOrEmpty(cnfId))
                cnfId = commonObj.GetMainCnf();

            if (string.IsNullOrEmpty(cnfId) || !cnfId.StartsWith("Tribute_", StringComparison.Ordinal))
                return;

            TributeNameResolver.Register(cnfId, value);
        }
        catch (Exception ex)
        {
            TributeForcerMod.Logger?.Error("[TributeForcer] name capture err: " + ex.Message);
        }
    }
}
