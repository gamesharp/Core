using System.Diagnostics.CodeAnalysis;

namespace GameSharp;

public sealed class CancellationTokenSourcePool : ObjectPool<CancellationTokenSource>
{
    public static CancellationTokenSourcePool Shared { get; } = new();

    protected override CancellationTokenSource Create()
    {
        return new CancellationTokenSource();
    }

    protected override bool TryReset([NotNullWhen(true)] CancellationTokenSource? item)
    {
        return item is { } && item.TryReset();
    }
}
