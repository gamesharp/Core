using GameSharp.Collections.Immutable;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace GameSharp.Collections.Concurrent;

/// <summary>
/// Represents a mutable, thread-safe type lookup.
/// </summary>
public sealed class ConcurrentTypeLookup : MutableTypeLookup, ITypeLookup, IDisposable
{
    /// <summary>
    /// Represents a collection of clusters, where each cluster contains contiguous items of the same type.
    /// </summary>
    public readonly struct ClusterCollection
    {
        /// <inheritdoc cref="IEnumerator{T}"/>
        public ref struct Enumerator : IStackOnlyEnumerator<TypeLookupCluster>, IDisposable
        {
            private ReadLockHandle _handle;
            private TypeLookupClusterEnumeratorCore _core;

            /// <inheritdoc cref="IEnumerator.Current"/>
            public readonly TypeLookupCluster Current => _core.Current;

            internal Enumerator(TypeLookupClusterEnumeratorCore core, ReadLockHandle handle)
            {
                _core = core;
                _handle = handle;
            }

            /// <inheritdoc cref="IEnumerator.MoveNext"/>
            public bool MoveNext() => _core.MoveNext();

            /// <inheritdoc/>
            public void Dispose() => _handle.Dispose();
        }

        private readonly ConcurrentTypeLookup _lookup;

        /// <inheritdoc cref="IReadOnlyCollection{T}.Count"/>
        public int Count => _lookup.ClusterCount;

        internal ClusterCollection(ConcurrentTypeLookup lookup) => _lookup = lookup;

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public Enumerator GetEnumerator()
        {
            ReadLockHandle handle = _lookup.GetState(out ReadOnlySpan<IntegerLookup> lookups, out ReadOnlySpan<object> items);
            try
            {
                TypeLookupClusterEnumeratorCore core = new(lookups, items);
                return new Enumerator(core, handle);
            }
            catch
            {
                handle.Dispose();
                throw;
            }
        }
    }

    /// <inheritdoc cref="IEnumerator{T}"/>
    public ref struct Enumerator : IDisposable
    {
        private TypeLookupEnumeratorCore<ClusterCollection.Enumerator> _core;
        private ReadLockHandle _handle;

        /// <inheritdoc cref="IEnumerator.Current"/>
        public readonly KeyValuePair<Type, object> Current => _core.Current;

        internal Enumerator(TypeLookupEnumeratorCore<ClusterCollection.Enumerator> core, ReadLockHandle handle)
        {
            _core = core;
            _handle = handle;
        }

        /// <inheritdoc cref="IEnumerator.MoveNext"/>
        public bool MoveNext() => _core.MoveNext();

        /// <inheritdoc/>
        public void Dispose() => _handle.Dispose();
    }

