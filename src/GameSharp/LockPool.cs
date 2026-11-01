using Microsoft.Extensions.ObjectPool;
using System.Threading;
#if NET9_0_OR_GREATER
using Lock = System.Threading.Lock;
#else
using Lock = System.Object;
#endif

namespace GameSharp;

/// <summary>
/// Represents a pool of reusable <see cref="Lock"/> objects.
/// </summary>
public sealed class LockPool() : DefaultObjectPool<Lock>(new LockPooledObjectPolicy())
{
    private sealed class LockPooledObjectPolicy : IPooledObjectPolicy<Lock>
    {
        public Lock Create() => new();
        public bool Return(Lock obj)
        {
#if NET9_0_OR_GREATER
            ThrowHelpers.ThrowIfArgIsNull(obj);
            if (obj.TryEnter())
            {
                obj.Exit();
                return true;
            }
#else
            if (obj is null)
            {
                throw new System.ArgumentNullException(nameof(obj));
            }
            if (!System.Threading.Monitor.IsEntered(obj) 
                && System.Threading.Monitor.TryEnter(obj))
            {
                System.Threading.Monitor.Exit(obj);
                return true;
            }
#endif
            return false;
        }
    }

    /// <summary>
    /// Gets the shared instance of the <see cref="LockPool"/> class.
    /// </summary>
    public static LockPool Shared { get; } = new();
}
