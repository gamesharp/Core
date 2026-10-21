using System.Runtime.CompilerServices;

namespace GameSharp.Collections;

/// <summary>
/// Represents a handle that holds an upgraded read (write) lock.
/// </summary>
public ref struct UpgradedReadLockHandle : IDisposable
{
    private readonly ReaderWriterLockSlim _lock;
    private ref int _opCount;

    /// <summary>
    /// Gets a value indicating whether the write lock is currently held by this handle.
    /// </summary>
    public readonly bool IsHeld => _opCount > 0;

    internal UpgradedReadLockHandle(ReaderWriterLockSlim rwLock, ref int opCount)
    {
        _opCount = ref opCount;
        _lock = rwLock;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (--_opCount == 0)
        {
            _lock.ExitWriteLock();
        }
    }
}