    /// <inheritdoc cref="Query{T}"/>
    public readonly struct Query : IEnumerable<object>
    {
        /// <inheritdoc cref="IEnumerator{T}"/>
        public ref struct Enumerator : IDisposable
        {
            private TypeLookupQueryEnumeratorCore _core;
            private ReadLockHandle _handle;

            /// <inheritdoc cref="IEnumerator.Current"/>
            public readonly object Current => _core.Current;

            internal Enumerator(TypeInfo typeInfo, ReadOnlySpan<IntegerLookup> lookups, ReadOnlySpan<object> items, ReadLockHandle handle)
            {
                _core = new TypeLookupQueryEnumeratorCore(typeInfo, lookups, items);
                _handle = handle;
            }

            /// <inheritdoc cref="IEnumerator.MoveNext"/>
            public bool MoveNext() => _core.MoveNext();

            /// <inheritdoc/>
            public void Dispose() => _handle.Dispose();
        }

        private readonly ConcurrentTypeLookup _lookup;
        private readonly TypeInfo _typeInfo;

        internal Query(ConcurrentTypeLookup lookup, TypeInfo typeInfo)
        {
            _lookup = lookup;
            _typeInfo = typeInfo;
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public Enumerator GetEnumerator()
        {
            ReadLockHandle handle = _lookup.GetState(out ReadOnlySpan<IntegerLookup> lookups, out ReadOnlySpan<object> items);
            try
            {
                return new Enumerator(_typeInfo, lookups, items, handle);
            }
            catch
            {
                handle.Dispose();
                throw;
            }
        }

        IEnumerator<object> IEnumerable<object>.GetEnumerator()
        {
            return ((IEnumerable<object>)[.. this]).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<object>)[.. this]).GetEnumerator();
        }
    }

    /// <summary>
    /// Represents a filtered query over a collection of objects.
    /// </summary>
    /// <typeparam name="T">The type of objects to filter by.</typeparam>
    public readonly struct Query<T> : IEnumerable<T> where T : class
    {
        /// <inheritdoc cref="IEnumerator{T}"/>
        public ref struct Enumerator : IDisposable
        {
            private TypeLookupQueryEnumeratorCore _core;
            private ReadLockHandle _handle;

            /// <inheritdoc cref="IEnumerator{T}.Current"/>
            public readonly T Current => Unsafe.As<T>(_core.Current);

            internal Enumerator(TypeInfo typeInfo, ReadOnlySpan<IntegerLookup> lookups, ReadOnlySpan<object> items, ReadLockHandle handle)
            {
                _core = new TypeLookupQueryEnumeratorCore(typeInfo, lookups, items);
                _handle = handle;
            }

            /// <inheritdoc cref="IEnumerator.MoveNext"/>
            public bool MoveNext() => _core.MoveNext();

            /// <inheritdoc/>
            public void Dispose() => _handle.Dispose();
        }

        private readonly ConcurrentTypeLookup _lookup;
        private readonly TypeInfo _typeInfo;

        internal Query(ConcurrentTypeLookup lookup, TypeInfo typeInfo)
        {
            _lookup = lookup;
            _typeInfo = typeInfo;
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public Enumerator GetEnumerator()
        {
            ReadLockHandle handle = _lookup.GetState(out ReadOnlySpan<IntegerLookup> lookups, out ReadOnlySpan<object> items);
            try
            {
                return new Enumerator(_typeInfo, lookups, items, handle);
            }
            catch
            {
                handle.Dispose();
                throw;
            }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return ((IEnumerable<T>)[.. this]).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)[.. this]).GetEnumerator();
        }
    }

    private ref struct LockedArrayBuilder : IArrayBuilder<IntegerLookup>, IArrayBuilder<object>, IDisposable
    {
        private readonly ArrayFactory _arrayFactory;
        private UpgradableReadLockHandle _readHandle;

        private ref IntegerLookup[] _lookups;
        readonly IntegerLookup[] IArrayBuilder<IntegerLookup>.Array => _lookups;

        private ref object[] _items;
        readonly object[] IArrayBuilder<object>.Array => _items;

        public required ref int lookupCount;
        int IArrayBuilder<IntegerLookup>.LogicalCount
        {
            readonly get => lookupCount;
            set => lookupCount = value;
        }

        public required ref int itemCount;
        int IArrayBuilder<object>.LogicalCount
        {
            readonly get => itemCount;
            set => itemCount = value;
        }

        public bool WasModified { readonly get; private set; }

        public LockedArrayBuilder(ArrayFactory arrayFactory, ref IntegerLookup[] lookups, ref object[] items, UpgradableReadLockHandle readHandle)
        {
            _arrayFactory = arrayFactory;
            _lookups = ref lookups;
            _items = ref items;
            _readHandle = readHandle;
        }

        public void Dispose() => _readHandle.Dispose();

        void IArrayBuilder<IntegerLookup>.Mutate()
        {
            _readHandle.Upgrade();
            _arrayFactory.SetCount(ref _lookups, lookupCount);
            WasModified = true;
        }

        void IArrayBuilder<object>.Mutate()
        {
            _readHandle.Upgrade();
            _arrayFactory.SetCount(ref _items, itemCount);
            WasModified = true;
        }
    }

    internal readonly CountingReaderWriterLock syncRoot;
    internal ImmutableTypeLookup lookup;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentTypeLookup"/> class.
    /// </summary>
    public ConcurrentTypeLookup()
    {
        syncRoot = new CountingReaderWriterLock();
        lookup = [];
    }

    internal ConcurrentTypeLookup(ImmutableTypeLookup lookup)
    {
        ArgumentNullException.ThrowIfNull(lookup);
        this.lookup = lookup;

        syncRoot = new CountingReaderWriterLock();
    }

    /// <inheritdoc/>
    public override int Count
    {
        get
        {
            if (syncRoot.EnterReadLock() is { IsHeld: true } handle)
            {
                return GetCountLocked(handle);
            }

            return Volatile.Read(ref lookup).Count;
        }
    }

    private int ClusterCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (syncRoot.EnterReadLock() is { IsHeld: true } handle)
            {
                return GetClusterCountLocked(handle);
            }

            return Volatile.Read(ref lookup).lookupCount;
        }
    }

    /// <summary>
    /// Gets a collection of clusters for the current instance.
    /// </summary>
    public ClusterCollection Clusters => new(this);

    /// <summary>
    /// Transitions this lookup to it's published runtime state, completing any in-flight synchronized mutations
    /// and switching subsequent updates to the snapshot-based concurrent path.
    /// </summary>
    public void Publish() => syncRoot.Close();

    /// <inheritdoc/>
    public void Dispose() => syncRoot.Dispose();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private protected override bool Contains(object item, TypeInfo typeInfo)
    {
        foreach (object obj in new Query(this, typeInfo))
        {
            if (ReferenceEquals(obj, item))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        if (syncRoot.EnterUpgradeableReadLock() is { IsHeld: true } handle)
        {
            ClearLocked(handle);
        }

        Interlocked.Exchange(ref lookup, []);
    }

    /// <inheritdoc cref="IReadOnlyTypeLookup.GetAll(Type)"/>
    public Query GetAll([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type)
    {
        return new Query(this, TypeInfo.Get(type));
    }

    /// <inheritdoc cref="IReadOnlyTypeLookup.GetAll{T}"/>
    public Query<T> GetAll<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>() where T : class
    {
        return new Query<T>(this, TypeInfo.Get<T>());
    }

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    public Enumerator GetEnumerator()
    {
        ReadLockHandle handle = syncRoot.TryEnterReadLock();
        try
        {
            ClusterCollection.Enumerator clustersEnumerator = Clusters.GetEnumerator();
            TypeLookupEnumeratorCore<ClusterCollection.Enumerator> core = new(clustersEnumerator);
            return new Enumerator(core, handle);
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    private protected override bool Add(object item, TypeInfo typeInfo)
    {
        if (syncRoot.TryEnterUpgradeableReadLock() is { IsHeld: true } handle)
        {
            return AddLocked(item, typeInfo, handle);
        }

        return ImmutableTypeLookup.InterlockedAdd(ref lookup, item, typeInfo);
    }

    private protected override bool Remove(object item, TypeInfo typeInfo)
    {
        if (syncRoot.TryEnterUpgradeableReadLock() is { IsHeld: true } handle)
        {
            return RemoveLocked(item, typeInfo, handle);
        }
        
        return ImmutableTypeLookup.InterlockedRemove(ref lookup, item, typeInfo);
    }

    private protected override bool TryGetOne<T>(TypeInfo typeInfo, [MaybeNullWhen(false)] out T item) where T : class
    {
        using ReadLockHandle handle = syncRoot.TryEnterReadLock();
        GetState(out ReadOnlySpan<IntegerLookup> lookups, out ReadOnlySpan<object> items);
        return TryGetOne(lookups, items, typeInfo, out item);
    }

    private protected override bool Replace(object existingItem, object newItem, TypeInfo typeInfo)
    {
        if (syncRoot.TryEnterUpgradeableReadLock() is { IsHeld: true } handle)
        {
            return ReplaceLocked(existingItem, newItem, typeInfo, handle);
        }

        return ImmutableTypeLookup.InterlockedReplace(ref lookup, existingItem, newItem, typeInfo);
    }

    private int GetCountLocked(ReadLockHandle handle)
    {
        using (handle)
        {
            return lookup.Count;
        }
    }

    private int GetClusterCountLocked(ReadLockHandle handle)
    {
        using (handle)
        {
            return lookup.lookupCount;
        }
    }

    private void ClearLocked(UpgradableReadLockHandle handle)
    {
        try
        {
            if (lookup.Count == 0)
            {
                return;
            }

            using UpgradedReadLockHandle writeHandle = handle.Upgrade();
            Array.Clear(lookup.items, 0, lookup.itemCount);
            lookup.itemCount = 0;
            lookup.lookupCount = 0;
        }
        finally
        {
            handle.Dispose();
        }
    }

    private bool AddLocked(object item, TypeInfo typeInfo, UpgradableReadLockHandle handle)
    {
        LockedArrayBuilder builder = new(CollectableArrayFactory.Default, ref lookup.lookups, ref lookup.items, handle)
        {
            lookupCount = ref lookup.lookupCount,
            itemCount = ref lookup.itemCount
        };

        try
        {
            TypeLookup.Add(ref builder, ref builder, item, typeInfo);
            return builder.WasModified;
        }
        finally
        {
            builder.Dispose();
        }
    }

    private bool RemoveLocked(object item, TypeInfo typeInfo, UpgradableReadLockHandle handle)
    {
        LockedArrayBuilder builder = new(CollectableArrayFactory.Default, ref lookup.lookups, ref lookup.items, handle)
        {
            lookupCount = ref lookup.lookupCount,
            itemCount = ref lookup.itemCount
        };

        try
        {
            TypeLookup.Remove(ref builder, ref builder, item, typeInfo);
            return builder.WasModified;
        }
        finally
        {
            builder.Dispose();
        }
    }

    private bool ReplaceLocked(object existingItem, object newItem, TypeInfo typeInfo, UpgradableReadLockHandle handle)
    {
        LockedArrayBuilder builder = new(CollectableArrayFactory.Default, ref Unsafe.NullRef<IntegerLookup[]>(), ref lookup.items, handle)
        {
            lookupCount = ref Unsafe.NullRef<int>(),
            itemCount = ref lookup.itemCount
        };

        try
        {
            Span<IntegerLookup> lookups = lookup.lookups.AsSpan(0, lookup.lookupCount);
            TypeLookup.Replace(lookups, ref builder, existingItem, newItem, typeInfo);
            return builder.WasModified;
        }
        finally
        {
            builder.Dispose();
        }
    }

    private ReadLockHandle GetState(out ReadOnlySpan<IntegerLookup> lookups, out ReadOnlySpan<object> items)
    {
        ReadLockHandle handle = syncRoot.EnterReadLock();
        try
        {
            ImmutableTypeLookup lookup = Volatile.Read(ref lookup);
            lookups = lookup.lookups.AsSpan(0, lookup.lookupCount);
            items = lookup.items.AsSpan(0, lookup.itemCount);
            return handle;
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    IEnumerable<object> IReadOnlyTypeLookup.GetAll([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type)
    {
        return [.. GetAll(type)];
    }

    IEnumerable<T> IReadOnlyTypeLookup.GetAll<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>()
    {
        return [.. GetAll<T>()];
    }

    void ICollection<KeyValuePair<Type, object>>.CopyTo(KeyValuePair<Type, object>[] array, int arrayIndex)
    {
        using ReadLockHandle handle = syncRoot.TryEnterReadLock();
        ((ICollection<KeyValuePair<Type, object>>)Volatile.Read(ref lookup)).CopyTo(array, arrayIndex);
    }

    IEnumerator<KeyValuePair<Type, object>> IEnumerable<KeyValuePair<Type, object>>.GetEnumerator()
    {
        if (syncRoot.TryEnterReadLock() is not { IsHeld: true } handle)
        {
            return ((IEnumerable<KeyValuePair<Type, object>>)Volatile.Read(ref lookup)).GetEnumerator();
        }

        using (handle)
        {
            return ((IEnumerable<KeyValuePair<Type, object>>)[.. lookup]).GetEnumerator();
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable<KeyValuePair<Type, object>>)this).GetEnumerator();
    }
}
