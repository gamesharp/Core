using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace GameSharp.Collections;

/// <summary>
/// Represents a handle that holds a read lock.
/// </summary>
public ref struct ReadLockHandle : IDisposable
{
    private readonly ReaderWriterLockSlim _lock;
    private readonly CountdownEvent _activeOperations;
    /// <summary>
    /// Gets a value indicating whether the read lock is currently held by this handle.
    /// </summary>
    public bool IsHeld { get; private set; }

    internal ReadLockHandle(ReaderWriterLockSlim rwLock, CountdownEvent activeOperations)
    {
        _lock = rwLock;
        _activeOperations = activeOperations;
        IsHeld = true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (IsHeld)
        {
            IsHeld = false;
            CountingReaderWriterLock.ReleaseReadLock(_lock, _activeOperations);
        }
    }
}
