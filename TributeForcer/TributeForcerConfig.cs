using System;
using System.Collections.Generic;
using System.Linq;

public sealed class TributeForcerConfig
{
    public bool Enabled { get; private set; } = true;

    public static TributeForcerConfig ParseLines(
        IEnumerable<string> lines,
        TributeForcerConfig previous)
    {
        TributeForcerConfig result = previous ?? new TributeForcerConfig();
        if (lines == null)
            return result;

        foreach (string rawLine in lines)
        {
            string line = rawLine == null ? string.Empty : rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                continue;

            int sep = line.IndexOf('=');
            if (sep <= 0)
                continue;

            string key = line.Substring(0, sep).Trim();
            string value = line.Substring(sep + 1).Trim();
            if (string.Equals(key, "enabled", StringComparison.OrdinalIgnoreCase))
            {
                if (value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase))
                    result = result.WithEnabled(true);
                else if (value == "0" || string.Equals(value, "false", StringComparison.OrdinalIgnoreCase))
                    result = result.WithEnabled(false);
            }
        }
        return result;
    }

    public IEnumerable<string> Serialize()
    {
        yield return "# TributeForcer 配置";
        yield return "# 1/true 启用强制刷出, 0/false 禁用。";
        yield return "enabled=" + (Enabled ? "1" : "0");
    }

    private TributeForcerConfig WithEnabled(bool value)
    {
        return new TributeForcerConfig { Enabled = value };
    }

    private TributeForcerConfig() { }

    public static TributeForcerConfig CreateDefault() => new TributeForcerConfig();
}
