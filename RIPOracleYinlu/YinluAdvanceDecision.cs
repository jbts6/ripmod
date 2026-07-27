internal enum YinluAdvanceState
{
    KeepOriginal,
    Empty,
    Ready
}

internal sealed class YinluAdvanceDecision
{
    private YinluAdvanceDecision(YinluAdvanceState state, float currentExp, float requiredExp)
    {
        State = state;
        CurrentExp = currentExp;
        RequiredExp = requiredExp;
    }

    public YinluAdvanceState State { get; }

    public float CurrentExp { get; }

    public float RequiredExp { get; }

    public static YinluAdvanceDecision Evaluate(int selectedCount, float currentExp)
    {
        if (selectedCount == 0)
            return new YinluAdvanceDecision(YinluAdvanceState.Empty, 0f, 1f);

        if (selectedCount == 1)
            return new YinluAdvanceDecision(YinluAdvanceState.Ready, currentExp, currentExp);

        return new YinluAdvanceDecision(YinluAdvanceState.KeepOriginal, currentExp, currentExp);
    }
}
