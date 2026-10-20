using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace GameSharp.Collections;

/// <summary>
/// A disposable reader–writer lock that counts active lock holders and blocks disposal until all holders have released
/// their locks.
/// </summary>
public sealed class CountingReaderWriterLock : IDisposable
{
    internal readonly ReaderWriterLockSlim syncRoot;
    internal readonly CountdownEvent activeOperations;
    private bool _isClosing;

    /// <summary>
    /// Initializes a new instance of <see cref="CountingReaderWriterLock"/> class.
    /// </summary>
    /// <param name="recursionPolicy">The <see cref="LockRecursionPolicy"/> to apply to the underlying synchronization primitive.</param>
    public CountingReaderWriterLock(LockRecursionPolicy recursionPolicy)
    {
        syncRoot = new(recursionPolicy);
        activeOperations = new(initialCount: 1);
    }

    /// <inheritdoc cref="CountingReaderWriterLock(LockRecursionPolicy)"/>
    public CountingReaderWriterLock()
    {
        syncRoot = new(LockRecursionPolicy.NoRecursion);
        activeOperations = new(initialCount: 1);
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

        activeOperations.Wait();
        return default;
    }

    /// <inheritdoc cref="TryEnterReadLock"/>
    public ReadLockHandle EnterReadLock()
    {
        EnsureCanEnterLock();
        return EnterReadLockImpl();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private ReadLockHandle EnterReadLockImpl()
    {
        try
        {
            syncRoot.EnterReadLock();
            return new ReadLockHandle(syncRoot, activeOperations);
        }
        catch
        {
            activeOperations.Signal();
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
        
        activeOperations.Wait();
        return default;
    }

    /// <inheritdoc cref="TryEnterWriteLock"/>
    public WriteLockHandle EnterWriteLock()
    {
        EnsureCanEnterLock();
        return EnterWriteLockImpl();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private WriteLockHandle EnterWriteLockImpl()
    {
        try
        {
            syncRoot.EnterWriteLock();
            return new WriteLockHandle(syncRoot, activeOperations);
        }
        catch
        {
            activeOperations.Signal();
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

        activeOperations.Wait();
        return default;
    }

    /// <inheritdoc cref="TryEnterUpgradeableReadLock"/>
    public UpgradableReadLockHandle EnterUpgradeableReadLock()
    {
        EnsureCanEnterLock();
        return EnterUpgradableReadLockImpl();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private UpgradableReadLockHandle EnterUpgradableReadLockImpl()
    {
        try
        {
            syncRoot.EnterUpgradeableReadLock();
            return new UpgradableReadLockHandle(syncRoot, activeOperations);
        }
        catch
        {
            activeOperations.Signal();
            throw;
        }
    }

    /// <summary>
    /// Prevents any further acquisitions of the lock and waits for all active lock holders to release their locks.
    /// </summary>
    public void Close()
    {
        if (Interlocked.Exchange(ref _isClosing, true))
        {
            activeOperations.Wait();
            return;
        }

        if (!activeOperations.Signal())
        {
            activeOperations.Wait();
        }

        syncRoot.Dispose();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Close();
        activeOperations.Dispose();
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
    private bool TryIncrementActiveOperations() => !Volatile.Read(ref _isClosing) && activeOperations.TryAddCount();

    [StackTraceHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureCanEnterLock()
    {
        if (!TryIncrementActiveOperations())
        {
            LockClosingOrClosed();
        }
    }

    [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
    private static void LockClosingOrClosed()
    {
        throw new InvalidOperationException("Cannot enter the lock because it is closing or has been closed.");
    }
}
