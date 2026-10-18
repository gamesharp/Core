using System.Diagnostics.CodeAnalysis;

namespace GameSharp.Collections.Collectible;

internal sealed class NonCollectibleTypeRegistry(short id) : TypeRegistry(id)
{
    private readonly ReaderWriterLockSlim _syncRoot = new(LockRecursionPolicy.NoRecursion);

    public override TypeInfo Get(int id)
    {
        _syncRoot.EnterReadLock();

        try
        {
            return base.Get(id);
        }
        finally
        {
            _syncRoot.ExitReadLock();
        }
    }

    public override bool TryGet(Type type, [MaybeNullWhen(false)] out TypeInfo typeInfo)
    {
        _syncRoot.EnterReadLock();

        try
        {
            return base.TryGet(type, out typeInfo);
        }
        finally
        {
            _syncRoot.ExitReadLock();
        }
    }

    public override bool GetOrAdd(Type type, [NotNull] out TypeInfo? typeInfo)
    {
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
        }
    }

    public override void Purge(short assemblyID)
    {
        _syncRoot.EnterWriteLock();

        try
        {
            base.Purge(assemblyID);
        }
        finally
        {
            _syncRoot.ExitWriteLock();
        }
    }
}
