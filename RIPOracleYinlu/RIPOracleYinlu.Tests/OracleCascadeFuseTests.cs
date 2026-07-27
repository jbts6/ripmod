using System;

internal static class OracleCascadeFuseTests
{
    public static void RunAll()
    {
        StopsWhenNoPlans();
        FusesUntilExhausted();
        StopsOnTryFailure();
        HitsRoundCap();
        RejectsInvalidMaxRounds();
    }

    private static void StopsWhenNoPlans()
    {
        OracleCascadeFuseResult result = OracleCascadeFuseLogic.Run(
            () => false,
            () => true,
            10);

        TestAssert.Equal(0, result.RoundsAttempted, "no plans attempts");
        TestAssert.Equal(0, result.RoundsSucceeded, "no plans success");
        TestAssert.Equal("no-more-plans", result.StopReason, "no plans reason");
        TestAssert.False(result.HitRoundCap, "no plans not cap");
    }

    private static void FusesUntilExhausted()
    {
        int remaining = 3;
        OracleCascadeFuseResult result = OracleCascadeFuseLogic.Run(
            () => remaining > 0,
            () =>
            {
                remaining--;
                return true;
            },
            10);

        TestAssert.Equal(3, result.RoundsAttempted, "three rounds attempted");
        TestAssert.Equal(3, result.RoundsSucceeded, "three rounds succeeded");
        TestAssert.Equal(0, remaining, "plans exhausted");
        TestAssert.Equal("no-more-plans", result.StopReason, "exhaust reason");
    }

    private static void StopsOnTryFailure()
    {
        int calls = 0;
        OracleCascadeFuseResult result = OracleCascadeFuseLogic.Run(
            () => true,
            () =>
            {
                calls++;
                return false;
            },
            10);

        TestAssert.Equal(1, result.RoundsAttempted, "fail stops after one attempt");
        TestAssert.Equal(0, result.RoundsSucceeded, "fail has zero success");
        TestAssert.Equal(1, calls, "try called once");
        TestAssert.Equal("try-failed", result.StopReason, "fail reason");
    }

    private static void HitsRoundCap()
    {
        OracleCascadeFuseResult result = OracleCascadeFuseLogic.Run(
            () => true,
            () => true,
            5);

        TestAssert.Equal(5, result.RoundsAttempted, "cap attempts");
        TestAssert.Equal(5, result.RoundsSucceeded, "cap success");
        TestAssert.True(result.HitRoundCap, "cap flag");
        TestAssert.Equal("round-cap", result.StopReason, "cap reason");
    }

    private static void RejectsInvalidMaxRounds()
    {
        OracleCascadeFuseResult result = OracleCascadeFuseLogic.Run(
            () => true,
            () => true,
            0);

        TestAssert.Equal(0, result.RoundsAttempted, "invalid max attempts");
        TestAssert.Equal("invalid-max-rounds", result.StopReason, "invalid max reason");
    }
}
