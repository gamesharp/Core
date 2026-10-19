using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace GameSharp;

/// <summary>
/// A disposable reader–writer lock that counts active lock holders and blocks disposal until all holders have released
/// their locks.
/// </summary>
public sealed class CountingReaderWriterLock : IDisposable
{
    private readonly ReaderWriterLockSlim _syncRoot;
    private readonly CountdownEvent _activeOperations;
    private Flag _isClosing;

    /// <summary>
    /// Initializes a new instance of <see cref="CountingReaderWriterLock"/> class.
    /// </summary>
    /// <param name="recursionPolicy">The <see cref="LockRecursionPolicy"/> to apply to the underlying synchronization primitive.</param>
    public CountingReaderWriterLock(LockRecursionPolicy recursionPolicy)
    {
        _syncRoot = new(recursionPolicy);
        _activeOperations = new(initialCount: 1);
    }

    /// <inheritdoc cref="CountingReaderWriterLock(LockRecursionPolicy)"/>
    public CountingReaderWriterLock()
    {
        _syncRoot = new(LockRecursionPolicy.NoRecursion);
        _activeOperations = new(initialCount: 1);
    }

    /// <returns>A handle that releases the read lock when disposed.</returns>
    /// <inheritdoc cref="ReaderWriterLockSlim.EnterReadLock"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadLockHandle TryEnterReadLock()
    {
        if (TryIncrementActiveOperations())
        {
            return EnterReadLockImpl();
        }

        _activeOperations.Wait();
        return default;
    }

    /// <inheritdoc cref="TryEnterReadLock"/>
    public ReadLockHandle EnterReadLock()
    {
        if (!TryIncrementActiveOperations())
        {
#if PRESERVE_STACK_TRACE
            throw ThrowHelpers.LockClosingOrClosedException();
#else
            ThrowHelpers.LockClosingOrClosed();
#endif
        }

        return EnterReadLockImpl();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private ReadLockHandle EnterReadLockImpl()
    {
        try
        {
            _syncRoot.EnterReadLock();
            return new ReadLockHandle(_syncRoot, _activeOperations);
        }
        catch
        {
            _activeOperations.Signal();
            throw;
        }
    }

    /// <summary>
    /// Tries to enter the lock in write mode.
    /// </summary>
    /// <returns>A handle that releases the write lock when disposed.</returns>
    /// <inheritdoc cref="CountdownEvent.AddCount()"/>
    public WriteLockHandle TryEnterWriteLock()
    {
        if (TryIncrementActiveOperations())
        {
            return EnterWriteLockImpl();
        }
        
        _activeOperations.Wait();
        return default;
    }

    /// <inheritdoc cref="TryEnterWriteLock"/>
    public WriteLockHandle EnterWriteLock()
    {
        if (!TryIncrementActiveOperations())
        {
#if PRESERVE_STACK_TRACE
            throw ThrowHelpers.LockClosingOrClosedException();
#else
            ThrowHelpers.LockClosingOrClosed();
#endif
        }

        return EnterWriteLockImpl();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private WriteLockHandle EnterWriteLockImpl()
    {
        try
        {
            _syncRoot.EnterWriteLock();
            return new WriteLockHandle(_syncRoot, _activeOperations);
        }
        catch
        {
            _activeOperations.Signal();
            throw;
        }
    }

    /// <summary>
    /// Tries to enter the lock in upgradeable mode.
    /// </summary>
    /// <returns>A handle that releases the upgradeable read lock when disposed.</returns>
    /// <inheritdoc cref="CountdownEvent.TryAddCount()"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UpgradableReadLockHandle TryEnterUpgradeableReadLock()
    {
        if (TryIncrementActiveOperations())
        {
            return EnterUpgradableReadLockImpl();
        }

        _activeOperations.Wait();
        return default;
    }

    /// <inheritdoc cref="TryEnterUpgradeableReadLock"/>
    public UpgradableReadLockHandle EnterUpgradeableReadLock()
    {
        if (!TryIncrementActiveOperations())
        {
#if PRESERVE_STACK_TRACE
            throw ThrowHelpers.LockClosingOrClosedException();
#else
            ThrowHelpers.LockClosingOrClosed();
#endif
        }

        return EnterUpgradableReadLockImpl();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private UpgradableReadLockHandle EnterUpgradableReadLockImpl()
    {
        try
        {
            _syncRoot.EnterUpgradeableReadLock();
            return new UpgradableReadLockHandle(_syncRoot, _activeOperations);
        }
        catch
        {
            _activeOperations.Signal();
            throw;
        }
    }

    /// <summary>
    /// Prevents any further acquisitions of the lock and waits for all active lock holders to release their locks.
    /// </summary>
    public void Close()
    {
        if (FlagInterlocked.Exchange(ref _isClosing, true))
        {
            _activeOperations.Wait();
            return;
        }

        if (!_activeOperations.Signal())
        {
            _activeOperations.Wait();
        }

        _syncRoot.Dispose();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Close();
        _activeOperations.Dispose();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ReleaseReadLock(ReaderWriterLockSlim rwLock, CountdownEvent activeOps)
    {
        try
        {
            rwLock.ExitReadLock();
        }
        finally
        {
            activeOps.Signal();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ReleaseWriteLock(ReaderWriterLockSlim rwLock, CountdownEvent activeOps)
    {
        try
        {
            rwLock.ExitWriteLock();
        }
        finally
        {
            activeOps.Signal();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ReleaseUpgradeableReadLock(ReaderWriterLockSlim rwLock, CountdownEvent activeOps)
    {
        try
        {
            rwLock.ExitUpgradeableReadLock();
        }
        finally
        {
            activeOps.Signal();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryIncrementActiveOperations() => !FlagInterlocked.Read(ref _isClosing) && _activeOperations.TryAddCount();
}
