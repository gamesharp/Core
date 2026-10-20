using GameSharp.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GameSharp.Collections;

/// <summary>
/// Represents a mutable type lookup.
/// </summary>
public class TypeLookup : MutableTypeLookup, ITypeLookup
{
    private protected readonly ref struct ArrayBuilder<T>(ref T[] array, ref int logicalCount, ref int version, ArrayFactory arrayFactory) : IArrayBuilder<T>
    {
        private readonly ref int _version = ref version;

        private readonly ref T[] _array = ref array;
        public T[] Array => _array;

        private readonly ref int _logicalCount = ref logicalCount;
        public int LogicalCount
        {
            get => _logicalCount;
            set => _logicalCount = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Mutate()
        {
            arrayFactory.EnsureCapacity(ref _array, _logicalCount);
            _version++;
        }
    }

    /// <summary>
    /// Represents a collection of clusters, where each cluster contains contiguous items of the same type.
    /// </summary>
    public readonly struct ClusterCollection
    {
        /// <inheritdoc cref="IEnumerator{T}"/>
        public ref struct Enumerator : IStackOnlyEnumerator<TypeLookupCluster>
        {
            private TypeLookupClusterEnumeratorCore _core;
            private readonly MutationMonitor _monitor;

            /// <inheritdoc cref="IEnumerator.Current"/>
            public readonly TypeLookupCluster Current => _core.Current;

            internal Enumerator(TypeLookup lookup)
            {
                _monitor = new MutationMonitor(lookup);
                ReadOnlySpan<IntegerLookup> lookups = lookup.lookups.AsSpan(0, lookup.lookupCount);
                ReadOnlySpan<object> items = lookup.items.AsSpan(0, lookup.itemCount);
                _core = new TypeLookupClusterEnumeratorCore(lookups, items);
            }

            /// <inheritdoc cref="IEnumerator.MoveNext"/>
            public bool MoveNext()
            {
                _monitor.ThrowIfCollectionWasModified();
                return _core.MoveNext();
            }
        }

        private readonly TypeLookup _lookup;

        /// <inheritdoc cref="IReadOnlyCollection{T}.Count"/>
        public int Count => _lookup.lookupCount;

        internal ClusterCollection(TypeLookup lookup) => _lookup = lookup;

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public Enumerator GetEnumerator() => new Enumerator(_lookup);
    }

    private sealed class EnumeratorImpl(MutationMonitor monitor, ReadOnlyMemory<IntegerLookup> lookups, ReadOnlyMemory<object> items) : 
        TypeLookupEnumerator(lookups, items)
    {
        public override bool MoveNext()
        {
            monitor.ThrowIfCollectionWasModified();
            return base.MoveNext();
        }
    }

    /// <inheritdoc cref="IEnumerator{T}"/>
    public ref struct Enumerator
    {
        private TypeLookupEnumeratorCore<ClusterCollection.Enumerator> _core;

        /// <inheritdoc cref="IEnumerator.Current"/>
        public readonly KeyValuePair<Type, object> Current => _core.Current;

        internal Enumerator(TypeLookup lookup)
        {
            ClusterCollection.Enumerator clusterEnum = new(lookup);
            _core = new TypeLookupEnumeratorCore<ClusterCollection.Enumerator>(clusterEnum);
        }

        /// <inheritdoc cref="IEnumerator.MoveNext"/>
        public bool MoveNext() => _core.MoveNext();
    }

    /// <inheritdoc cref="Query{T}"/>
    public readonly struct Query : IEnumerable<object>
    {
        /// <inheritdoc cref="IEnumerator{T}"/>
        public ref struct Enumerator
        {
            private TypeLookupQueryEnumeratorCore _core;
            private readonly MutationMonitor _monitor;

            /// <inheritdoc cref="IEnumerator.Current"/>
            public readonly object Current => _core.Current;

            internal Enumerator(TypeInfo typeInfo, TypeLookup lookup)
            {
                _monitor = new MutationMonitor(lookup);
                ReadOnlySpan<IntegerLookup> lookups = lookup.lookups.AsSpan(0, lookup.lookupCount);
                ReadOnlySpan<object> items = lookup.items.AsSpan(0, lookup.itemCount);
                _core = new TypeLookupQueryEnumeratorCore(typeInfo, lookups, items);
            }

            /// <inheritdoc cref="IEnumerator.MoveNext"/>
            public bool MoveNext()
            {
                _monitor.ThrowIfCollectionWasModified();
                return _core.MoveNext();
            }
        }

        private readonly TypeLookup _lookup;
        private readonly TypeInfo _typeInfo;

        internal Query(TypeLookup lookup, TypeInfo typeInfo)
        {
            _lookup = lookup;
            _typeInfo = typeInfo;
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public Enumerator GetEnumerator() => new(_typeInfo, _lookup);

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
        public ref struct Enumerator
        {
            private TypeLookupQueryEnumeratorCore _core;
            private readonly MutationMonitor _monitor;

            /// <inheritdoc cref="IEnumerator{T}.Current"/>
            public readonly T Current => Unsafe.As<T>(_core.Current);

            internal Enumerator(TypeInfo typeInfo, TypeLookup lookup)
            {
                _monitor = new MutationMonitor(lookup);
                ReadOnlySpan<IntegerLookup> lookups = lookup.lookups.AsSpan(0, lookup.lookupCount);
                ReadOnlySpan<object> items = lookup.items.AsSpan(0, lookup.itemCount);
                _core = new TypeLookupQueryEnumeratorCore(typeInfo, lookups, items);
            }

            /// <inheritdoc cref="IEnumerator.MoveNext"/>
            public bool MoveNext()
            {
                _monitor.ThrowIfCollectionWasModified();
                return _core.MoveNext();
            }
        }

        private readonly TypeLookup _lookup;
        private readonly TypeInfo _typeInfo;

        internal Query(TypeLookup lookup, TypeInfo typeInfo)
        {
            _lookup = lookup;
            _typeInfo = typeInfo;
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public Enumerator GetEnumerator() => new(_typeInfo, _lookup);

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return ((IEnumerable<T>)[.. this]).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)[.. this]).GetEnumerator();
        }
    }

