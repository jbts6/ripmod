public sealed class AbsorbOperationGate
{
    public bool IsPending { get; private set; }

    public bool TryBegin()
    {
        if (IsPending)
            return false;

        IsPending = true;
        return true;
    }

    public void Complete()
    {
        IsPending = false;
    }
}
