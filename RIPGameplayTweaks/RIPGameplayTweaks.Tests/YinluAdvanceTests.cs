internal static class YinluAdvanceTests
{
    public static void RunAll()
    {
        EmptySelectionUsesZeroOfOneDisplay();
        OneMaterialFillsCurrentRequirement();
        MultipleMaterialsKeepOriginalBehavior();
    }

    private static void EmptySelectionUsesZeroOfOneDisplay()
    {
        YinluAdvanceDecision decision = YinluAdvanceDecision.Evaluate(0, 0f);

        TestAssert.Equal((int)YinluAdvanceState.Empty, (int)decision.State, "empty selection state");
        TestAssert.Near(0, decision.CurrentExp, "empty selection current exp");
        TestAssert.Near(1, decision.RequiredExp, "empty selection required exp");
    }

    private static void OneMaterialFillsCurrentRequirement()
    {
        YinluAdvanceDecision decision = YinluAdvanceDecision.Evaluate(1, 25f);

        TestAssert.Equal((int)YinluAdvanceState.Ready, (int)decision.State, "one material state");
        TestAssert.Near(25, decision.CurrentExp, "one material current exp");
        TestAssert.Near(25, decision.RequiredExp, "one material required exp");
    }

    private static void MultipleMaterialsKeepOriginalBehavior()
    {
        YinluAdvanceDecision decision = YinluAdvanceDecision.Evaluate(2, 50f);

        TestAssert.Equal(
            (int)YinluAdvanceState.KeepOriginal,
            (int)decision.State,
            "multiple materials state");
    }
}
