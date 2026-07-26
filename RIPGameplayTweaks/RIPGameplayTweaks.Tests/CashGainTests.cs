using System;

internal static class CashGainTests
{
    private static readonly IntPtr Owner = new IntPtr(0x1234);

    public static void RunAll()
    {
        ScalesPositiveDeltaOnly();
        RoundsSmallFractionAwayFromZero();
        CapsAtIntMaximum();
        RestoreScopeSuppressesAcquisition();
        MatchingContextScalesOnceAndCleansUp();
        NonMatchingContextKeepsProposedValue();
        CashKeyIsNarrowlyScoped();
    }

    private static void ScalesPositiveDeltaOnly()
    {
        TestAssert.Equal(120, CashGainMath.ScalePositiveDelta(100, 110, 2), "positive delta scales");
        TestAssert.Equal(90, CashGainMath.ScalePositiveDelta(100, 90, 2), "negative delta is unchanged");
        TestAssert.Equal(110, CashGainMath.ScalePositiveDelta(100, 110, 1), "neutral multiplier is unchanged");
    }

    private static void RoundsSmallFractionAwayFromZero()
    {
        TestAssert.Equal(102, CashGainMath.ScalePositiveDelta(100, 101, 1.5), "fraction rounds away from zero");
    }

    private static void CapsAtIntMaximum()
    {
        TestAssert.Equal(
            int.MaxValue,
            CashGainMath.ScalePositiveDelta(int.MaxValue - 1, int.MaxValue, 2),
            "scaled gain caps at int maximum");
    }

    private static void RestoreScopeSuppressesAcquisition()
    {
        CashGainContext.BeginRestoreScope();
        try
        {
            TestAssert.False(
                CashGainContext.TryBeginAcquisition(Owner, "ResourePick1", 100, 2),
                "restore scope suppresses acquisition");
        }
        finally
        {
            CashGainContext.EndRestoreScope();
        }
    }

    private static void MatchingContextScalesOnceAndCleansUp()
    {
        TestAssert.True(
            CashGainContext.TryBeginAcquisition(Owner, "ResourePick1", 100, 2),
            "matching acquisition starts");
        try
        {
            int scaled;
            TestAssert.True(
                CashGainContext.TryScale(Owner, "ResourePick1", 110, out scaled),
                "matching context scales");
            TestAssert.Equal(120, scaled, "matching context result");
            TestAssert.False(
                CashGainContext.TryScale(Owner, "ResourePick1", 115, out scaled),
                "matching context applies once");
            TestAssert.Equal(115, scaled, "second call remains original");
        }
        finally
        {
            CashGainContext.EndAcquisition();
        }

        int afterCleanup;
        TestAssert.False(
            CashGainContext.TryScale(Owner, "ResourePick1", 110, out afterCleanup),
            "context is cleared after finalizer");
        TestAssert.Equal(110, afterCleanup, "cleared context result");
    }

    private static void NonMatchingContextKeepsProposedValue()
    {
        CashGainContext.TryBeginAcquisition(Owner, "ResourePick1", 100, 2);
        try
        {
            int scaled;
            TestAssert.False(
                CashGainContext.TryScale(new IntPtr(0x5678), "ResourePick1", 110, out scaled),
                "different owner does not scale");
            TestAssert.Equal(110, scaled, "different owner result");
        }
        finally
        {
            CashGainContext.EndAcquisition();
        }
    }

    private static void CashKeyIsNarrowlyScoped()
    {
        TestAssert.True(CashGainKey.IsCashKey("ResourePick1"), "cash key matches");
        TestAssert.False(CashGainKey.IsCashKey("ResourePick2"), "other resource key does not match");
        TestAssert.False(CashGainKey.IsCashKey(null), "null key does not match");
    }
}
