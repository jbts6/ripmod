using System;

internal static class PerforationRateLogic
{
    public const float ForcedRate = 1f;

    private const string BaseKeyPrefix = "SkillExpand";
    private const string PityKeyPrefix = "CurSkillExpand";
    private const int MinSlot = 1;
    private const int MaxSlot = 5;

    public static bool IsPerforationRateKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        return MatchesSlotKey(key, BaseKeyPrefix) ||
               MatchesSlotKey(key, PityKeyPrefix);
    }

    public static bool TryGetForcedRate(string key, out float value)
    {
        if (IsPerforationRateKey(key))
        {
            value = ForcedRate;
            return true;
        }

        value = default;
        return false;
    }

    private static bool MatchesSlotKey(string key, string prefix)
    {
        if (key.Length != prefix.Length + 1)
            return false;

        if (!key.StartsWith(prefix, StringComparison.Ordinal))
            return false;

        char slotDigit = key[prefix.Length];
        return slotDigit >= (char)('0' + MinSlot) &&
               slotDigit <= (char)('0' + MaxSlot);
    }
}
