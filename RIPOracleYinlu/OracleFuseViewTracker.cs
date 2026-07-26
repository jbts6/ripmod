using HarmonyLib;
using Il2CppRushOracle;

/// <summary>
/// Tracks the active oracle fuse view so cascade fuse only runs while the UI is open.
/// </summary>
internal static class OracleFuseViewTracker
{
    public static ViewOracleFuseSys ActiveView { get; private set; }

    public static void SetActive(ViewOracleFuseSys view)
    {
        ActiveView = view;
    }

    public static void ClearIfMatch(ViewOracleFuseSys view)
    {
        if (ActiveView != null && ActiveView == view)
            ActiveView = null;
    }

    public static void Clear()
    {
        ActiveView = null;
    }
}

[HarmonyPatch(typeof(ViewOracleFuseSys), nameof(ViewOracleFuseSys.OpenFuseUI))]
internal static class OracleFuseOpenFuseUiPatch
{
    private static void Postfix(ViewOracleFuseSys __instance)
    {
        OracleFuseViewTracker.SetActive(__instance);
    }
}

[HarmonyPatch(typeof(ViewOracleFuseSys), nameof(ViewOracleFuseSys.OpenOracleFuse))]
internal static class OracleFuseOpenOracleFusePatch
{
    private static void Postfix(ViewOracleFuseSys __instance)
    {
        OracleFuseViewTracker.SetActive(__instance);
    }
}

[HarmonyPatch(typeof(ViewOracleFuseSys), nameof(ViewOracleFuseSys.CloseFuseUI))]
internal static class OracleFuseCloseFuseUiPatch
{
    private static void Postfix(ViewOracleFuseSys __instance)
    {
        OracleFuseViewTracker.ClearIfMatch(__instance);
    }
}
