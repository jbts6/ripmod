using System;

public readonly struct QualityWeights
{
    public QualityWeights(double common, double rare, double epic, double legend)
    {
        Common = common;
        Rare = rare;
        Epic = epic;
        Legend = legend;
    }

    public double Common { get; }
    public double Rare { get; }
    public double Epic { get; }
    public double Legend { get; }
    public double Total => Common + Rare + Epic + Legend;
    public double LegendProbability => Legend / Total;
}

public static class LegendQualityMath
{
    public static QualityWeights ApplyMinimumLegendChance(
        QualityWeights original,
        double minimumLegendChance)
    {
        Validate(original, minimumLegendChance);

        double total = original.Total;
        if (original.Legend / total >= minimumLegendChance)
            return original;

        double lowerQualityTotal =
            original.Common + original.Rare + original.Epic;
        double legend = total * minimumLegendChance;
        double lowerQualityScale = (total - legend) / lowerQualityTotal;

        return new QualityWeights(
            original.Common * lowerQualityScale,
            original.Rare * lowerQualityScale,
            original.Epic * lowerQualityScale,
            legend);
    }

    private static void Validate(
        QualityWeights weights,
        double minimumLegendChance)
    {
        if (!IsFinite(minimumLegendChance) ||
            minimumLegendChance < 0.0 ||
            minimumLegendChance > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimumLegendChance),
                "Legend 概率下限必须位于 0 到 1 之间。");
        }

        ValidateWeight(weights.Common, nameof(weights.Common));
        ValidateWeight(weights.Rare, nameof(weights.Rare));
        ValidateWeight(weights.Epic, nameof(weights.Epic));
        ValidateWeight(weights.Legend, nameof(weights.Legend));

        if (!IsFinite(weights.Total) || weights.Total <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(weights),
                "品质总权重必须为有限正数。");
        }
    }

    private static void ValidateWeight(double weight, string name)
    {
        if (!IsFinite(weight) || weight < 0.0)
            throw new ArgumentOutOfRangeException(name, "品质权重必须为有限非负数。");
    }

    private static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
