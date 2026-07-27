using System;

internal static class CashGainKey
{
    public const string Name = "ResourePick1";

    public static bool IsCashKey(string key)
    {
        return string.Equals(key, Name, StringComparison.Ordinal);
    }
}
