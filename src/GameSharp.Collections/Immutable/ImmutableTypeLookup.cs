using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GameSharp.Collections.Immutable;

/// <summary>
/// Represents an immutable collection of objects, providing efficient lookup and retrieval by object type.
/// </summary>
/// <inheritdoc/>
public sealed partial class ImmutableTypeLookup : ReadOnlyTypeLookup, IReadOnlyTypeLookup, ICloneable
{
    private struct ArrayBuilder<T>(T[] destination, int logicalCount) : IArrayBuilder<T>
    {
        private readonly ArraySegment<T> _source = new(destination, 0, logicalCount);
        public readonly T[] Array => destination;
        public int LogicalCount { get; set; } = logicalCount;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Mutate()
        {
            if (ReferenceEquals(_source.Array, destination))
            {
                destination = GC.AllocateUninitializedArray<T>(LogicalCount);
                ArraySegment<T> src = _source.Slice(0, Math.Min(_source.Count, LogicalCount));
                ArraySegment<T> dst = new(destination);
                src.CopyTo(dst);
            }
            else
            {
                CollectableArrayFactory.Default.SetCount(ref destination, LogicalCount);
            }
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

            /// <inheritdoc cref="IEnumerator.Current"/>
            public readonly TypeLookupCluster Current => _core.Current;

            internal Enumerator(ReadOnlySpan<IntegerLookup> lookups, ReadOnlySpan<object> items)
            {
                _core = new TypeLookupClusterEnumeratorCore(lookups, items);
            }

            /// <inheritdoc cref="IEnumerator.MoveNext"/>
            public bool MoveNext() => _core.MoveNext();
        }

        private readonly ImmutableTypeLookup _lookup;

        /// <inheritdoc cref="IReadOnlyCollection{T}.Count"/>
        public int Count => _lookup.lookupCount;

        internal ClusterCollection(ImmutableTypeLookup lookup) => _lookup = lookup;

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public Enumerator GetEnumerator()
        {
            Span<IntegerLookup> lookups = _lookup.lookups.AsSpan(0, _lookup.lookupCount);
            Span<object> items = _lookup.items.AsSpan(0, _lookup.itemCount);
            return new Enumerator(lookups, items);
        }
    }

