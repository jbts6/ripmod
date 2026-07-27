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
            RIPOracleYinluMod.Logger?.Msg("[CascadeFuse] 原版一键动画播放中，稍后再按 G");
            return false;
        }

        OracleFuseSys fuse = view.OracleFuse;
        if (fuse == null || fuse.WasCollected)
        {
            RIPOracleYinluMod.Logger?.Warning("[CascadeFuse] oracle fuse system unavailable");
            return false;
        }

        if (!fuse.HasBatchFuseAvailableOnce())
        {
            RIPOracleYinluMod.Logger?.Msg("[CascadeFuse] 当前没有可批量合成的命石");
            RefreshUi(view, fuse);
            return false;
        }

        _busy = true;
        var allRecords = new List<OracleFuseRecord>();
        try
        {
            RIPOracleYinluMod.Logger?.Msg("[CascadeFuse] start (G) — fusing all tiers…");

            OracleCascadeFuseResult result = OracleCascadeFuseLogic.Run(
                () => fuse.HasBatchFuseAvailableOnce(),
                () => TryFuseOnce(fuse, allRecords),
                OracleCascadeFuseLogic.DefaultMaxRounds);

            RefreshUi(view, fuse);
            ShowResultFeedback(view, allRecords, result);

            RIPOracleYinluMod.Logger?.Msg(
                "[CascadeFuse] done attempted=" + result.RoundsAttempted +
                " succeeded=" + result.RoundsSucceeded +
                " records=" + allRecords.Count +
                " stop=" + result.StopReason +
                (result.HitRoundCap ? " (hit round cap)" : string.Empty));
            return result.RoundsSucceeded > 0;
        }
        catch (Exception exception)
        {
            RIPOracleYinluMod.Logger?.Error("[CascadeFuse] failed: " + exception);
            try { RefreshUi(view, fuse); } catch { /* ignore secondary */ }
            return false;
        }
        finally
        {
            _busy = false;
        }
    }

    private static bool TryFuseOnce(OracleFuseSys fuse, List<OracleFuseRecord> allRecords)
    {
        List<OracleFuseRecord> records = null;
        if (!fuse.TryBatchFuseAvailableOnce(out records))
            return false;

        if (records != null)
        {
            for (int i = 0; i < records.Count; i++)
                allRecords.Add(records[i]);
        }

        return true;
    }

    private static void RefreshUi(ViewOracleFuseSys view, OracleFuseSys fuse)
    {
        if (view == null || view.WasCollected)
            return;

        try
        {
            if (fuse != null && !fuse.WasCollected)
                fuse.AutoSort();
        }
        catch (Exception exception)
        {
            RIPOracleYinluMod.Logger?.Warning("[CascadeFuse] AutoSort failed: " + exception.Message);
        }

        try
        {
            // true = sort while refreshing, same as clicking 排序
            view.RefreshStoneFuseEquipList(true);
        }
        catch (Exception exception)
        {
            RIPOracleYinluMod.Logger?.Warning(
                "[CascadeFuse] RefreshStoneFuseEquipList failed: " + exception.Message);
        }

        try
        {
            view.RefreshUniversalRuneStoneUI();
        }
        catch
        {
            // optional UI path
        }

        try
        {
            view.UpdateScrollList();
        }
        catch
        {
            // optional UI path
        }
    }

    private static void ShowResultFeedback(
        ViewOracleFuseSys view,
        List<OracleFuseRecord> allRecords,
        OracleCascadeFuseResult result)
    {
        if (view == null || view.WasCollected)
            return;

        try
        {
            if (allRecords != null && allRecords.Count > 0)
            {
                // Il2Cpp List is not typed as IReadOnlyList in interop; runtime accepts the same object.
                var tipList = (Il2CppSystem.Collections.Generic.IReadOnlyList<OracleFuseRecord>)(object)allRecords;
                view.ShowOneKeyFuseTips(tipList);
                view.RefreshOneKeyFuseCompleteView(tipList);
            }
        }
        catch (Exception exception)
        {
            RIPOracleYinluMod.Logger?.Warning(
                "[CascadeFuse] native tip view failed, using log only: " + exception.Message);
            try
            {
                // Fallback: show tip text for the last few records via log.
                int shown = 0;
                for (int i = allRecords.Count - 1; i >= 0 && shown < 8; i--, shown++)
                {
                    string line = view.BuildOneKeyFuseTipText(allRecords[i]);
                    if (!string.IsNullOrEmpty(line))
                        RIPOracleYinluMod.Logger?.Msg("[CascadeFuse] " + line);
                }
            }
            catch
            {
                // ignore
            }
        }

        string summary = result.RoundsSucceeded <= 0
            ? "[CascadeFuse] 未合成任何命石"
            : "[CascadeFuse] 完成：成功 " + result.RoundsSucceeded +
              " 轮，产出记录 " + (allRecords == null ? 0 : allRecords.Count) + " 条（列表已刷新）";
        RIPOracleYinluMod.Logger?.Msg(summary);
    }
}
