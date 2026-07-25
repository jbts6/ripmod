using System;
using System.Globalization;

public static class QualityBoostConfigValueParser
{
    public static double ParseDoubleOrKeep(
        string raw,
        double previous,
        string key,
        Action<string> warning)
    {
        if (double.TryParse(
                raw,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out double parsed) &&
            !double.IsNaN(parsed) &&
            !double.IsInfinity(parsed))
        {
            return parsed;
        }

        warning?.Invoke(
            $"{key}='{raw ?? "null"}' 不是有效有限数字，保留 {previous:R}。");
        return previous;
    }
}
