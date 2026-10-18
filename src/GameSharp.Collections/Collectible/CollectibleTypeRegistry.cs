using System.Diagnostics.CodeAnalysis;

namespace GameSharp.Collections.Collectible;

internal sealed class CollectibleTypeRegistry(short id) : TypeRegistry(id), IDisposable
{
    private readonly CountingReaderWriterLock _syncRoot = new();

    public override TypeInfo Get(int id)
    {
        using ReadLockHandle handle = _syncRoot.EnterReadLock();
        return base.Get(id);
    }

    public override bool GetOrAdd(Type type, [NotNull] out TypeInfo? typeInfo)
    {
        using UpgradableReadLockHandle readHandle = _syncRoot.EnterUpgradeableReadLock();
        if (Registrations.TryGetValue(type, out typeInfo))
        {
            return true;
        }

        using UpgradedReadLockHandle writeHandle = readHandle.Upgrade();
        return base.GetOrAdd(type, out typeInfo);
    }

    public override bool TryGet(Type type, [MaybeNullWhen(false)] out TypeInfo typeInfo)
    {
        using ReadLockHandle handle = _syncRoot.EnterReadLock();
        return base.TryGet(type, out typeInfo);
    }

    public override void Purge(short assemblyID)
    {
        using WriteLockHandle handle = _syncRoot.EnterWriteLock();
        base.Purge(assemblyID);
    }

    public void Close() => _syncRoot.Close();

    public void Dispose() => _syncRoot.Dispose();
}
