namespace GameSharp;

public readonly record struct EnqueuedWorkItem
{
    public required Action<object?> Callback { get; init; }

    public object? State { get; init; }
}