    /// <inheritdoc cref="IEnumerator{T}"/>
    public ref struct Enumerator
    {
        private TypeLookupEnumeratorCore<TypeLookupClusterEnumeratorCore> _core;

        /// <inheritdoc cref="IEnumerator.Current"/>
        public readonly KeyValuePair<Type, object> Current => _core.Current;

        internal Enumerator(ReadOnlySpan<IntegerLookup> lookups, ReadOnlySpan<object> items)
        {
            TypeLookupClusterEnumeratorCore clusterEnum = new(lookups, items);
            _core = new TypeLookupEnumeratorCore<TypeLookupClusterEnumeratorCore>(clusterEnum);
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

            /// <inheritdoc cref="IEnumerator.Current"/>
            public readonly object Current => _core.Current;

            internal Enumerator(TypeInfo typeInfo, ReadOnlySpan<IntegerLookup> lookups, ReadOnlySpan<object> items)
            {
                _core = new TypeLookupQueryEnumeratorCore(typeInfo, lookups, items);
            }

            /// <inheritdoc cref="IEnumerator.MoveNext"/>
            public bool MoveNext() => _core.MoveNext();
        }

        private readonly ImmutableTypeLookup _lookup;
        private readonly TypeInfo _typeInfo;

        internal Query(ImmutableTypeLookup lookup, TypeInfo typeInfo)
        {
            _lookup = lookup;
            _typeInfo = typeInfo;
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public Enumerator GetEnumerator()
        {
            ReadOnlySpan<IntegerLookup> lookups = _lookup.lookups.AsSpan(0, _lookup.lookupCount);
            ReadOnlySpan<object> items = _lookup.items.AsSpan(0, _lookup.itemCount);
            return new Enumerator(_typeInfo, lookups, items);
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
        public ref struct Enumerator
        {
            private TypeLookupQueryEnumeratorCore _core;

            /// <inheritdoc cref="IEnumerator{T}.Current"/>
            public readonly T Current => Unsafe.As<T>(_core.Current);

            internal Enumerator(TypeInfo typeInfo, ReadOnlySpan<IntegerLookup> lookups, ReadOnlySpan<object> items)
            {
                _core = new TypeLookupQueryEnumeratorCore(typeInfo, lookups, items);
            }

            /// <inheritdoc cref="IEnumerator.MoveNext"/>
            public bool MoveNext() => _core.MoveNext();
        }

        private readonly ImmutableTypeLookup _lookup;
        private readonly TypeInfo _typeInfo;

        internal Query(ImmutableTypeLookup lookup, TypeInfo typeInfo)
        {
            _lookup = lookup;
            _typeInfo = typeInfo;
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public Enumerator GetEnumerator()
        {
            ReadOnlySpan<IntegerLookup> lookups = _lookup.lookups.AsSpan(0, _lookup.lookupCount);
            ReadOnlySpan<object> items = _lookup.items.AsSpan(0, _lookup.itemCount);
            return new Enumerator(_typeInfo, lookups, items);
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

    internal IntegerLookup[] lookups;
    internal int lookupCount;

    internal object[] items;
    internal int itemCount;

    /// <summary>
    /// Gets a <see cref="ClusterCollection"/> representing the contiguous clusters of items in the collection, grouped by type.
    /// </summary>
    public ClusterCollection Clusters => new(this);

    /// <inheritdoc/>
    public override int Count => itemCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImmutableTypeLookup"/> class that is empty.
    /// </summary>
    public ImmutableTypeLookup()
    {
        lookups = [];
        items = [];
    }

    internal ImmutableTypeLookup(IntegerLookup[] lookups, int lookupCount, object[] items, int itemCount)
    {
        this.lookups = lookups;
        this.lookupCount = lookupCount;

        this.items = items;
        this.itemCount = itemCount;
    }

    internal ImmutableTypeLookup(IntegerLookup[] lookups, object[] items)
    {
        this.lookups = lookups;
        lookupCount = lookups.Length;

        this.items = items;
        itemCount = items.Length;
    }

    /// <inheritdoc cref="ICloneable.Clone"/>
    public ImmutableTypeLookup Clone() => Count == 0 ? [] : new(lookups[..lookupCount], items[..itemCount]);

    private protected override bool Contains(object item, TypeInfo typeInfo)
    {
        TypeLookupQueryEnumeratorCore enumerator = new(typeInfo, lookups.AsSpan(0, lookupCount), items.AsSpan(0, itemCount));

        while (enumerator.MoveNext())
        {
            if (ReferenceEquals(enumerator.Current, item))
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

    /// <typeparam name="T"> The type of the item to add. Must be a reference type.</typeparam>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="item"/> is <see langword="null"/>.</exception>
    /// <inheritdoc cref="Add(object, Type)"/>
    public ImmutableTypeLookup Add<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>(T item) where T : class
    {
        ArgumentNullException.ThrowIfNull(item);

        TypeInfo typeInfo = TypeInfo.Get<T>();
        return Add(item, typeInfo);
    }

    /// <returns>
    /// A new <see cref="ImmutableTypeLookup"/> that contains the specified item, or the current collection if the item is
    /// already present.
    /// </returns>
    /// <inheritdoc cref="MutableTypeLookup.Add(object, Type)"/>
    public ImmutableTypeLookup Add(object item, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(type);

        TypeInfo typeInfo = TypeInfo.Get(type);
        return Add(item, typeInfo);
    }

    internal ImmutableTypeLookup Add(object item, TypeInfo typeInfo)
    {
        ArrayBuilder<IntegerLookup> lookupBuilder = new(lookups, lookupCount);
        ArrayBuilder<object> itemBuilder = new(items, itemCount);

        TypeLookup.Add(ref lookupBuilder, ref itemBuilder, item, typeInfo);

        if (ReferenceEquals(lookupBuilder.Array, lookups) && ReferenceEquals(itemBuilder.Array, items))
        {
            return this;
        }

        return new ImmutableTypeLookup(lookupBuilder.Array, lookupBuilder.LogicalCount, itemBuilder.Array, itemBuilder.LogicalCount);
    }

    /// <typeparam name="T">The type of the item to remove. Must be a reference type.</typeparam>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="item"/> is <see langword="null"/>.</exception>
    /// <inheritdoc cref="Remove(object, Type)"/>
    public ImmutableTypeLookup Remove<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>(T item) where T : class
    {
        ArgumentNullException.ThrowIfNull(item);

        TypeInfo typeInfo = TypeInfo.Get<T>();
        return Remove(item, typeInfo);
    }

    /// <returns>
    /// A new <see cref="ImmutableTypeLookup"/> with the specified item removed; or the current 
    /// collection if the item is not found.
    /// </returns>
    /// <inheritdoc cref="MutableTypeLookup.Remove(object, Type)"/>
    public ImmutableTypeLookup Remove(object item, Type type)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(type);

        if (TypeInfo.TryGet(type, out TypeInfo? typeInfo))
        {
            return Remove(item, typeInfo);
        }

        return this;
    }

    internal ImmutableTypeLookup Remove(object item, TypeInfo typeInfo)
    {
        ArrayBuilder<IntegerLookup> lookupBuilder = new(lookups, lookupCount);
        ArrayBuilder<object> itemBuilder = new(items, itemCount);

        TypeLookup.Remove(ref lookupBuilder, ref itemBuilder, item, typeInfo);

        if (ReferenceEquals(lookupBuilder.Array, lookups) && ReferenceEquals(itemBuilder.Array, items))
        {
            return this;
        }

        return new ImmutableTypeLookup(lookupBuilder.Array, lookupBuilder.LogicalCount, itemBuilder.Array, itemBuilder.LogicalCount);
    }

    /// <inheritdoc cref="ITypeLookup.Replace(object, object, Type)"/>
    public ImmutableTypeLookup Replace(object? existingItem, object newItem, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type)
    {
        ArgumentNullException.ThrowIfNull(newItem);
        TypeInfo typeInfo = TypeInfo.Get(type);
        return existingItem is { } ? Replace(existingItem, newItem, typeInfo) : Add(newItem, typeInfo);
    }
    /// <inheritdoc cref="ITypeLookup.Replace{T}(T, T)"/>
    public ImmutableTypeLookup Replace<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>(T? existingItem, T newItem) where T : class
    {
        ArgumentNullException.ThrowIfNull(newItem);
        TypeInfo typeInfo = TypeInfo.Get<T>();
        return existingItem is { } ? Replace(existingItem, newItem, typeInfo) : Add(newItem, typeInfo);
    }

    internal ImmutableTypeLookup Replace(object existingItem, object newItem, TypeInfo typeInfo)
    {
        ArrayBuilder<object> itemBuilder = new(items, itemCount);

        TypeLookup.Replace(lookups.AsSpan(0, lookupCount), ref itemBuilder, existingItem, newItem, typeInfo);

        if (ReferenceEquals(itemBuilder.Array, items))
        {
            return this;
        }

        return new ImmutableTypeLookup(lookups, lookupCount, itemBuilder.Array, itemBuilder.LogicalCount);
    }

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    public Enumerator GetEnumerator()
    {
        Span<IntegerLookup> lookups = this.lookups.AsSpan(0, lookupCount);
        Span<object> items = this.items.AsSpan(0, itemCount);
        return new Enumerator(lookups, items);
    }

    /// <summary>
    /// Creates a new <see cref="Builder"/> instance.
    /// </summary>
    /// <returns>A new <see cref="Builder"/> instance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Builder CreateBuilder()
    {
        return [];
    }

    /// <summary>
    /// Mutates an immutable type lookup in-place with optimistic locking transaction semantics via a specified transformation function. 
    /// The transformation is retried as many times as necessary to win the optimistic locking race.
    /// </summary>
    /// <typeparam name="T">The type of the transformer.</typeparam>
    /// <param name="location1">The immutable type lookup to be changed.</param>
    /// <param name="transformer">A tranformer that produces a new immutable type lookup from the old.</param>
    /// <returns>
    /// <see langword="true"/> if the location's value is changed by applying the result of the transformer function; 
    /// <see langword="false"/> if the location's value remained the same because the last invocation of transformer returned the existing value.
    /// </returns>
    public static bool InterlockedUpdate<T>(ref ImmutableTypeLookup location1, in T transformer) 
        where T : struct, IImmutableTypeLookupTransformer, allows ref struct
    {
        ImmutableTypeLookup currLookup = Volatile.Read(ref location1), prevLookup;
        do
        {
            ImmutableTypeLookup newLookup = transformer.Apply(currLookup);
            if (ReferenceEquals(currLookup, newLookup))
            {
                return false;
            }
            (prevLookup, currLookup) = (currLookup, Interlocked.CompareExchange(ref location1, newLookup, currLookup));
        }
        while (!ReferenceEquals(prevLookup, currLookup));
        return true;
    }

    /// <summary>
    /// Atomically updates the <see cref="ImmutableTypeLookup"/> instance at the specified location using the provided transformation function.
    /// </summary>
    /// <inheritdoc cref="InterlockedUpdate{TState1}(ref ImmutableTypeLookup, Func{ImmutableTypeLookup, TState1, ImmutableTypeLookup}, TState1)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining), Obsolete("This method is obsolete. Use InterlockedUpdate<T>(ImmutableTypeLookup, in T) instead.")]
    public static bool InterlockedUpdate(ref ImmutableTypeLookup location1, Func<ImmutableTypeLookup, ImmutableTypeLookup> factory)
    {
        DelegateTransformation transformation = new() { Transformation = factory };
        return InterlockedUpdate(ref location1, in transformation);
    }

    /// <inheritdoc cref="InterlockedUpdate{TState1, TState2}(ref ImmutableTypeLookup, Func{ImmutableTypeLookup, TState1, TState2, ImmutableTypeLookup}, TState1, TState2)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining), Obsolete("This method is obsolete. Use InterlockedUpdate<T>(ImmutableTypeLookup, in T) instead.")]
    public static bool InterlockedUpdate<TState1>(ref ImmutableTypeLookup location1, 
        Func<ImmutableTypeLookup, TState1, ImmutableTypeLookup> factory, 
        TState1 state1) 
        where TState1 : allows ref struct
    {
        DelegateTransformation<TState1> transformation = new() { Transformation = factory, State = state1 };
        return InterlockedUpdate(ref location1, in transformation);
    }

    /// <summary>
    /// Atomically updates the <see cref="ImmutableTypeLookup"/> instance at the specified location using the provided transformation function and state.
    /// </summary>
    /// <typeparam name="TState1">The type of the state parameter.</typeparam>
    /// <typeparam name="TState2">The type of the state parameter.</typeparam>
    /// <param name="location1">The location of the <see cref="ImmutableTypeLookup"/> instance to update.</param>
    /// <param name="factory">The transformation function to apply.</param>
    /// <param name="state1">The state to pass to the transformation function.</param>
    /// <param name="state2">The state to pass to the transformation function.</param>
    /// <returns><see langword="true"/> if the update was successful; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining), Obsolete("This method is obsolete. Use InterlockedUpdate<T>(ImmutableTypeLookup, in T) instead.")]
    public static bool InterlockedUpdate<TState1, TState2>(ref ImmutableTypeLookup location1, 
        Func<ImmutableTypeLookup, TState1, TState2, ImmutableTypeLookup> factory, 
        TState1 state1,
        TState2 state2) 
        where TState1 : allows ref struct
        where TState2 : allows ref struct
    {
        DelegateTransformation<TState1, TState2> transformation = new() { Transformation = factory, State1 = state1, State2 = state2 };
        return InterlockedUpdate(ref location1, in transformation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool InterlockedAdd(ref ImmutableTypeLookup location1, object item, TypeInfo typeInfo)
    {
        AddTransformation transformation = new() { Item = item, TypeInfo = typeInfo };
        return InterlockedUpdate(ref location1, in transformation);
    }

    /// <summary>
    /// Atomically adds an item to an immutable type lookup using the specified runtime type.
    /// </summary>
    /// <param name="location1">A reference to the immutable type lookup to update.</param>
    /// <param name="item">The item to add.</param>
    /// <param name="type">The runtime type used to resolve lookup metadata for the item.</param>
    /// <returns><see langword="true"/> if the item was added; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="item"/> or <paramref name="type"/> is <see langword="null"/>.</exception>
    public static bool InterlockedAdd(ref ImmutableTypeLookup location1, 
        object item, 
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(type);
        TypeInfo typeInfo = TypeInfo.Get(type);
        return InterlockedAdd(ref location1, item, typeInfo);
    }

    /// <summary>
    /// Atomically adds an item to an immutable type lookup.
    /// </summary>
    /// <typeparam name="T">The reference type of the item to add.</typeparam>
    /// <param name="location1">A reference to the immutable lookup to update.</param>
    /// <param name="item">The item to add. Cannot be <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the lookup was updated; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="item"/> is <see langword="null"/>.</exception>
    public static bool InterlockedAdd<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>(ref ImmutableTypeLookup location1, 
        T item) 
        where T : class
    {
        ArgumentNullException.ThrowIfNull(item);
        TypeInfo typeInfo = TypeInfo.Get<T>();
        return InterlockedAdd(ref location1, item, typeInfo);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool InterlockedRemove(ref ImmutableTypeLookup location1, object item, TypeInfo typeInfo)
    {
        RemoveTransformation transformation = new() { Item = item, TypeInfo = typeInfo };
        return InterlockedUpdate(ref location1, in transformation);
    }

    /// <summary>
    /// Atomically removes an item from an immutable type lookup using the specified runtime type.
    /// </summary>
    /// <param name="location1">A reference to the immutable type lookup to update.</param>
    /// <param name="item">The item to remove.</param>
    /// <param name="type">The runtime type used to resolve lookup metadata for the item.</param>
    /// <returns><see langword="true"/> if the item was removed; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="item"/> or <paramref name="type"/> is <see langword="null"/>.</exception>
    public static bool InterlockedRemove(ref ImmutableTypeLookup location1,
        object item,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(type);
        TypeInfo typeInfo = TypeInfo.Get(type);
        return InterlockedRemove(ref location1, item, typeInfo);
    }

    /// <summary>
    /// Atomically removes an item from an immutable type lookup.
    /// </summary>
    /// <typeparam name="T">The reference type of the item to remove.</typeparam>
    /// <param name="location1">A reference to the immutable lookup to update.</param>
    /// <param name="item">The item to remove. Cannot be <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the lookup was updated; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="item"/> is <see langword="null"/>.</exception>
    public static bool InterlockedRemove<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>(ref ImmutableTypeLookup location1,
        T item)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(item);
        TypeInfo typeInfo = TypeInfo.Get<T>();
        return InterlockedRemove(ref location1, item, typeInfo);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool InterlockedReplace(ref ImmutableTypeLookup location1, object existingItem, object newItem, TypeInfo typeInfo)
    {
        ReplaceTransformation transformation = new() { ExistingItem = existingItem, NewItem = newItem, TypeInfo = typeInfo };
        return InterlockedUpdate(ref location1, in transformation);
    }

    /// <summary>
    /// Atomically adds or replaces an item in an immutable type lookup.
    /// </summary>
    /// <remarks>
    /// When <paramref name="existingItem"/> is <see langword="null"/>, performs an add operation; 
    /// otherwise, performs a conditional replace operation.
    /// </remarks>
    /// <param name="location1">The location that stores the immutable type lookup to update.</param>
    /// <param name="existingItem">
    /// The expected current item for the specified type, 
    /// or <see langword="null"/> to add a new item when no value is expected.
    /// </param>
    /// <param name="newItem">The item to store for the specified type.</param>
    /// <param name="type">The type of item to add or replace.</param>
    /// <returns><see langword="true"/> if the lookup was updated; otherwise, <see langword="false"/>.</returns>
    public static bool InterlockedReplace(ref ImmutableTypeLookup location1,
        object? existingItem,
        object newItem,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type)
    {
        ArgumentNullException.ThrowIfNull(newItem);
        ArgumentNullException.ThrowIfNull(type);
        TypeInfo typeInfo = TypeInfo.Get(type);
        return existingItem is null
            ? InterlockedAdd(ref location1, newItem, typeInfo)
            : InterlockedReplace(ref location1, existingItem, newItem, typeInfo);
    }

    /// <typeparam name="T">The reference type of the item to add or replace.</typeparam>
    /// <inheritdoc cref="InterlockedReplace(ref ImmutableTypeLookup, object?, object, Type)"/>
    public static bool InterlockedReplace<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>(ref ImmutableTypeLookup location1,
        T? existingItem,
        T newItem)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(newItem);
        TypeInfo typeInfo = TypeInfo.Get<T>();
        return existingItem is null
            ? InterlockedAdd(ref location1, newItem, typeInfo)
            : InterlockedReplace(ref location1, existingItem, newItem, typeInfo);
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

    object ICloneable.Clone()
    {
        return Clone();
    }

    IEnumerator<KeyValuePair<Type, object>> IEnumerable<KeyValuePair<Type, object>>.GetEnumerator()
    {
        return new TypeLookupEnumerator(lookups.AsMemory(0, lookupCount), items.AsMemory(0, itemCount));
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable<KeyValuePair<Type, object>>)this).GetEnumerator();
    }
}
