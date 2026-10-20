using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace GameSharp.Collections;

/// <summary>
/// Represents a handle that holds an upgraded read (write) lock.
/// </summary>
public ref struct UpgradedReadLockHandle : IDisposable
{
    private readonly ReaderWriterLockSlim _lock;
    internal int opCount;

    /// <summary>
    /// Gets a value indicating whether the write lock is currently held by this handle.
    /// </summary>
    public readonly bool IsHeld => opCount > 0;

    internal UpgradedReadLockHandle(ReaderWriterLockSlim rwLock)
    {
        _lock = rwLock;
        opCount = 1;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (--opCount == 0)
        {
            _lock.ExitWriteLock();
        }
    }

    internal void Release()
    {
        if (IsHeld)
        {
            opCount = 0;
            _lock.ExitWriteLock();
        }
    }
}
