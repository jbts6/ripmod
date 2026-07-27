using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

internal static class AbsorbTests
{
    public static void RunAll()
    {
        GateRejectsOverlappingOperations();
        GateAllowsOneConcurrentBegin();
        CoordinatorIgnoresDuplicateAndLateCallbacks();
        CoordinatorCancelReleasesOnlyMatchingOperation();
    }

    private static void GateRejectsOverlappingOperations()
    {
        var gate = new AbsorbOperationGate();
        TestAssert.True(gate.TryBegin(), "first absorb begins");
        TestAssert.False(gate.TryBegin(), "overlapping absorb rejected");
        gate.Complete();
        TestAssert.True(gate.TryBegin(), "new absorb allowed after completion");
        gate.Complete();
        gate.Complete();
        TestAssert.False(gate.IsPending, "completion is idempotent");
    }

    private static void GateAllowsOneConcurrentBegin()
    {
        var gate = new AbsorbOperationGate();
        int started = 0;
        Parallel.For(0, 128, _ =>
        {
            if (gate.TryBegin())
                Interlocked.Increment(ref started);
        });

        TestAssert.Equal(1, started, "only one concurrent absorb begins");
        gate.Complete();
        TestAssert.False(gate.IsPending, "concurrent gate completion releases operation");
    }

    private static void CoordinatorIgnoresDuplicateAndLateCallbacks()
    {
        var coordinator = new AbsorbOperationCoordinator<string>();
        var settlements = new List<string>();

        TestAssert.True(coordinator.TryBegin("first", out AbsorbOperation<string> first), "first operation begins");
        TestAssert.True(coordinator.TryComplete(first, settlements.Add), "first callback settles");
        TestAssert.False(coordinator.TryComplete(first, settlements.Add), "duplicate callback ignored");

        TestAssert.True(coordinator.TryBegin("second", out AbsorbOperation<string> second), "second operation begins");
        TestAssert.False(coordinator.TryComplete(first, settlements.Add), "late old callback ignored");
        TestAssert.True(coordinator.IsPending, "late old callback keeps second operation pending");
        TestAssert.True(coordinator.TryComplete(second, settlements.Add), "second callback settles");

        TestAssert.Equal("first,second", string.Join(",", settlements), "callbacks settle once");
        TestAssert.False(coordinator.IsPending, "second completion releases gate");
    }

    private static void CoordinatorCancelReleasesOnlyMatchingOperation()
    {
        var coordinator = new AbsorbOperationCoordinator<string>();
        TestAssert.True(coordinator.TryBegin("failed", out AbsorbOperation<string> failed), "failed operation begins");
        coordinator.Cancel(failed);
        TestAssert.False(coordinator.IsPending, "sync exception cleanup releases failed operation");

        TestAssert.True(coordinator.TryBegin("active", out AbsorbOperation<string> active), "new operation begins after cleanup");
        coordinator.Cancel(failed);
        TestAssert.True(coordinator.IsPending, "old cleanup cannot release new operation");
        coordinator.Cancel(active);
        TestAssert.False(coordinator.IsPending, "matching cleanup releases active operation");
    }
}
