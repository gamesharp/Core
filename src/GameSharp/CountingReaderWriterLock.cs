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

    /// <summary>
    /// Tries to enter the lock in read mode.
    /// </summary>
    /// <param name="handle">
    /// When this method returns <see langword="true"/>, contains a <see cref="ReadLockHandle"/> that releases the read lock 
    /// when disposed; otherwise, contains the default value.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the read lock was acquired and handle contains a valid <see cref="ReadLockHandle"/>; 
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <inheritdoc cref="CountdownEvent.TryAddCount()"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool EnterReadLock(out ReadLockHandle handle)
    {
        if (FlagInterlocked.Read(ref _isClosing) || !_activeOperations.TryAddCount())
        {
            handle = default;
            return false;
        }

        handle = EnterReadLockImpl();
        return true;
    }

    /// <returns>A handle that releases the read lock when disposed.</returns>
    /// <inheritdoc cref="ReaderWriterLockSlim.EnterReadLock"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadLockHandle EnterReadLock()
    {
        if (!EnterReadLock(out ReadLockHandle handle))
        {
#if PRESERVE_STACK_TRACE
            throw ThrowHelpers.LockClosingOrClosedException();
#else
            ThrowHelpers.LockClosingOrClosed();
#endif
        }

        return handle;
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
    /// <param name="handle">
    /// When this method returns <see langword="true"/>, contains a <see cref="WriteLockHandle"/> that releases the write lock 
    /// when disposed; otherwise, contains the default value.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the write lock was acquired and handle contains a valid <see cref="WriteLockHandle"/>; 
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <inheritdoc cref="CountdownEvent.TryAddCount()"/>
    public bool EnterWriteLock(out WriteLockHandle handle)
    {
        if (FlagInterlocked.Read(ref _isClosing) || !_activeOperations.TryAddCount())
        {
            handle = default;
            return false;
        }

        handle = EnterWriteLockImpl();
        return true;
    }

    /// <summary>
    /// Tries to enter the lock in write mode.
    /// </summary>
    /// <returns>A handle that releases the write lock when disposed.</returns>
    /// <inheritdoc cref="CountdownEvent.AddCount()"/>
    public WriteLockHandle EnterWriteLock()
    {
        if (!EnterWriteLock(out WriteLockHandle handle))
        {
#if PRESERVE_STACK_TRACE
            throw ThrowHelpers.LockClosingOrClosedException();
#else
            ThrowHelpers.LockClosingOrClosed();
#endif
        }

        return handle;
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
    /// <param name="handle">
    /// When this method returns <see langword="true"/>, contains a <see cref="UpgradableReadLockHandle"/> that releases the upgradeable read lock 
    /// when disposed; otherwise, contains the default value.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the upgradeable read lock was acquired and handle contains a valid <see cref="UpgradableReadLockHandle"/>; 
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <inheritdoc cref="CountdownEvent.TryAddCount()"/>
    public bool EnterUpgradeableReadLock(out UpgradableReadLockHandle handle)
    {
        if (FlagInterlocked.Read(ref _isClosing) || !_activeOperations.TryAddCount())
        {
            handle = default;
            return false;
        }

        handle = EnterUpgradableReadLockImpl();
        return true;
    }

    /// <summary>
    /// Tries to enter the lock in upgradeable mode.
    /// </summary>
    /// <returns>A handle that releases the upgradeable read lock when disposed.</returns>
    /// <inheritdoc cref="CountdownEvent.TryAddCount()"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UpgradableReadLockHandle EnterUpgradeableReadLock()
    {
        if (!EnterUpgradeableReadLock(out UpgradableReadLockHandle handle))
        {
#if PRESERVE_STACK_TRACE
            throw ThrowHelpers.LockClosingOrClosedException();
#else
            ThrowHelpers.LockClosingOrClosed();
#endif
        }

        return handle;
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
}
