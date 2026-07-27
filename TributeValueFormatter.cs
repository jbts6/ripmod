using System;
using System.Globalization;
using System.Text.RegularExpressions;

public static class TributeValueFormatter
{
    private static readonly Regex NumberPattern = new Regex("[+-]?\\d+(?:\\.\\d+)?", RegexOptions.Compiled);

    public static string Apply(string text, double multiplier)
    {
        return NumberPattern.Replace(text, match => FormatScaledValue(match.Value, multiplier));
    }

    private static string FormatScaledValue(string raw, double multiplier)
    {
        double value;
        if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            return raw;
        }

        var scaledValue = value * multiplier;
        var result = raw.Contains(".", StringComparison.Ordinal)
            ? scaledValue.ToString("0.##", CultureInfo.InvariantCulture)
            : Math.Round(scaledValue).ToString("0", CultureInfo.InvariantCulture);

        return raw.StartsWith("+", StringComparison.Ordinal) && scaledValue >= 0 && !result.StartsWith("+", StringComparison.Ordinal)
            ? "+" + result
            : result;
    }
}
