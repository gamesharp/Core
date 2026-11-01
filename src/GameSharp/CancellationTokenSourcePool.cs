using Microsoft.Extensions.ObjectPool;
using System.Threading;

namespace GameSharp;

/// <summary>
/// Represents a pool of <see cref="CancellationTokenSource"/> instances that can be reused.
/// </summary>
#if NET5_0_OR_GREATER
public sealed partial class CancellationTokenSourcePool : DefaultObjectPool<CancellationTokenSource>
{
    private sealed class ResetPolicy : PooledObjectPolicy<CancellationTokenSource>
    {
        public override CancellationTokenSource Create() => new();

        public override bool Return(CancellationTokenSource obj)
        {
            if (obj.TryReset())
            {
                return true;
            }

            obj.Dispose();
            return false;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CancellationTokenSourcePool"/> class.
    /// </summary>
    public CancellationTokenSourcePool() : base(new ResetPolicy()) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CancellationTokenSourcePool"/> class with a specified maximum number of retained objects.
    /// </summary>
    /// <inheritdoc cref="DefaultObjectPool{T}(IPooledObjectPolicy{T}, int)"/>
    public CancellationTokenSourcePool(int maximumRetained) : base(new ResetPolicy(), maximumRetained) { }
}
#else
public sealed partial class CancellationTokenSourcePool : ObjectPool<CancellationTokenSource>
{
    /// <inheritdoc/>
    public override CancellationTokenSource Get() => new();
    /// <inheritdoc/>
    public override void Return(CancellationTokenSource obj) => obj.Dispose();
}
#endif
public sealed partial class CancellationTokenSourcePool
{
    /// <summary>
    /// Gets the shared instance of the <see cref="CancellationTokenSourcePool"/> class.
    /// </summary>
    public static CancellationTokenSourcePool Shared { get; } = new();
}