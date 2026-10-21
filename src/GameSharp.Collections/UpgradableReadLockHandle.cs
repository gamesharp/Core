using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace GameSharp.Collections;

/// <summary>
/// Represents a handle that holds an upgradable read lock.
/// </summary>
public ref struct UpgradableReadLockHandle : IDisposable
{
    internal readonly ReaderWriterLockSlim rwLock;
    private readonly CountdownEvent _activeOperations;
    private int _opCount;

    /// <summary>
    /// Gets a value indicating whether the upgradable read lock is currently held by this handle.
    /// </summary>
    public bool IsHeld { get; private set; }

    internal UpgradableReadLockHandle(ReaderWriterLockSlim rwLock, CountdownEvent activeOperations)
    {
        this.rwLock = rwLock;
        _activeOperations = activeOperations;
        IsHeld = true;
    }

    /// <summary>
    /// Upgrades the current lock to a write lock.
    /// </summary>
    /// <returns>An <see cref="UpgradedReadLockHandle"/> that can be disposed to release the write lock.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining), UnscopedRef]
    public UpgradedReadLockHandle Upgrade()
    {
        if (_opCount++ == 0)
        {
            rwLock.EnterWriteLock();
        }
        
        return new UpgradedReadLockHandle(rwLock, ref _opCount);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (IsHeld)
        {
            IsHeld = false;
            CountingReaderWriterLock.ReleaseUpgradeableReadLock(rwLock, _activeOperations);
        }
    }
}
