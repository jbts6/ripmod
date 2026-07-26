using System;
using System.Globalization;

public static class GameplayTweaksConfigValueParser
{
    public static double ParseMultiplierOrKeep(string raw, double previous, string key, Action<string> warn)
    {
        double value;
        if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
            double.IsNaN(value) ||
            double.IsInfinity(value) ||
            value <= 0 ||
            value > 100)
        {
            warn("Invalid multiplier for " + key + ": '" + raw + "'. Keeping previous value.");
            return previous;
        }

        return value;
    }
}
