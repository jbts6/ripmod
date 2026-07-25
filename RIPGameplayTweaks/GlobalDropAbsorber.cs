using System;
using Il2CppBattle;
using Il2CppInterop.Runtime;
using Il2CppStage;
using UnityEngine;

public static class GlobalDropAbsorber
{
    private static readonly AbsorbOperationGate Gate = new AbsorbOperationGate();
    private static readonly Il2CppSystem.Action<Il2CppSystem.Collections.Generic.List<IDrop>> _onAbsorbed =
        DelegateSupport.ConvertDelegate<Il2CppSystem.Action<Il2CppSystem.Collections.Generic.List<IDrop>>>(
            (Action<Il2CppSystem.Collections.Generic.List<IDrop>>)OnAbsorbed);
    private static DropSys _activeDropSys;

    public static bool TryAbsorbAll()
    {
        if (!Gate.TryBegin())
            return false;

        try
        {
            NZStage nzStage = UnityEngine.Object.FindObjectOfType<NZStage>();
            if (nzStage == null)
                return CompleteWithoutAbsorb();

            StageSys stage = nzStage.CurStage;
            if (stage == null)
                return CompleteWithoutAbsorb();

            stage.TryGetFeature<DropSys>(out DropSys dropSys);
            if (dropSys == null)
                return CompleteWithoutAbsorb();

            stage.TryGetFeature<BattleStageInfoSys>(out BattleStageInfoSys stageInfo);
            if (stageInfo == null)
                return CompleteWithoutAbsorb();

            RigidObj host = stageInfo.HostView;
            if (host == null)
                return CompleteWithoutAbsorb();

            Transform target = host.GetTransformNode();
            if (target == null)
                return CompleteWithoutAbsorb();

            _activeDropSys = dropSys;
            dropSys.GlobalAbsorbDrops(target, _onAbsorbed);
            return true;
        }
        catch (Exception exception)
        {
            LogError("[Absorb] global absorb failed: " + exception);
            _activeDropSys = null;
            Gate.Complete();
            return false;
        }
    }

    private static bool CompleteWithoutAbsorb()
    {
        Gate.Complete();
        return false;
    }

    private static void OnAbsorbed(Il2CppSystem.Collections.Generic.List<IDrop> dropList)
    {
        try
        {
            _activeDropSys?.CalPick(dropList);
        }
        catch (Exception exception)
        {
            LogError("[Absorb] settlement failed: " + exception);
        }
        finally
        {
            _activeDropSys = null;
            Gate.Complete();
        }
    }

    private static void LogError(string message)
    {
        RIPGameplayTweaksMod.Logger?.Error(message);
    }
}
