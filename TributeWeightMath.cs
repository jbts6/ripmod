using System;
using System.Collections.Generic;

public static class TributeWeightMath
{
    public static double ResolveMultiplier(
        double desiredProbability,
        double directMultiplier,
        double targetWeight,
        double otherWeight)
    {
        return desiredProbability > 0.0
            ? CalculateMultiplier(desiredProbability, targetWeight, otherWeight)
            : ValidateAndReturnMultiplier(directMultiplier);
    }

    public static double CalculateMultiplier(
        double desiredProbability,
        double targetWeight,
        double otherWeight)
    {
        ValidateProbability(desiredProbability);
        ValidateWeights(targetWeight, otherWeight);
        if (otherWeight <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(otherWeight), "设置目标概率时，其他权重必须为有限正数。");

        return desiredProbability * otherWeight /
               (targetWeight * (1.0 - desiredProbability));
    }

    public static double CalculateProbability(
        double multiplier,
        double targetWeight,
        double otherWeight)
    {
        ValidateMultiplier(multiplier);
        ValidateWeights(targetWeight, otherWeight);

        double scaledTargetWeight = targetWeight * multiplier;
        double totalWeight = scaledTargetWeight + otherWeight;
        if (!IsFinite(totalWeight) || totalWeight <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(multiplier), "缩放后的总权重必须为有限正数。");

        return scaledTargetWeight / totalWeight;
    }

    public static Dictionary<string, float> ScaleWeights(
        IReadOnlyDictionary<string, float> originalWeights,
        ISet<string> targetIds,
        double multiplier)
    {
        if (originalWeights == null)
            throw new ArgumentNullException(nameof(originalWeights));
        if (targetIds == null)
            throw new ArgumentNullException(nameof(targetIds));

        ValidateMultiplier(multiplier);

        var result = new Dictionary<string, float>(originalWeights.Count, StringComparer.Ordinal);
        foreach (KeyValuePair<string, float> pair in originalWeights)
        {
            if (pair.Value < 0f || float.IsNaN(pair.Value) || float.IsInfinity(pair.Value))
                throw new ArgumentOutOfRangeException(nameof(originalWeights), "权重必须为有限非负数。");

            double value = targetIds.Contains(pair.Key)
                ? pair.Value * multiplier
                : pair.Value;
            if (!IsFinite(value) || value > float.MaxValue)
                throw new OverflowException($"配置 {pair.Key} 的缩放权重超出 Single 范围。");

            result[pair.Key] = (float)value;
        }

        return result;
    }

    private static void ValidateProbability(double probability)
    {
        if (!IsFinite(probability) || probability <= 0.0 || probability >= 1.0)
            throw new ArgumentOutOfRangeException(nameof(probability), "目标概率必须大于 0 且小于 1。");
    }

    private static void ValidateWeights(double targetWeight, double otherWeight)
    {
        if (!IsFinite(targetWeight) || targetWeight <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(targetWeight), "目标权重必须为有限正数。");
        if (!IsFinite(otherWeight) || otherWeight < 0.0)
            throw new ArgumentOutOfRangeException(nameof(otherWeight), "其他权重必须为有限非负数。");
    }

    private static void ValidateMultiplier(double multiplier)
    {
        if (!IsFinite(multiplier) || multiplier < 0.0)
            throw new ArgumentOutOfRangeException(nameof(multiplier), "权重倍率必须为有限非负数。");
    }

    private static double ValidateAndReturnMultiplier(double multiplier)
    {
        ValidateMultiplier(multiplier);
        return multiplier;
    }

    private static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
