using System;
using HarmonyLib;
using Il2CppEquipEnhance;
using Il2CppSysCommon;

internal sealed class YinluOverrideContext
{
    public YinluOverrideContext(
        IntPtr targetPointer,
        YinluQualityOverrideMap values)
    {
        TargetPointer = targetPointer;
        Values = values;
    }

    public IntPtr TargetPointer { get; }
    public YinluQualityOverrideMap Values { get; }
}

internal sealed class YinluOverrideState
{
    private readonly YinluOverrideContext _applied;
    private readonly YinluOverrideContext _previous;
    private bool _restorePending = true;

    public YinluOverrideState(
        YinluOverrideContext applied,
        YinluOverrideContext previous)
    {
        _applied = applied;
        _previous = previous;
    }

    public void Restore()
    {
        if (!_restorePending)
            return;

        if (ReferenceEquals(YinluLegendController.CurrentOverride, _applied))
        {
            _restorePending = false;
            YinluLegendController.CurrentOverride = _previous;
            return;
        }

        QualityBoostMod.L?.Warning(
            "[Yinlu] override context changed before restore; " +
            "the newer context was preserved.");
    }
}

internal static class YinluLegendController
{
    private const double SumTolerance = 0.000001;
    private static DateTime _lastLogUtc = DateTime.MinValue;

    [ThreadStatic]
    private static YinluOverrideContext _currentOverride;

    internal static YinluOverrideContext CurrentOverride
    {
        get => _currentOverride;
        set => _currentOverride = value;
    }

    public static YinluOverrideState TryBegin(EquipEnhanceObj enhanceObj)
    {
        if (!QualityBoostMod.Cfg.yinluEnabled || enhanceObj == null)
            return null;

        double common = FetchEffectiveWeight(
            enhanceObj,
            EquipEnhanceObj.Common_Prop);
        double rare = FetchEffectiveWeight(
            enhanceObj,
            EquipEnhanceObj.Rare_Prop);
        double epic = FetchEffectiveWeight(
            enhanceObj,
            EquipEnhanceObj.Epic_Prop);
        double declaredLegend = FetchEffectiveWeight(
            enhanceObj,
            EquipEnhanceObj.Legend_Prop);

        double lowerQualityTotal = common + rare + epic;
        if (lowerQualityTotal > 1.0 + SumTolerance)
        {
            throw new InvalidOperationException(
                $"前三档品质概率之和为 {lowerQualityTotal:R}，超过 1。");
        }

        double actualLegend = Math.Max(0.0, 1.0 - lowerQualityTotal);
        var original = new QualityWeights(common, rare, epic, actualLegend);
        QualityWeights adjusted =
            LegendQualityMath.ApplyMinimumLegendChance(
                original,
                QualityBoostMod.Cfg.yinluLegendChance);
        if (adjusted.Legend <= actualLegend)
            return null;

        var context = new YinluOverrideContext(
            enhanceObj.Pointer,
            new YinluQualityOverrideMap(adjusted));
        var state = new YinluOverrideState(context, CurrentOverride);
        CurrentOverride = context;
        try
        {
            LogApplied(
                common,
                rare,
                epic,
                actualLegend,
                declaredLegend,
                adjusted.Legend);
            return state;
        }
        catch
        {
            state.Restore();
            throw;
        }
    }

    public static bool TryOverride(
        CommonObj source,
        string key,
        out float value)
    {
        YinluOverrideContext current = CurrentOverride;
        if (current != null &&
            source != null &&
            source.Pointer == current.TargetPointer)
        {
            return current.Values.TryGetValue(key, out value);
        }

        value = default;
        return false;
    }

    private static float FetchEffectiveWeight(
        EquipEnhanceObj enhanceObj,
        string key)
    {
        return enhanceObj.FetchFloatValue(key, 0f);
    }

    private static void LogApplied(
        double common,
        double rare,
        double epic,
        double actualLegend,
        double declaredLegend,
        double adjustedLegend)
    {
        DateTime now = DateTime.UtcNow;
        if ((now - _lastLogUtc).TotalSeconds < 0.20)
            return;

        _lastLogUtc = now;
        QualityBoostMod.L?.Msg(
            "[Yinlu] Legend floor applied: " +
            $"base={common:P1}/{rare:P1}/{epic:P1}/{actualLegend:P1} " +
            $"declared={declaredLegend:P1} effective={adjustedLegend:P1}");
    }
}

[HarmonyPatch(
    typeof(CommonObj),
    nameof(CommonObj.FetchFloatValue),
    new[] { typeof(string), typeof(float) })]
internal static class YinluFloatValueOverridePatch
{
    [HarmonyPriority(Priority.Last)]
    private static void Postfix(
        CommonObj __instance,
        string __0,
        ref float __result)
    {
        if (YinluLegendController.TryOverride(
                __instance,
                __0,
                out float overridden))
        {
            __result = overridden;
        }
    }
}

[HarmonyPatch(typeof(EquipEnhanceSystem), "GetRandomQuality")]
internal static class YinluLegendPatch
{
    [HarmonyPriority(Priority.Last)]
    private static void Prefix(
        EquipEnhanceObj __0,
        out YinluOverrideState __state)
    {
        __state = null;
        try
        {
            QualityBoostMod.ReloadCfg();
            __state = YinluLegendController.TryBegin(__0);
        }
        catch (Exception ex)
        {
            __state?.Restore();
            __state = null;
            QualityBoostMod.L?.Error("[Yinlu] apply failed: " + ex);
        }
    }

    [HarmonyPriority(Priority.First)]
    private static void Postfix(YinluOverrideState __state)
    {
        __state?.Restore();
    }

    [HarmonyPriority(Priority.First)]
    private static Exception Finalizer(
        Exception __exception,
        YinluOverrideState __state)
    {
        __state?.Restore();
        return __exception;
    }
}
