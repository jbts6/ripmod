using System;

internal readonly struct OracleCascadeFuseResult
{
    public OracleCascadeFuseResult(int roundsAttempted, int roundsSucceeded, bool hitRoundCap, string stopReason)
    {
        RoundsAttempted = roundsAttempted;
        RoundsSucceeded = roundsSucceeded;
        HitRoundCap = hitRoundCap;
        StopReason = stopReason ?? string.Empty;
    }

    public int RoundsAttempted { get; }
    public int RoundsSucceeded { get; }
    public bool HitRoundCap { get; }
    public string StopReason { get; }
}

/// <summary>
/// Pure loop controller for cascade batch fuse. Game API is injected for offline tests.
/// </summary>
internal static class OracleCascadeFuseLogic
{
    public const int DefaultMaxRounds = 256;

    public static OracleCascadeFuseResult Run(
        Func<bool> hasAvailable,
        Func<bool> tryFuseOnce,
        int maxRounds = DefaultMaxRounds)
    {
        if (hasAvailable == null)
            throw new ArgumentNullException(nameof(hasAvailable));
        if (tryFuseOnce == null)
            throw new ArgumentNullException(nameof(tryFuseOnce));
        if (maxRounds <= 0)
            return new OracleCascadeFuseResult(0, 0, false, "invalid-max-rounds");

        int attempted = 0;
        int succeeded = 0;

        while (attempted < maxRounds)
        {
            if (!hasAvailable())
                return new OracleCascadeFuseResult(attempted, succeeded, false, "no-more-plans");

            attempted++;
            if (tryFuseOnce())
                succeeded++;
            else
                return new OracleCascadeFuseResult(attempted, succeeded, false, "try-failed");
        }

        return new OracleCascadeFuseResult(attempted, succeeded, true, "round-cap");
    }
}