    private readonly struct MutationMonitor(TypeLookup collection)
    {
        private readonly int _version = collection._version;

        public bool WasModified
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => collection._version != _version;
        }

        [StackTraceHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ThrowIfCollectionWasModified()
        {
            if (WasModified)
            {
                CollectionWasModified();
            }
        }
    }

    private int _version = int.MinValue;

    private protected IntegerLookup[] lookups;
    private protected int lookupCount;

    private protected object[] items;
    private protected int itemCount;

    /// <inheritdoc/>
    public override int Count => itemCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeLookup"/> class.
    /// </summary>
    public TypeLookup()
    {
        lookups = [];
        items = [];
    }

    internal TypeLookup(IntegerLookup[] lookups, int lookupCount, object[] items, int itemCount)
    {
        this.lookups = lookups;
        this.lookupCount = lookupCount;

        this.items = items;
        this.itemCount = itemCount;
    }

    internal TypeLookup(IntegerLookup[] lookups, object[] items)
    {
        this.lookups = lookups;
        lookupCount = lookups.Length;

        this.items = items;
        itemCount = items.Length;
    }

    private protected override bool Replace(object existingItem, object newItem, TypeInfo typeInfo)
    {
        ArrayBuilder<object> itemBuilder = new(ref items, ref itemCount, ref _version, CollectableArrayFactory.Default);
        MutationMonitor monitor = new(this);
        Replace(lookups.AsSpan(0, lookupCount), ref itemBuilder, existingItem, newItem, typeInfo);
        return monitor.WasModified;
    }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private protected override bool TryGetOne<T>(TypeInfo typeInfo, [MaybeNullWhen(false)] out T item) where T : class
    {
        return TryGetOne(lookups.AsSpan(0, lookupCount), items.AsSpan(0, itemCount), typeInfo, out item);
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

    /// <inheritdoc/>
    public void Clear()
    {
        lookupCount = 0;

        Array.Clear(items, 0, itemCount);
        itemCount = 0;

        _version++;
    }

    /// <summary>
    /// Gets a <see cref="TypeLookupQueryFactory"/> for the specified type.
    /// </summary>
    /// <param name="type">The type for which to create the factory.</param>
    /// <returns>A <see cref="TypeLookupQueryFactory"/> that performs type-based lookups for the specified type.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="type"/> is <see langword="null"/>.</exception>
    public static TypeLookupQueryFactory GetQueryFactory([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        TypeInfo typeInfo = TypeInfo.Get(type);
        return new TypeLookupQueryFactory(typeInfo);
    }

    /// <summary>
    /// Gets a <see cref="TypeLookupQueryFactory{T}"/> for the specified type.
    /// </summary>
    /// <typeparam name="T">The type for which to create the factory.</typeparam>
    /// <returns>A <see cref="TypeLookupQueryFactory{T}"/> that performs type-based lookups for the specified type.</returns>
    public static TypeLookupQueryFactory<T> GetQueryFactory<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>() where T : class
    {
        TypeInfo typeInfo = TypeInfo.Get<T>();
        return new TypeLookupQueryFactory<T>(typeInfo);
    }

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private protected override bool Add(object item, TypeInfo typeInfo)
    {
        GetBuilders(out ArrayBuilder<IntegerLookup> lookupBuilder, out ArrayBuilder<object> itemBuilder);
        MutationMonitor monitor = new(this);
        Add(ref lookupBuilder, ref itemBuilder, item, typeInfo);
        return monitor.WasModified;
    }

    internal static void Add<TLookupBuilder, TItemBuilder>(
        scoped ref TLookupBuilder lookupBuilder,
        scoped ref TItemBuilder itemBuilder,
        object item,
TypeInfo typeInfo)
        where TLookupBuilder : struct, IArrayBuilder<IntegerLookup>, allows ref struct
        where TItemBuilder : struct, IArrayBuilder<object>, allows ref struct
    {
        int lookupIndex, itemIndex;

        Span<IntegerLookup> lookups = lookupBuilder.Array.AsSpan(..lookupBuilder.LogicalCount);
        Span<object> items = itemBuilder.Array.AsSpan(0, itemBuilder.LogicalCount);
        if (TryGetCluster(lookups, items, typeInfo.ID, out TypeLookupClusterIndex cluster))
        {
            foreach (object clusterItem in cluster.Items)
            {
                if (ReferenceEquals(clusterItem, item))
                {
                    return;
                }
            }

            itemIndex = itemBuilder.LogicalCount++;
            itemBuilder.Mutate();
            InsertAt(itemBuilder.Array, cluster.Lookup.index, itemIndex, item);

            lookupIndex = lookupBuilder.LogicalCount - 1;
            if (cluster.Index == lookupIndex)
            {
                return;
            }
            lookupBuilder.Mutate();
        }
        else
        {
            lookupIndex = lookupBuilder.LogicalCount++;
            lookupBuilder.Mutate();
            InsertAt(lookupBuilder.Array, cluster.Index, lookupIndex, cluster.Lookup);

            itemIndex = itemBuilder.LogicalCount++;
            itemBuilder.Mutate();
            InsertAt(itemBuilder.Array, cluster.Lookup.index, itemIndex, item);
        }

        if (cluster.Index < lookupIndex)
        {
            Span<IntegerLookup> subsequentLookups = lookupBuilder.Array.AsSpan(cluster.Index + 1, lookupIndex - cluster.Index);
            IntegerLookup.Offset(subsequentLookups, 1);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private protected override bool Remove(object item, TypeInfo typeInfo)
    {
        GetBuilders(out ArrayBuilder<IntegerLookup> lookupBuilder, out ArrayBuilder<object> itemBuilder);
        MutationMonitor monitor = new(this);
        Remove(ref lookupBuilder, ref itemBuilder, item, typeInfo);
        return monitor.WasModified;
    }

    internal static void Remove<TLookupBuilder, TItemBuilder>(
        scoped ref TLookupBuilder lookupBuilder, 
        scoped ref TItemBuilder itemBuilder, 
        object item,
        TypeInfo typeInfo)
        where TLookupBuilder : struct, IArrayBuilder<IntegerLookup>, allows ref struct
        where TItemBuilder : struct, IArrayBuilder<object>, allows ref struct
    {
        if (lookupBuilder.LogicalCount == 0)
        {
            return;
        }

        DerivedTypeCollection.CopyContext ctx = typeInfo.Derived.GetCopyContext();
        Span<int> derived = stackalloc int[ctx.Count];
        ctx.CopyTo(derived);

        ref int firstID = ref MemoryMarshal.GetReference(derived);
        ref int currentID = ref Unsafe.Add(ref firstID, derived.Length - 1);

        TypeLookupClusterIndex cluster;
        int li = lookupBuilder.LogicalCount;
        for (; Unsafe.IsAddressGreaterThanOrEqualTo(in currentID, in firstID); currentID = ref Unsafe.Subtract(ref currentID, 1), li = cluster.Index)
        {
            if (!TryGetCluster(lookupBuilder.Array.AsSpan(0, li), itemBuilder.Array.AsSpan(0, itemBuilder.LogicalCount), currentID, out cluster))
            {
                continue;
            }

            ReadOnlySpan<object> clusterItems = cluster.Items;
            ref object dataRef = ref MemoryMarshal.GetReference(clusterItems);
            int removedCount = 0;

            for (int ci = clusterItems.Length - 1; ci >= 0; ci--)
            {
                if (!ReferenceEquals(Unsafe.Add(ref dataRef, ci), item))
                {
                    continue;
                }

                itemBuilder.Mutate();
                itemBuilder.LogicalCount = RemoveAt(itemBuilder.Array, ci, itemBuilder.LogicalCount);
                removedCount++;
            }

            if (removedCount == 0)
            {
                continue;
            }

            lookupBuilder.Mutate();

            int absIndex = cluster.Index;

            if (absIndex < lookupBuilder.LogicalCount)
            {
                int start = absIndex + 1;
                IntegerLookup.Offset(lookupBuilder.Array.AsSpan(start, lookupBuilder.LogicalCount - start), -removedCount);
            }

            if (removedCount == clusterItems.Length)
            {
                lookupBuilder.LogicalCount = RemoveAt(lookupBuilder.Array, absIndex, lookupBuilder.LogicalCount);
            }
        }
    }

    internal static void Replace<TItemBuilder>(
        ReadOnlySpan<IntegerLookup> lookups,
        scoped ref TItemBuilder itemBuilder,
        object existingItem,
        object newItem,
        TypeInfo typeInfo)
        where TItemBuilder : struct, IArrayBuilder<object>, allows ref struct
    {
        ReadOnlySpan<object> items = itemBuilder.Array.AsSpan(0, itemBuilder.LogicalCount);
        TypeLookupQueryClusterEnumeratorCore enumerator = new(typeInfo, lookups, items);
        while (enumerator.MoveNext())
        {
            TypeLookupClusterIndex cluster = enumerator.Current;
            int i = cluster.Lookup.index;

            foreach (object clusterItem in cluster.Items)
            {
                if (ReferenceEquals(clusterItem, existingItem))
                {
                    itemBuilder.Mutate();
                    SetAtUnchecked(itemBuilder.Array, i, newItem);

                    return;
                }

                i++;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private protected void GetBuilders(out ArrayBuilder<IntegerLookup> lookupBuilder, out ArrayBuilder<object> itemBuilder)
    {
        lookupBuilder = new(ref lookups, ref lookupCount, ref _version, CollectableArrayFactory.Default);
        itemBuilder = new(ref items, ref itemCount, ref _version, CollectableArrayFactory.Default);
    }

    internal static bool TryGetCluster(ReadOnlySpan<IntegerLookup> lookups, ReadOnlySpan<object> items, int id, out TypeLookupClusterIndex cluster)
    {
        IntegerLookup lookup;

        if (lookups.IsEmpty)
        {
            lookup = new IntegerLookup(id, 0);
            cluster = new TypeLookupClusterIndex()
            {
                Index = 0,
                Lookup = lookup,
                Items = default
            };
            return false;
        }

        ref IntegerLookup first = ref MemoryMarshal.GetReference(lookups), last = ref Unsafe.Add(ref first, lookups.Length - 1);

        ref IntegerLookup low = ref first, high = ref last;
        ref IntegerLookup result = ref UnsafeSpanExtensions.HybridSearch(ref low, ref high, id, out bool exists);

        int index = IntegerLookup.ElementOffset((int)Unsafe.ByteOffset(in first, in result));

        if (exists)
        {
            cluster = new TypeLookupClusterIndex()
            {
                Index = index,
                Lookup = result,
                Items = items[Unsafe.AreSame(ref result, ref last) ? result.index.. : result.index..Unsafe.Add(ref result, 1).index]
            };

            return true;
        }

        if (Unsafe.IsAddressGreaterThan(ref result, ref last))
        {
            cluster = new TypeLookupClusterIndex()
            {
                Index = lookups.Length,
                Lookup = new IntegerLookup(id, items.Length),
                Items = default
            };

            return false;
        }

        cluster = new TypeLookupClusterIndex()
        {
            Index = index,
            Lookup = new IntegerLookup(id, result.index),
            Items = default
        };

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int InsertAt<T>(T[] array, int index, int count, T item)
    {
        if (index < count)
        {
            Span<T> destination = array.AsSpan(index + 1);
            Span<T> source = array.AsSpan(index, count - index);
            source.CopyTo(destination);
        }
        SetAtUnchecked(array, index, item);
        return count + 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int RemoveAt<T>(T[] array, int index, int count)
    {
        if (index < --count)
        {
            Span<T> destination = array.AsSpan(index++);
            Span<T> source = array.AsSpan(index, count - index);
            source.CopyTo(destination);
        }

        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            SetAtUnchecked(array, count, default);
        }

        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetAtUnchecked<T>(T[] array, int index, T item)
    {
        ref T dataRef = ref MemoryMarshal.GetArrayDataReference(array);
        Unsafe.Add(ref dataRef, index) = item;
    }

    [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
    private static void CollectionWasModified()
    {
        throw new InvalidOperationException("The collection was modified during enumeration.");
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
        ThrowHelpers.ThrowIfArrayIndexIsOutOfRange(arrayIndex, array, Count);
        ref KeyValuePair<Type, object> entryRef = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), arrayIndex);
        foreach (KeyValuePair<Type, object> entry in this)
        {
            entryRef = entry;
            entryRef = ref Unsafe.Add(ref entryRef, 1);
        }
    }

    IEnumerator<KeyValuePair<Type, object>> IEnumerable<KeyValuePair<Type, object>>.GetEnumerator()
    {
        MutationMonitor monitor = new(this);
        Memory<IntegerLookup> lookups = this.lookups.AsMemory(0, lookupCount);
        Memory<object> items = this.items.AsMemory(0, itemCount);
        return new EnumeratorImpl(monitor, lookups, items);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable<KeyValuePair<Type, object>>)this).GetEnumerator();
    }
}
