using System;
using Il2CppRushOracle;
using Il2CppSystem.Collections.Generic;
using UnityEngine;

internal static class OracleCascadeFuseRunner
{
    private static bool _busy;

    public static bool TryRunFromHotkey()
    {
        if (_busy)
            return false;

        if (!UnityEngine.Input.GetKeyDown(KeyCode.G))
            return false;

        ViewOracleFuseSys view = OracleFuseViewTracker.ActiveView;
        if (view == null || view.WasCollected)
            return false;

        if (view.isOneKeyOracleFusePlaying)
        {
            RIPOracleYinluMod.Logger?.Msg("[CascadeFuse] skipped: native one-key fuse is playing");
            return false;
        }

        OracleFuseSys fuse = view.OracleFuse;
        if (fuse == null || fuse.WasCollected)
        {
            RIPOracleYinluMod.Logger?.Warning("[CascadeFuse] oracle fuse system unavailable");
            return false;
        }

        _busy = true;
        try
        {
            OracleCascadeFuseResult result = OracleCascadeFuseLogic.Run(
                () => fuse.HasBatchFuseAvailableOnce(),
                () => TryFuseOnce(fuse),
                OracleCascadeFuseLogic.DefaultMaxRounds);

            RIPOracleYinluMod.Logger?.Msg(
                "[CascadeFuse] done attempted=" + result.RoundsAttempted +
                " succeeded=" + result.RoundsSucceeded +
                " stop=" + result.StopReason +
                (result.HitRoundCap ? " (hit round cap)" : string.Empty));
            return result.RoundsSucceeded > 0 || result.RoundsAttempted > 0;
        }
        catch (Exception exception)
        {
            RIPOracleYinluMod.Logger?.Error("[CascadeFuse] failed: " + exception);
            return false;
        }
        finally
        {
            _busy = false;
        }
    }

    private static bool TryFuseOnce(OracleFuseSys fuse)
    {
        List<OracleFuseRecord> records = null;
        return fuse.TryBatchFuseAvailableOnce(out records);
    }
}
