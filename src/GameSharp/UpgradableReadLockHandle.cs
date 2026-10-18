using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace GameSharp;

/// <summary>
/// Represents a handle that holds an upgradable read lock.
/// </summary>
public ref struct UpgradableReadLockHandle : IDisposable
{
    private readonly ReaderWriterLockSlim _lock;
    private readonly CountdownEvent _activeOperations;
    private UpgradedReadLockHandle _writeHandle;

    /// <summary>
    /// Gets a value indicating whether the upgradable read lock is currently held by this handle.
    /// </summary>
    public bool IsHeld { get; private set; }

    internal UpgradableReadLockHandle(ReaderWriterLockSlim rwLock, CountdownEvent activeOperations)
    {
        _lock = rwLock;
        _activeOperations = activeOperations;
        _writeHandle = new UpgradedReadLockHandle(rwLock);
        IsHeld = true;
    }

    /// <summary>
    /// Upgrades the current lock to a write lock.
    /// </summary>
    /// <returns>An <see cref="UpgradedReadLockHandle"/> that can be disposed to release the write lock.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining), UnscopedRef]
    public ref UpgradedReadLockHandle Upgrade()
    {
        if (_writeHandle.opCount++ == 0)
        {
            _lock.EnterWriteLock();
        }
        
        return ref _writeHandle;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        _writeHandle.Release();

        if (IsHeld)
        {
            IsHeld = false;
            CountingReaderWriterLock.ReleaseUpgradeableReadLock(_lock, _activeOperations);
        }
    }
}
