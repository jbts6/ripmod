using System;
using Il2CppBattle;
using Il2CppInterop.Runtime;
using Il2CppStage;
using UnityEngine;

public static class GlobalDropAbsorber
{
    private static readonly AbsorbOperationCoordinator<DropSys> Operations =
        new AbsorbOperationCoordinator<DropSys>();

    public static bool TryAbsorbAll()
    {
        AbsorbOperation<DropSys> operation = null;

        try
        {
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
            Operations.Cancel(operation);
            return false;
        }
    }

    private static void OnAbsorbed(
        AbsorbOperation<DropSys> operation,
        Il2CppSystem.Collections.Generic.List<IDrop> dropList)
    {
        try
        {
            Operations.TryComplete(operation, dropSys => dropSys.CalPick(dropList));
        }
        catch (Exception exception)
        {
            LogError("[Absorb] settlement failed: " + exception);
        }
    }

    private static void LogError(string message)
    {
        RIPGameplayTweaksMod.Logger?.Error(message);
    }
}
