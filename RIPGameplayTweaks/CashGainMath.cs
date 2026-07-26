using System;

internal static class CashGainMath
{
    public static int ScalePositiveDelta(int oldValue, int proposedValue, double multiplier)
    {
        if (proposedValue <= oldValue ||
            multiplier <= 0 ||
            double.IsNaN(multiplier) ||
            double.IsInfinity(multiplier))
        {
            return proposedValue;
        }

        long delta = (long)proposedValue - oldValue;
        double scaledValue = oldValue + (delta * multiplier);
        if (scaledValue >= int.MaxValue)
            return int.MaxValue;
        if (scaledValue <= int.MinValue)
            return int.MinValue;

        double rounded = Math.Round(scaledValue, MidpointRounding.AwayFromZero);
        if (rounded >= int.MaxValue)
            return int.MaxValue;
        if (rounded <= int.MinValue)
            return int.MinValue;
        return (int)rounded;
    }
}
