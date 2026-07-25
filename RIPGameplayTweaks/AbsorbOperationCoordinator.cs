using System;
using System.Threading;

public sealed class AbsorbOperation<T> where T : class
{
    private int _isCompleted;

    internal AbsorbOperation(long generation, T value)
    {
        Generation = generation;
        Value = value;
    }

    public long Generation { get; }
    public T Value { get; }
    public object Callback { get; private set; }

    public void SetCallback(object callback)
    {
        Callback = callback ?? throw new ArgumentNullException(nameof(callback));
    }

    internal bool TryMarkCompleted()
    {
        return Interlocked.CompareExchange(ref _isCompleted, 1, 0) == 0;
    }

    internal void ClearCallback()
    {
        Callback = null;
    }
}

public sealed class AbsorbOperationCoordinator<T> where T : class
{
    private readonly object _sync = new object();
    private readonly AbsorbOperationGate _gate = new AbsorbOperationGate();
    private long _nextGeneration;
    private AbsorbOperation<T> _current;

    public bool IsPending => _gate.IsPending;

    public bool TryBegin(T value, out AbsorbOperation<T> operation)
    {
        operation = null;
        if (value == null)
            return false;

        lock (_sync)
        {
            if (!_gate.TryBegin())
                return false;

            operation = new AbsorbOperation<T>(++_nextGeneration, value);
            _current = operation;
            return true;
        }
    }

    public bool TryComplete(AbsorbOperation<T> operation, Action<T> settle)
    {
        if (!TryMarkCurrentOperation(operation))
            return false;

        try
        {
            settle?.Invoke(operation.Value);
        }
        finally
        {
            ClearCurrentOperation(operation);
        }

        return true;
    }

    public void Cancel(AbsorbOperation<T> operation)
    {
        if (!TryMarkCurrentOperation(operation))
            return;

        ClearCurrentOperation(operation);
    }

    private bool TryMarkCurrentOperation(AbsorbOperation<T> operation)
    {
        if (operation == null)
            return false;

        lock (_sync)
        {
            return ReferenceEquals(_current, operation) && operation.TryMarkCompleted();
        }
    }

    private void ClearCurrentOperation(AbsorbOperation<T> operation)
    {
        lock (_sync)
        {
            if (!ReferenceEquals(_current, operation))
                return;

            _current = null;
            operation.ClearCallback();
            _gate.Complete();
        }
    }
}
