using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class TributeForcerConfig
{
    public bool Enabled { get; private set; } = true;

    /// <summary>开关界面快捷键，默认 F7。可在 cfg 里写 hotkey=F8 等 Unity KeyCode 名。</summary>
    public KeyCode ToggleKey { get; private set; } = KeyCode.F7;

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
            else if (string.Equals(key, "hotkey", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(key, "toggleKey", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(key, "key", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseKeyCode(value, out KeyCode kc))
                    result = result.WithToggleKey(kc);
            }
        }
        return result;
    }

    public static bool TryParseKeyCode(string value, out KeyCode key)
    {
        key = KeyCode.None;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string v = value.Trim();
        // 常见别名
        if (string.Equals(v, "esc", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(v, "escape", StringComparison.OrdinalIgnoreCase))
        {
            key = KeyCode.Escape;
            return true;
        }

        if (Enum.TryParse(v, ignoreCase: true, out KeyCode parsed) && parsed != KeyCode.None)
        {
            key = parsed;
            return true;
        }

        // F1-F12 数字简写：f7 / F7
        if (v.Length >= 2 && (v[0] == 'f' || v[0] == 'F') &&
            int.TryParse(v.Substring(1), out int fn) && fn >= 1 && fn <= 15)
        {
            if (Enum.TryParse("F" + fn, true, out KeyCode fk))
            {
                key = fk;
                return true;
            }
        }

        return false;
    }

    public IEnumerable<string> Serialize()
    {
        yield return "# TributeForcer 配置";
        yield return "# 1/true 启用强制刷出, 0/false 禁用。";
        yield return "enabled=" + (Enabled ? "1" : "0");
        yield return "# 开关界面快捷键（Unity KeyCode 名，如 F7 / F8 / Alpha0 / Keypad0）";
        yield return "# 打开时按 ESC 也可关闭（与 hotkey 无关）。";
        yield return "hotkey=" + ToggleKey;
    }

    private TributeForcerConfig WithEnabled(bool value)
    {
        return new TributeForcerConfig { Enabled = value, ToggleKey = ToggleKey };
    }

    private TributeForcerConfig WithToggleKey(KeyCode value)
    {
        return new TributeForcerConfig { Enabled = Enabled, ToggleKey = value };
    }

    private TributeForcerConfig() { }

    public static TributeForcerConfig CreateDefault() => new TributeForcerConfig();
}
