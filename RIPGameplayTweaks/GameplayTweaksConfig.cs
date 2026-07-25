using System;
using System.Collections.Generic;

public sealed class GameplayTweaksConfig
{
    public bool AbsorbEnabled { get; private set; } = true;
    public double TributeAttributeMultiplier { get; private set; } = 1.5;

    public static GameplayTweaksConfig ParseLines(
        IEnumerable<string> lines,
        GameplayTweaksConfig previous,
        Action<string> warn)
    {
        GameplayTweaksConfig source = previous ?? new GameplayTweaksConfig();
        var result = new GameplayTweaksConfig
        {
            AbsorbEnabled = source.AbsorbEnabled,
            TributeAttributeMultiplier = source.TributeAttributeMultiplier
        };

        if (lines == null)
            return result;

        foreach (string rawLine in lines)
        {
            string line = rawLine == null ? string.Empty : rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                continue;

            int separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
                continue;

            string key = line.Substring(0, separatorIndex).Trim();
            string value = line.Substring(separatorIndex + 1).Trim();
            if (string.Equals(key, "absorbEnabled", StringComparison.OrdinalIgnoreCase))
            {
                result.AbsorbEnabled = ParseBoolOrKeep(value, result.AbsorbEnabled, key, warn);
            }
            else if (string.Equals(key, "tributeAttributeMultiplier", StringComparison.OrdinalIgnoreCase))
            {
                result.TributeAttributeMultiplier = GameplayTweaksConfigValueParser.ParseMultiplierOrKeep(
                    value,
                    result.TributeAttributeMultiplier,
                    key,
                    warn);
            }
        }

        return result;
    }

    private static bool ParseBoolOrKeep(string value, bool previous, string key, Action<string> warn)
    {
        if (value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase))
            return true;
        if (value == "0" || string.Equals(value, "false", StringComparison.OrdinalIgnoreCase))
            return false;

        warn?.Invoke("Invalid " + key + "; keeping previous value.");
        return previous;
    }
}
