using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ObjectiveC;

namespace GameSharp.Collections;

/// <summary>
/// Represents a read-only view over a collection of objects, providing efficient lookup and retrieval by object type.
/// </summary>
/// <inheritdoc/>
public abstract class ReadOnlyTypeLookup : ICollection<KeyValuePair<Type, object>>
{
    internal interface IArrayBuilder<T>
    {
        T[] Array { get; }

        int LogicalCount { get; set; }

        void Mutate();
    }

    internal readonly ref struct TypeCluster
    {
        public int Index { get; init; }

        public IntegerLookup Lookup { get; init; }

        public ReadOnlySpan<object> Items { get; init; }
    }

    [StructLayout(LayoutKind.Explicit, Size = 8)] // Do not change
    internal struct IntegerLookup(int key, int index) : IComparable<int>
    {
        private static readonly int _structsPerVector;
        private static readonly Vector<int> _altMask;

        [FieldOffset(0)]
        public int key = key;

        [FieldOffset(4)]
        public int index = index;

        static IntegerLookup()
        {
            if (!Vector.IsHardwareAccelerated)
            {
                return;
            }

            _structsPerVector = Vector<int>.Count / 2;

            Span<int> mask = stackalloc int[Vector<int>.Count];
            ref int maskDataRef = ref MemoryMarshal.GetReference(mask);
            for (int i = 0; i < mask.Length; i++)
            {
                Unsafe.Add(ref maskDataRef, i) = i % 2;
            }

            _altMask = new Vector<int>(mask);
        }

