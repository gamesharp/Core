using System.Diagnostics.CodeAnalysis;

namespace GameSharp.Collections.Collectible;

internal sealed class CollectibleTypeRegistry(short id) : TypeRegistry(id), IDisposable
{
    private readonly ReaderWriterLockSlim _syncRoot = new(LockRecursionPolicy.NoRecursion);
    private readonly CountdownEvent _activeOps = new(initialCount: 0);
    private bool _disposed;

    public override TypeInfo Get(int id)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed), this);
        _activeOps.AddCount();
        _syncRoot.EnterReadLock();

        try
        {
            return base.Get(id);
        }
        finally
        {
            _syncRoot.ExitReadLock();
            _activeOps.Signal();
        }
    }

    public override bool GetOrAdd(Type type, [NotNull] out TypeInfo? typeInfo)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed), this);
        _activeOps.AddCount();
        _syncRoot.EnterUpgradeableReadLock();

        try
        {
            if (Registrations.TryGetValue(type, out typeInfo))
            {
                return true;
            }

            _syncRoot.EnterWriteLock();

            try
            {
                return base.GetOrAdd(type, out typeInfo);
            }
            finally
            {
                _syncRoot.ExitWriteLock();
            }
        }
        finally
        {
            _syncRoot.ExitUpgradeableReadLock();
            _activeOps.Signal();
        }
    }

    public override bool TryGet(Type type, [MaybeNullWhen(false)] out TypeInfo typeInfo)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed), this);
        _activeOps.AddCount();
        _syncRoot.EnterReadLock();

        try
        {
            return base.TryGet(type, out typeInfo);
        }
        finally
        {
            _syncRoot.ExitReadLock();
            _activeOps.Signal();
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, true))
        {
            return;
        }

        _activeOps.Wait();
        _activeOps.Dispose();
        _syncRoot.Dispose();
    }

    public override PurgeContext GetPurgeContext()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed), this);
        _activeOps.AddCount();
        _syncRoot.EnterReadLock();
        return base.GetPurgeContext();
    }

    protected override void CompletePurge()
    {
        _syncRoot.ExitReadLock();
        _activeOps.Signal();
    }
}
