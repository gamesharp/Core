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
    [Flags]
    private enum State : byte
    {
        None = 0,
        Closing = 1,
        Disposed = 2
    }

    internal readonly ReaderWriterLockSlim syncRoot;
    internal readonly CountdownEvent activeOperations;
    private State _state;
    private bool IsDisposed
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ((State)Volatile.Read(ref AsByte(ref _state)) & State.Disposed) != 0;
    }

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
    /// <exception cref="ObjectDisposedException">The lock has been disposed.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadLockHandle TryEnterReadLock()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
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
        ObjectDisposedException.ThrowIf(IsDisposed, this);
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
    /// <exception cref="ObjectDisposedException">The lock has been disposed.</exception>
    public WriteLockHandle TryEnterWriteLock()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
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
        ObjectDisposedException.ThrowIf(IsDisposed, this);
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
    /// <exception cref="ObjectDisposedException">The lock has been disposed.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UpgradableReadLockHandle TryEnterUpgradeableReadLock()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
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
        ObjectDisposedException.ThrowIf(IsDisposed, this);
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
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        CloseImpl();
    }

    private void CloseImpl()
    {
        if ((InterlockedOr(ref _state, State.Closing) & State.Closing) != 0)
        {
            activeOperations.Wait();
            return;
        }
        else if (!activeOperations.Signal())
        {
            activeOperations.Wait();
        }

        syncRoot.Dispose();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if ((InterlockedOr(ref _state, State.Disposed) & State.Disposed) == 0)
        {
            CloseImpl();
            activeOperations.Dispose();
        }
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
    private bool TryIncrementActiveOperations()
    {
        return ((State)Volatile.Read(ref AsByte(ref _state)) & State.Closing) == 0
            && activeOperations.TryAddCount();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref byte AsByte(ref State location1) => ref Unsafe.As<State, byte>(ref location1);

    private static State InterlockedOr(ref State location1, State value)
    {
        State currValue = (State)Volatile.Read(ref AsByte(ref location1)), prevValue;
        do
        {
            (currValue, prevValue) = ((State)Interlocked.CompareExchange(ref AsByte(ref location1), (byte)(currValue | value), (byte)currValue), currValue);
        }
        while (prevValue != currValue);
        return currValue;
    }

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
