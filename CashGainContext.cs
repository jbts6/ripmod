using System;

internal static class CashGainContext
{
    [ThreadStatic]
    private static int _restoreScopeDepth;

    [ThreadStatic]
    private static Acquisition _acquisition;

    public static void BeginRestoreScope()
    {
        _restoreScopeDepth++;
        _acquisition = null;
    }

    public static void EndRestoreScope()
    {
        if (_restoreScopeDepth > 0)
            _restoreScopeDepth--;
    }

    public static bool TryBeginAcquisition(
        IntPtr owner,
        string key,
        int oldValue,
        double multiplier)
    {
        if (_restoreScopeDepth > 0)
        {
            _acquisition = null;
            return false;
        }

        _acquisition = new Acquisition(owner, key, oldValue, multiplier);
        return true;
    }

    public static bool TryScale(
        IntPtr owner,
        string key,
        int proposedValue,
        out int scaledValue)
    {
        Acquisition acquisition = _acquisition;
        if (acquisition == null ||
            acquisition.Applied ||
            acquisition.Owner != owner ||
            !string.Equals(acquisition.Key, key, StringComparison.Ordinal))
        {
            scaledValue = proposedValue;
            return false;
        }

        acquisition.Applied = true;
        scaledValue = CashGainMath.ScalePositiveDelta(
            acquisition.OldValue,
            proposedValue,
            acquisition.Multiplier);
        return true;
    }

    public static void EndAcquisition()
    {
        _acquisition = null;
    }

    private sealed class Acquisition
    {
        public Acquisition(IntPtr owner, string key, int oldValue, double multiplier)
        {
            Owner = owner;
            Key = key;
            OldValue = oldValue;
            Multiplier = multiplier;
        }

        public IntPtr Owner { get; }
        public string Key { get; }
        public int OldValue { get; }
        public double Multiplier { get; }
        public bool Applied { get; set; }
    }
}