        public static void Offset(Span<IntegerLookup> values, int count)
        {
            ref IntegerLookup currLookup = ref MemoryMarshal.GetReference(values);
            ref IntegerLookup endLookup = ref Unsafe.Add(ref currLookup, values.Length);

            if (Vector.IsHardwareAccelerated)
            {
                (int quotient, int remainder) = Math.DivRem(values.Length, _structsPerVector);

                if (quotient > 0)
                {
                    Vector<int> offset = _altMask * count;

                    ref int currInt = ref Unsafe.As<IntegerLookup, int>(ref currLookup);
                    ref int endInt = ref Unsafe.Add(ref currInt, quotient * Vector<int>.Count);

                    for (; Unsafe.IsAddressLessThan(ref currInt, ref endInt); currInt = ref Unsafe.Add(ref currInt, Vector<int>.Count))
                    {
                        Vector<int> vector = Vector.LoadUnsafe(ref currInt) + offset;
                        vector.StoreUnsafe(ref currInt);
                    }

                    currLookup = ref Unsafe.Add(ref currLookup, values.Length - remainder);
                }
            }

            for (; Unsafe.IsAddressLessThan(ref currLookup, ref endLookup); currLookup = ref Unsafe.Add(ref currLookup, 1))
            {
                currLookup.index += count;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ElementOffset(int byteOffset) => byteOffset >> 3;

        public readonly int CompareTo(int other)
        {
            return key.CompareTo(other);
        }
    }

    /// <inheritdoc cref="IEnumerator{T}"/>
    public ref struct Enumerator
    {
        private Cluster _current;
        private ClusterCollection.Enumerator _clusterEnum;
        private ReadOnlySpan<object>.Enumerator _itemEnum;

        /// <inheritdoc cref="IEnumerator{T}.Current"/>
        public readonly KeyValuePair<Type, object> Current => new(_current.Type, _itemEnum.Current);

        internal Enumerator(ReadOnlySpan<IntegerLookup> lookups, ReadOnlySpan<object> items)
        {
            _clusterEnum = new ClusterCollection.Enumerator(lookups, items);
        }

        /// <inheritdoc cref="IEnumerator.MoveNext()"/>
        public bool MoveNext()
        {
            while (!_itemEnum.MoveNext())
            {
                if (_clusterEnum.MoveNext())
                {
                    _current = _clusterEnum.Current;
                    _itemEnum = _current.Items.GetEnumerator();
                    continue;
                }

                return false;
            }

            return true;
        }
    }

    /// <inheritdoc cref="IEnumerator{T}"/>
    protected class EnumeratorImpl : IEnumerator<KeyValuePair<Type, object>>
    {
        private readonly ReadOnlyMemory<object> _items;
        private readonly ReadOnlyMemory<IntegerLookup> _lookups;
        private readonly int _lastIndex;
        private IEnumerator<object> _enumerator;
        private int _index;
        private Type? _type;

        /// <inheritdoc/>
        public KeyValuePair<Type, object> Current => new(_type!, _enumerator.Current);
        object IEnumerator.Current => Current;

        internal EnumeratorImpl(ReadOnlyMemory<IntegerLookup> lookups, ReadOnlyMemory<object> items)
        {
            _items = items;
            _lookups = lookups;
            _lastIndex = lookups.Length - 1;
            IEnumerable<object> empty = [];
            _enumerator = empty.GetEnumerator();
        }

        /// <inheritdoc/>
        public virtual bool MoveNext()
        {
            while (!_enumerator.MoveNext())
            {
                if (_index > _lastIndex)
                {
                    return false;
                }

                ReadOnlySpan<IntegerLookup> lookups = _lookups.Span;
                IntegerLookup lookup = lookups[_index];
                int nextIndex = _index + 1;
                _type = TypeInfo.Get(lookup.key).Type;
                if (nextIndex > _lastIndex)
                {
                    IEnumerable<object> cluster = MemoryMarshal.ToEnumerable(_items[lookup.index..]);
                    _enumerator = cluster.GetEnumerator();
                }
                else
                {
                    IEnumerable<object> cluster = MemoryMarshal.ToEnumerable(_items[lookup.index..lookups[nextIndex].index]);
                    _enumerator = cluster.GetEnumerator();
                }
                _index = nextIndex;
            }

            return true;
        }

        void IDisposable.Dispose() { }

        void IEnumerator.Reset() => throw new NotSupportedException();
    }

    /// <inheritdoc cref="Query{T}"/>
    public readonly struct Query
    {
        /// <inheritdoc cref="IEnumerator{T}"/>
        public ref struct Enumerator
        {
            private readonly ReadOnlySpan<object> _items;
            private readonly ReadOnlySpan<IntegerLookup> _lookups;
            private int _lookupsStart, _lookupsEnd, _itemsEnd, _id;
            private DerivedTypeCollection.Enumerator _derivedEnumerator;
            private ReadOnlySpan<object>.Enumerator _itemEnumerator;

            internal Enumerator(TypeInfo typeInfo, ReadOnlySpan<IntegerLookup> lookups, ReadOnlySpan<object> items)
            {
                _derivedEnumerator = typeInfo.Derived.GetEnumerator();
                _id = _derivedEnumerator.ID;
                _lookupsEnd = lookups.Length;
                _itemsEnd = items.Length;
                _lookups = lookups;
                _items = items;
            }

            /// <inheritdoc/>
            public readonly object Current => _itemEnumerator.Current;

            /// <inheritdoc/>
            public bool MoveNext()
            {
                if (_itemEnumerator.MoveNext())
                {
                    return true;
                }

                for (TypeCluster cluster; _derivedEnumerator.MoveNext(); _lookupsStart += cluster.Index)
                {
                    if (_derivedEnumerator.Current < _id)
                    {
                        _itemsEnd = _lookupsStart < _lookups.Length ? _lookups[_lookupsStart].index : _items.Length;
                        (_lookupsStart, _lookupsEnd) = (0, _lookupsStart);
                        _id = _derivedEnumerator.Current;
                    }

                    if (!TypeLookup.TryGetCluster(_lookups[_lookupsStart.._lookupsEnd], _items[.._itemsEnd], _derivedEnumerator.Current, out cluster))
                    {
                        continue;
                    }

                    _itemEnumerator = cluster.Items.GetEnumerator();
                    if (_itemEnumerator.MoveNext())
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private readonly ReadOnlyTypeLookup _typeLookup;
        private readonly TypeInfo _typeInfo;

        internal Query(ReadOnlyTypeLookup typeLookup, TypeInfo typeInfo)
        {
            _typeLookup = typeLookup;
            _typeInfo = typeInfo;
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator()"/>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(_typeInfo, _typeLookup.Lookups.Span, _typeLookup.Items.Span);
        }
    }

    /// <summary>
    /// Represents a filtered query over a collection of objects.
    /// </summary>
    /// <typeparam name="T">The type of objects to filter by.</typeparam>
    public readonly struct Query<T> where T : class
    {
        /// <inheritdoc cref="IEnumerator{T}"/>
        public ref struct Enumerator
        {
            private Query.Enumerator _enumerator;

            /// <inheritdoc cref="IEnumerator{T}.Current"/>
            public readonly T Current => Cast<T>(_enumerator.Current);

            internal Enumerator(TypeInfo typeInfo, ReadOnlySpan<IntegerLookup> lookups, ReadOnlySpan<object> items)
            {
                _enumerator = new Query.Enumerator(typeInfo, lookups, items);
            }

            /// <inheritdoc cref="IEnumerator.MoveNext()"/>
            public bool MoveNext() => _enumerator.MoveNext();
        }

        private readonly ReadOnlyTypeLookup _typeLookup;
        private readonly TypeInfo _typeInfo;

        internal Query(ReadOnlyTypeLookup typeLookup, TypeInfo typeInfo)
        {
            _typeLookup = typeLookup;
            _typeInfo = typeInfo;
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator()"/>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(_typeInfo, _typeLookup.Lookups.Span, _typeLookup.Items.Span);
        }
    }

    /// <summary>
    /// Represents a contiguous cluster of items of the same type.
    /// </summary>
    public readonly ref struct Cluster
    {
        internal ReadOnlySpan<object> Items { get; }

        private readonly TypeInfo _typeInfo;
        /// <summary>
        /// Gets the type of the items in the cluster.
        /// </summary>
        public Type Type => _typeInfo.Type;

        /// <summary>
        /// Gets the number of items in the cluster.
        /// </summary>
        public int Count => Items.Length;

        internal Cluster(TypeInfo typeInfo, ReadOnlySpan<object> items)
        {
            _typeInfo = typeInfo;
            Items = items;
        }

        /// <summary>
        /// Attempts to cast the items in the cluster to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to cast the items to.</typeparam>
        /// <param name="items">When this method returns, contains the items cast to the specified type, if the cast succeeded; otherwise, the default value.</param>
        /// <returns><see langword="true"/> if the items were successfully cast to the specified type; otherwise, <see langword="false"/>.</returns>
        public bool TryCast<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>(out ReadOnlySpan<T> items) where T : class
        {
            if (Is<T>())
            {
                ref object data = ref MemoryMarshal.GetReference(Items);
                items = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<object, T>(ref data), Items.Length);
                return true;
            }

            items = default;
            return false;
        }

        /// <summary>
        /// Casts the items in the cluster to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to cast the items to.</typeparam>
        /// <returns>A read-only span of the items cast to the specified type.</returns>
        /// <exception cref="InvalidCastException">Thrown if the items in the cluster cannot be cast to the specified type.</exception>
        public ReadOnlySpan<T> Cast<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>() where T : class
        {
            if (!Is<T>())
            {
                InvalidCast(typeof(T));
            }

            ref object data = ref MemoryMarshal.GetReference(Items);
            return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<object, T>(ref data), Items.Length);
        }

        /// <inheritdoc cref="Is(Type)"/>
        /// <typeparam name="T">The type to check for.</typeparam>
        public bool Is<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>() where T : class
        {
            TypeInfo typeInfo = TypeInfo.Get<T>();
            return typeInfo.EqualsOrAssignableFrom(_typeInfo);
        }

        /// <summary>
        /// Determines whether the cluster contains items of the specified type or any of its derived types.
        /// </summary>
        /// <param name="type">The type to check for.</param>
        /// <returns>
        /// <see langword="true"/> if the cluster contains items of the specified type or any of its derived types; 
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public bool Is([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type)
        {
            TypeInfo typeInfo = TypeInfo.Get(type);
            return typeInfo.EqualsOrAssignableFrom(_typeInfo);
        }

        [DoesNotReturn, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
        private void InvalidCast(Type targetType)
        {
            throw new InvalidCastException($"Cannot cast a cluster of type '{Type.FullName}' to '{targetType.FullName}'.");
        }

        /// <summary>
        /// Defines an implicit conversion from a <see cref="Cluster"/> to a <see cref="ReadOnlySpan{T}"/> of objects.
        /// </summary>
        /// <param name="value">The cluster to convert.</param>
        public static implicit operator ReadOnlySpan<object>(Cluster value) => value.Items;
    }

    /// <summary>
    /// Represents a collection of clusters, where each cluster contains contiguous items of the same type.
    /// </summary>
    public readonly ref struct ClusterCollection
    {
        /// <summary>
        /// An enumerator for iterating over the clusters in the collection.
        /// </summary>
        public ref struct Enumerator
        {
            private readonly ReadOnlySpan<object> _items;
            private readonly ref IntegerLookup _last;
            private ref IntegerLookup _current;

            /// <inheritdoc cref="IEnumerator{T}.Current"/>
            public Cluster Current { readonly get; private set; }

            internal Enumerator(ReadOnlySpan<IntegerLookup> lookups, ReadOnlySpan<object> items)
            {
                _current = ref MemoryMarshal.GetReference(lookups);
                _last = ref Unsafe.Add(ref _current, lookups.Length - 1);
                _items = items;
            }

            /// <inheritdoc cref="IEnumerator.MoveNext()"/>
            public bool MoveNext()
            {
                if (Unsafe.IsAddressGreaterThan(in _current, in _last))
                {
                    return false;
                }

                ref IntegerLookup nextRef = ref Unsafe.Add(ref _current, 1);
                TypeInfo typeInfo = TypeInfo.Get(_current.key);
                if (Unsafe.AreSame(in _current, in _last))
                {
                    Current = new Cluster(typeInfo, _items[_current.index..]);
                }
                else
                {
                    Current = new Cluster(typeInfo, _items[_current.index..nextRef.index]);
                }
                _current = ref nextRef;

                return true;
            }
        }

        private readonly ReadOnlySpan<IntegerLookup> _lookups;
        private readonly ReadOnlySpan<object> _items;

        /// <summary>
        /// Gets the number of clusters in the collection.
        /// </summary>
        public int Count => _lookups.Length;

        internal ClusterCollection(ReadOnlySpan<IntegerLookup> lookups, ReadOnlySpan<object> items)
        {
            _lookups = lookups;
            _items = items;
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator()"/>
        public Enumerator GetEnumerator() => new(_lookups, _items);
    }

    internal abstract ReadOnlyMemory<IntegerLookup> Lookups { get; }
    internal abstract ReadOnlyMemory<object> Items { get; }

    /// <inheritdoc/>
    public int Count => Items.Length;

    /// <summary>
    /// Gets a <see cref="ClusterCollection"/> representing the contiguous clusters of items in the collection, grouped by type.
    /// </summary>
    public ClusterCollection Clusters => new(Lookups.Span, Items.Span);

    bool ICollection<KeyValuePair<Type, object>>.IsReadOnly => true;

    /// <param name="type">The type of items to retrieve.</param>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="type"/> is <see langword="null"/>.</exception>
    /// <inheritdoc cref="GetAll{T}()"/>
    public Query GetAll([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type)
    {
        TypeInfo typeInfo = TypeInfo.Get(type);
        return new Query(this, typeInfo);
    }

    /// <summary>
    /// Retrieves all items of the specified type from the collection.
    /// </summary>
    /// <typeparam name="T">The type of items to retrieve from the collection.</typeparam>
    /// <returns>
    /// An enumerable sequence containing all contiguous items of the specified type found in the 
    /// collection; or an empty sequence if no such items are present.
    /// </returns>
    public Query<T> GetAll<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>() where T : class
    {
        TypeInfo typeInfo = TypeInfo.Get<T>();
        return new Query<T>(this, typeInfo);
    }

    /// <summary>
    /// Retrieves the first object of the specified type from the collection.
    /// </summary>
    /// <returns>The first object of the specified type found in the collection.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if no object of the specified type is found.</exception>
    /// <inheritdoc cref="TryGetOne{T}(out T)"/>
    public T GetOne<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>() where T : class
    {
        if (!TryGetOne(out T? result))
        {
            ItemNotPresent();
        }

        return result;
    }

    /// <typeparam name="T">The type of object to retrieve. Must be a reference type.</typeparam>
    /// <inheritdoc cref="TryGetOne(Type, out object)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetOne<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>([NotNullWhen(true)] out T? result) where T : class
    {
        TypeInfo typeInfo = TypeInfo.Get<T>();

        if (TryGetOne(typeInfo, out object? item))
        {
            result = Cast<T>(item);
            return true;
        }

        result = null;
        return false;
    }

    /// <summary>
    /// Retrieves the first object of the specified type from the collection.
    /// </summary>
    /// <returns>The first object of the specified type found in the collection.</returns>
    /// <inheritdoc cref="TryGetOne(Type, out object)"/>
    /// <exception cref="ArgumentException">Thrown if no object of the specified type is found.</exception>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="type"/> is <see langword="null"/>.</exception>
    public object GetOne(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (TypeInfo.TryGet(type, out TypeInfo? typeInfo) && TryGetOne(typeInfo, out object? item))
        {
            return item;
        }

        ItemNotPresent(nameof(type));
        return null;
    }

    /// <summary>
    /// Attempts to retrieve the first object of the specified type from the collection.
    /// </summary>
    /// <param name="type">The type of object to retrieve.</param>
    /// <param name="item">
    /// When this method returns, contains the first instance of the specified type if one is found; otherwise, 
    /// <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if an object of the specified type was found and assigned to <paramref name="item"/>; 
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool TryGetOne([NotNullWhen(true)] Type? type, [NotNullWhen(true)] out object? item)
    {
        if (type is { } && TypeInfo.TryGet(type, out TypeInfo? typeInfo))
        {
            return TryGetOne(typeInfo, out item);
        }

        item = null;
        return false;
    }

    /// <typeparam name="T">The type of object to check for. Must be a reference type.</typeparam>
    /// <inheritdoc cref="Contains(object, Type)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains<T>(T item) where T : class
    {
        return Contains(item, typeof(T));
    }

    /// <summary>
    /// Determines whether the collection contains the specified item of the given type.
    /// </summary>
    /// <param name="item">The object to locate in the collection.</param>
    /// <param name="type">The type to use when searching for the item.</param>
    /// <returns>
    /// <see langword="true"/> if the item is found in the collection for the specified type; 
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool Contains(object item, Type type)
    {
        if (item is { } && TypeInfo.TryGet(type, out TypeInfo? typeInfo))
        {
            return Contains(item, typeInfo);
        }

        return false;
    }

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator()"/>
    public Enumerator GetEnumerator() => new(Lookups.Span, Items.Span);

    private bool TryGetOne(TypeInfo typeInfo, [NotNullWhen(true)] out object? item)
    {
        ReadOnlySpan<IntegerLookup> lookups = Lookups.Span;
        ReadOnlySpan<object> items = Items.Span;

        DerivedTypeCollection.Enumerator enumerator = typeInfo.Derived.GetEnumerator();
        int startIndex = 0, endIndex = lookups.Length, pivot = enumerator.ID;
        while (enumerator.MoveNext())
        {
            if (enumerator.Current < pivot)
            {
                endIndex = startIndex;
                startIndex = 0;
                pivot = enumerator.Current;
            }

            ref IntegerLookup lookup = ref lookups[startIndex..endIndex].HybridSearch(enumerator.Current, out int byteOffset, out bool exists);
            if (exists)
            {
                item = items[lookup.index];
                return true;
            }
            startIndex += IntegerLookup.ElementOffset(byteOffset);
        }

        item = null;
        return false;
    }

    private bool Contains(object item, TypeInfo typeInfo)
    {
        Query query = new(this, typeInfo);

        foreach (object obj in query)
        {
            if (ReferenceEquals(obj, item))
            {
                return true;
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T Cast<T>(object obj) where T : class
    {
#if DEBUG
        return (T)obj;
#else   
        return Unsafe.As<T>(obj);
#endif
    }

    [DoesNotReturn, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
    private static void ItemNotPresent(string? paramName = null)
    {
        throw new ArgumentException("No items of the specified type were present the collection.", paramName);
    }

    IEnumerator<KeyValuePair<Type, object>> IEnumerable<KeyValuePair<Type, object>>.GetEnumerator()
    {
        return new EnumeratorImpl(Lookups, Items);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return new EnumeratorImpl(Lookups, Items);
    }

    bool ICollection<KeyValuePair<Type, object>>.Contains(KeyValuePair<Type, object> item)
    {
        return Contains(item.Value, item.Key);
    }

    void ICollection<KeyValuePair<Type, object>>.Add(KeyValuePair<Type, object> item) => throw new NotSupportedException();

    void ICollection<KeyValuePair<Type, object>>.Clear() => throw new NotSupportedException();

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

    bool ICollection<KeyValuePair<Type, object>>.Remove(KeyValuePair<Type, object> item) => throw new NotSupportedException();
}
