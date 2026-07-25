using System;
using System.Collections.Generic;

internal sealed class TributeWeightBaselineCache
{
    private readonly Dictionary<IntPtr, CacheEntry> _entries =
        new Dictionary<IntPtr, CacheEntry>();
    private IntPtr _contextIdentity;
    private bool _hasContext;

    public void BeginContext(IntPtr contextIdentity)
    {
        if (_hasContext && _contextIdentity == contextIdentity)
            return;

        _entries.Clear();
        _contextIdentity = contextIdentity;
        _hasContext = true;
    }

    public IReadOnlyDictionary<string, float> GetBaseline(
        IntPtr identity,
        IReadOnlyDictionary<string, float> liveWeights)
    {
        if (liveWeights == null)
            throw new ArgumentNullException(nameof(liveWeights));

        if (!_entries.TryGetValue(identity, out CacheEntry entry))
        {
            entry = new CacheEntry(Copy(liveWeights), Copy(liveWeights));
            _entries[identity] = entry;
        }
        else if (!Matches(liveWeights, entry.ExpectedLive))
        {
            entry = MergeExternalChanges(entry, liveWeights);
            _entries[identity] = entry;
        }

        return entry.Baseline;
    }

    public void RecordExpected(
        IntPtr identity,
        IReadOnlyDictionary<string, float> expectedLive)
    {
        if (expectedLive == null)
            throw new ArgumentNullException(nameof(expectedLive));

        if (!_entries.TryGetValue(identity, out CacheEntry entry))
            throw new InvalidOperationException("必须先为同一权重池建立基线。");

        entry.ExpectedLive = Copy(expectedLive);
    }

    private static bool Matches(
        IReadOnlyDictionary<string, float> liveWeights,
        IReadOnlyDictionary<string, float> expectedLive)
    {
        if (expectedLive == null || liveWeights.Count != expectedLive.Count)
            return false;

        foreach (KeyValuePair<string, float> pair in expectedLive)
        {
            if (!liveWeights.TryGetValue(pair.Key, out float value) || value != pair.Value)
                return false;
        }

        return true;
    }

    private static Dictionary<string, float> Copy(IReadOnlyDictionary<string, float> source)
    {
        var result = new Dictionary<string, float>(source.Count, StringComparer.Ordinal);
        foreach (KeyValuePair<string, float> pair in source)
            result[pair.Key] = pair.Value;
        return result;
    }

    private static CacheEntry MergeExternalChanges(
        CacheEntry current,
        IReadOnlyDictionary<string, float> liveWeights)
    {
        Dictionary<string, float> baseline = Copy(liveWeights);

        // 未变化的实时值可能仍包含本 mod 上次施加的倍率，需保留其原始基线。
        foreach (KeyValuePair<string, float> pair in liveWeights)
        {
            if (current.ExpectedLive.TryGetValue(pair.Key, out float expected) &&
                pair.Value == expected &&
                current.Baseline.TryGetValue(pair.Key, out float original))
            {
                baseline[pair.Key] = original;
            }
        }

        return new CacheEntry(baseline, Copy(liveWeights));
    }

    private sealed class CacheEntry
    {
        public CacheEntry(
            Dictionary<string, float> baseline,
            Dictionary<string, float> expectedLive)
        {
            Baseline = baseline;
            ExpectedLive = expectedLive;
        }

        public Dictionary<string, float> Baseline { get; }

        public Dictionary<string, float> ExpectedLive { get; set; }
    }

}
