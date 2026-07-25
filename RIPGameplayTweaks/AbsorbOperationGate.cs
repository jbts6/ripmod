using System.Threading;

public sealed class AbsorbOperationGate
{
    private int _isPending;

    public bool IsPending => Volatile.Read(ref _isPending) != 0;

    public bool TryBegin()
    {
        return Interlocked.CompareExchange(ref _isPending, 1, 0) == 0;
    }

    public void Complete()
    {
        Interlocked.Exchange(ref _isPending, 0);
    }
}
