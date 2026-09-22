using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GameSharp.Collections;

/// <summary>
/// Represents a collection of objects, providing efficient lookup and retrieval by object type.
/// </summary>
public class TypeLookup : ReadOnlyTypeLookup, ICollection<KeyValuePair<Type, object>>
{
    private protected readonly ref struct ArrayBuilder<T>(ref T[] array, ref int logicalCount, ref int version) : IArrayBuilder<T>
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
            if (_logicalCount > _array.Length)
            {
                GrowArray();
            }

            _version++;
        }

        private void GrowArray()
        {
            int capacity = _array.Length == 0 ? 16 : _array.Length;

            while (capacity < _logicalCount)
            {
                capacity += capacity >> 1;
            }

            System.Array.Resize(ref _array, capacity);
        }
    }

    /// <inheritdoc cref="IEnumerator{T}"/>
    public new readonly ref struct Enumerator
    {
        private readonly ReadOnlyTypeLookup.Enumerator _enumerator;
        private readonly MutationMonitor _monitor;

        /// <inheritdoc cref="IEnumerator.Current"/>
        public readonly KeyValuePair<Type, object> Current => _enumerator.Current;

        internal Enumerator(TypeLookup collection)
        {
            _monitor = new MutationMonitor(collection);
            _enumerator = new ReadOnlyTypeLookup.Enumerator(collection.Lookups.Span, collection.Items.Span);
        }

        /// <inheritdoc cref="IEnumerator.MoveNext"/>
        public bool MoveNext()
        {
            _monitor.ThrowIfCollectionWasModified();
            return _enumerator.MoveNext();
        }
    }

    private new sealed class EnumeratorImpl(TypeLookup collection) : ReadOnlyTypeLookup.EnumeratorImpl(collection.Lookups, collection.Items)
    {
        private readonly MutationMonitor _monitor = new(collection);

        public override bool MoveNext()
        {
            _monitor.ThrowIfCollectionWasModified();
            return base.MoveNext();
        }
    }

    private protected readonly struct MutationMonitor(TypeLookup collection)
    {
        private readonly int _version = collection._version;

        public bool WasModified => collection._version != _version;

        [StackTraceHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ThrowIfCollectionWasModified()
        {
            if (WasModified)
            {
                CollectionWasModified();
            }
        }

        [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
        private static void CollectionWasModified()
        {
            throw new InvalidOperationException();
        }
    }

    private int _version = int.MinValue;

    private protected IntegerLookup[] lookups;
    private protected int lookupCount;

    private protected object[] items;
    private protected int itemCount;

    bool ICollection<KeyValuePair<Type, object>>.IsReadOnly => false;

    internal override ReadOnlyMemory<IntegerLookup> Lookups => new(lookups, 0, lookupCount);
    internal override ReadOnlyMemory<object> Items => new(items, 0, itemCount);

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

    /// <inheritdoc cref="ICollection{T}.Clear"/>
    public void Clear()
    {
        lookupCount = 0;

        Array.Clear(items, 0, itemCount);
        itemCount = 0;

        _version++;
    }

    /// <typeparam name="T"> The type of the item to add. Must be a reference type.</typeparam>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="item"/> is <see langword="null"/>.</exception>
    /// <inheritdoc cref="Add(object, Type)"/>
    public bool Add<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>(T item) where T : class
    {
        ArgumentNullException.ThrowIfNull(item);
        TypeInfo typeInfo = TypeInfo.Get<T>();
        return Add(item, typeInfo);
    }

    /// <summary>
    /// Adds the specified item to the collection.
    /// </summary>
    /// <param name="type">The type of the item to add.</param>
    /// <param name="item">The item to add to the collection. Cannot be <see langword="null"/>.</param>
    /// <returns>
    /// <see langword="true"/> if the item was successfully added; <see langword="false"/> if the item is
    /// already present.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if either the <paramref name="item"/> or the <paramref name="type"/> is <see langword="null"/>.
    /// </exception>
    public bool Add(object item, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(type);
        TypeInfo typeInfo = TypeInfo.Get(type);
        return Add(item, typeInfo);
    }

    /// <typeparam name="T">The type of the item to remove. Must be a reference type.</typeparam>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="item"/> is <see langword="null"/>.</exception>
    /// <inheritdoc cref="Remove(object, Type)"/>
    public bool Remove<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>(T item) where T : class
    {
        ArgumentNullException.ThrowIfNull(item);
        TypeInfo typeInfo = TypeInfo.Get<T>();
        return Remove(item, typeInfo);
    }

    /// <summary>
    /// Removes all occurrences of the specified item from the collection.
    /// </summary>
    /// <param name="type">The type of the item to remove.</param>
    /// <param name="item">The item to remove from the collection.</param>
    /// <returns>
    /// <see langword="true"/> if item was found and removed from the collection; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if either the <paramref name="item"/> or the <paramref name="type"/> is <see langword="null"/>.
    /// </exception> 
    public bool Remove(object item, Type type)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(type);
        return TypeInfo.TryGet(type, out TypeInfo? typeInfo) && Remove(item, typeInfo);
    }

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    public new Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private protected virtual bool Add(object item, TypeInfo typeInfo)
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
        if (TryGetCluster(lookups, items, typeInfo.ID, out TypeCluster cluster))
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
    private protected virtual bool Remove(object item, TypeInfo typeInfo)
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

        TypeCluster cluster;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private protected void GetBuilders(out ArrayBuilder<IntegerLookup> lookupBuilder, out ArrayBuilder<object> itemBuilder)
    {
        lookupBuilder = new(ref lookups, ref lookupCount, ref _version);
        itemBuilder = new(ref items, ref itemCount, ref _version);
    }

    internal static bool TryGetCluster(ReadOnlySpan<IntegerLookup> lookups, ReadOnlySpan<object> items, int id, out TypeCluster cluster)
    {
        IntegerLookup lookup;

        if (lookups.IsEmpty)
        {
            lookup = new IntegerLookup(id, 0);
            cluster = new TypeCluster()
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
            cluster = new TypeCluster()
            {
                Index = index,
                Lookup = result,
                Items = items[Unsafe.AreSame(ref result, ref last) ? result.index.. : result.index..Unsafe.Add(ref result, 1).index]
            };

            return true;
        }

        if (Unsafe.IsAddressGreaterThan(ref result, ref last))
        {
            cluster = new TypeCluster()
            {
                Index = lookups.Length,
                Lookup = new IntegerLookup(id, items.Length),
                Items = default
            };

            return false;
        }

        cluster = new TypeCluster()
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

    [SuppressMessage("Trimming", "IL2072")]
    void ICollection<KeyValuePair<Type, object>>.Add(KeyValuePair<Type, object> item)
    {
        Add(item.Value, item.Key);
    }

    void ICollection<KeyValuePair<Type, object>>.Clear()
    {
        Clear();
    }

    bool ICollection<KeyValuePair<Type, object>>.Remove(KeyValuePair<Type, object> item)
    {
        return Remove(item.Value, item.Key);
    }

    IEnumerator<KeyValuePair<Type, object>> IEnumerable<KeyValuePair<Type, object>>.GetEnumerator()
    {
        return new EnumeratorImpl(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return new EnumeratorImpl(this);
    }
}
