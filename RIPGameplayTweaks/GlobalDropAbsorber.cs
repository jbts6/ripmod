using System;
using System.Collections.Generic;
using Il2CppBattle;
using Il2CppInterop.Runtime;
using Il2CppStage;
using UnityEngine;

public static class GlobalDropAbsorber
{
    private static readonly AbsorbOperationCoordinator<DropSys> Operations =
        new AbsorbOperationCoordinator<DropSys>();
    private static readonly List<IDrop> PendingDrops = new List<IDrop>();
    private static AbsorbOperation<DropSys> CurrentOperation;
    private static bool IsCollecting;
    private static float LastCallbackTime;
    private const float CallbackGapTimeout = 0.3f;

    public static bool TryAbsorbAll()
    {
        AbsorbOperation<DropSys> operation = null;

        try
        {
            if (IsCollecting)
                return false;

            NZStage nzStage = UnityEngine.Object.FindObjectOfType<NZStage>();
            if (nzStage == null)
                return false;

            StageSys stage = nzStage.CurStage;
            if (stage == null)
                return false;

            stage.TryGetFeature<DropSys>(out DropSys dropSys);
            if (dropSys == null)
                return false;

            stage.TryGetFeature<BattleStageInfoSys>(out BattleStageInfoSys stageInfo);
            if (stageInfo == null)
                return false;

            RigidObj host = stageInfo.HostView;
            if (host == null)
                return false;

            Transform target = host.GetTransformNode();
            if (target == null)
                return false;

            if (!Operations.TryBegin(dropSys, out operation))
                return false;

            CurrentOperation = operation;
            PendingDrops.Clear();
            IsCollecting = true;
            LastCallbackTime = Time.time;

            var callback = DelegateSupport.ConvertDelegate<
                Il2CppSystem.Action<Il2CppSystem.Collections.Generic.List<IDrop>>>(
                (Action<Il2CppSystem.Collections.Generic.List<IDrop>>)(dropList =>
                    OnAbsorbed(operation, dropList)));
            operation.SetCallback(callback);
            dropSys.GlobalAbsorbDrops(target, callback);
            return true;
        }
        catch (Exception exception)
        {
            LogError("[Absorb] global absorb failed: " + exception);
            IsCollecting = false;
            PendingDrops.Clear();
            Operations.Cancel(CurrentOperation);
            CurrentOperation = null;
            return false;
        }
    }

    public static void Update()
    {
        if (!IsCollecting)
            return;

        if (PendingDrops.Count > 0 && Time.time - LastCallbackTime > CallbackGapTimeout)
        {
            CompleteAbsorb();
        }
    }

    private static void CompleteAbsorb()
    {
        if (!IsCollecting)
            return;

        IsCollecting = false;
        var drops = PendingDrops.ToArray();
        PendingDrops.Clear();
        var operation = CurrentOperation;
        CurrentOperation = null;

        if (drops.Length == 0)
        {
            Operations.Cancel(operation);
            return;
        }

        LogInfo("[Absorb] completing with total drops=" + drops.Length);

        try
        {
            var il2cppList = new Il2CppSystem.Collections.Generic.List<IDrop>();
            foreach (var drop in drops)
            {
                if (drop != null)
                    il2cppList.Add(drop);
            }

            Operations.TryComplete(operation, dropSys => dropSys.CalPick(il2cppList));
        }
        catch (Exception exception)
        {
            LogError("[Absorb] settlement failed: " + exception);
        }
    }

    private static void OnAbsorbed(
        AbsorbOperation<DropSys> operation,
        Il2CppSystem.Collections.Generic.List<IDrop> dropList)
    {
        try
        {
            LastCallbackTime = Time.time;
            int count = dropList?.Count ?? 0;
            if (count > 0 && dropList != null)
            {
                for (int i = 0; i < count; i++)
                {
                    var drop = dropList[i];
                    if (drop != null)
                        PendingDrops.Add(drop);
                }
            }
            LogInfo("[Absorb] callback received count=" + count + ", total pending=" + PendingDrops.Count);
        }
        catch (Exception exception)
        {
            LogError("[Absorb] callback failed: " + exception);
        }
    }

    private static void LogInfo(string message)
    {
        RIPGameplayTweaksMod.Logger?.Msg(message);
    }

    private static void LogError(string message)
    {
        RIPGameplayTweaksMod.Logger?.Error(message);
    }
}
