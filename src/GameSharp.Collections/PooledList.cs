using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GameSharp.Collections;

/// <summary>
/// A list of elements that is backed by a pooled array.
/// </summary>
/// <typeparam name="T">The type of elements stored in the list.</typeparam>
public ref struct PooledList<T>
{
    internal Array? array;
    internal Span<T> span;

    /// <summary>
    /// Gets the number of elements contained in the list.
    /// </summary>
    public int Count
    {
        readonly get => span.Length;
        set
        {
            ObjectDisposedException.ThrowIf(array is null, typeof(PooledList<T>));
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            GetLengthRefUnsafe(ref span) = value;
            PooledArrayFactory.Default.SetCount(ref array, ref span);
        }
    }

    /// <summary>
    /// Gets or sets the element at the specified index in the list.
    /// </summary>
    /// <param name="index">The zero-based index of the element to get or set.</param>
    /// <returns>The element at the specified index.</returns>
    public readonly T this[int index]
    {
        get
        {
            ObjectDisposedException.ThrowIf(array is null, typeof(PooledList<T>));
            ThrowIfIndexOutOfRange(index);
            return Unsafe.Add(ref MemoryMarshal.GetReference(span), index);
        }
        set
        {
            ObjectDisposedException.ThrowIf(array is null, typeof(PooledList<T>));
            ThrowIfIndexOutOfRange(index);
            Unsafe.Add(ref MemoryMarshal.GetReference(span), index) = value;
        }
    }

    internal PooledList(int count, int capacity)
    {
        span = PooledArrayFactory.Default.Get<T>(out array, count, capacity);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PooledList{T}"/> struct with the specified initial capacity.
    /// </summary>
    /// <param name="initialCapacity">The initial number of elements that the list can contain.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="initialCapacity"/> is negative.</exception>
    [Obsolete("Use PooledList.Empty<T>(initialCapacity) instead.", error: true)]
    public PooledList(int initialCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(initialCapacity);
        span = PooledArrayFactory.Default.Get<T>(out array, 0, initialCapacity);
    }

    /// <summary>
    /// Converts the <see cref="PooledList{T}"/> to an <see cref="IMemoryOwner{T}"/> that owns the underlying array.
    /// </summary>
    /// <remarks>
    /// Subsequent operations on the <see cref="PooledList{T}"/> after calling this method will throw 
    /// an <see cref="ObjectDisposedException"/>.
    /// </remarks>
    /// <returns>An <see cref="IMemoryOwner{T}"/> that owns the underlying array.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the list has been disposed.</exception>
    public IMemoryOwner<T> ToMemoryOwner()
    {
        ObjectDisposedException.ThrowIf(array is null, typeof(PooledList<T>));
        PooledMemoryOwner<T> owner = new(array, span.Length);
        DisposeUnsafe();
        return owner;
    }

    /// <summary>
    /// Adds an element to the end of the list.
    /// </summary>
    /// <param name="item">The element to add.</param>
    /// <exception cref="ObjectDisposedException">Thrown when the list has been disposed.</exception>
    public void Add(T item)
    {
        ObjectDisposedException.ThrowIf(array is null, typeof(PooledList<T>));
        Span<T> newSpan = span;
        int index = GetLengthRefUnsafe(ref newSpan)++;
        PooledArrayFactory.Default.EnsureCapacity(ref array, ref newSpan);
        Unsafe.Add(ref MemoryMarshal.GetReference(newSpan), index) = item;
        span = newSpan;
    }

    /// <summary>
    /// Adds a range of elements to the end of the list.
    /// </summary>
    /// <param name="items">The elements to add.</param>
    public void AddRange(ReadOnlySpan<T> items)
    {
        ObjectDisposedException.ThrowIf(array is null, typeof(PooledList<T>));

        if (items.IsEmpty)
        {
            return;
        }

        Span<T> newSpan = span;
        int index = span.Length;
        GetLengthRefUnsafe(ref newSpan) += items.Length;
        PooledArrayFactory.Default.EnsureCapacity(ref array, ref newSpan);
        Span<T> dst = newSpan.Slice(index, items.Length);
        items.CopyTo(dst);
        span = newSpan;
    }

    /// <summary>
    /// Inserts an element at the specified index in the list.
    /// </summary>
    /// <param name="item">The element to insert.</param>
    /// <param name="index">The zero-based index at which to insert the element.</param>
    /// <exception cref="ObjectDisposedException">Thrown when the list has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is out of range.</exception>
    public void Insert(T item, int index)
    {
        ObjectDisposedException.ThrowIf(array is null, typeof(PooledList<T>));
        if ((uint)index > (uint)span.Length)
        {
            IndexWasOutOfRange(index, nameof(index));
        }

        Span<T> newSpan = span;
        int lastIndex = GetLengthRefUnsafe(ref newSpan)++;
        PooledArrayFactory.Default.EnsureCapacity(ref array, ref newSpan);
        ref T data = ref MemoryMarshal.GetReference(newSpan);

        if (index != lastIndex)
        {
            int length = lastIndex - index;
            ReadOnlySpan<T> src = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref data, index), length);
            Span<T> dst = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref data, index + 1), length);
            src.CopyTo(dst);
        }

        Unsafe.Add(ref data, index) = item;
        span = newSpan;
    }

    /// <summary>
    /// Removes the element at the specified index in the list.
    /// </summary>
    /// <param name="index">The zero-based index of the element to remove.</param>
    /// <exception cref="ObjectDisposedException">Thrown when the list has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is out of range.</exception>
    public void RemoveAt(int index)
    {
        ObjectDisposedException.ThrowIf(array is null, typeof(PooledList<T>));
        ThrowIfIndexOutOfRange(index);
        RemoveAtUnsafe(index);
    }

    /// <summary>
    /// Removes all elements from the list.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the list has been disposed.</exception>
    public void Clear()
    {
        ObjectDisposedException.ThrowIf(array is null, typeof(PooledList<T>));
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            span.Clear();
        }
        GetLengthRefUnsafe(ref span) = 0;
    }

    /// <summary>
    /// Attempts to remove and return the last element of the list.
    /// </summary>
    /// <param name="item">
    /// When this method returns, contains the removed element if the list is not empty; 
    /// otherwise, the default value for the type.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if an element was removed and returned; 
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ObjectDisposedException">Thrown when the list has been disposed.</exception>
    public bool TryTake([MaybeNullWhen(false)] out T item)
    {
        ObjectDisposedException.ThrowIf(array is null, typeof(PooledList<T>));

        if (span.Length == 0)
        {
            item = default;
            return false;
        }

        ref T itemRef = ref Unsafe.Add(ref MemoryMarshal.GetReference(span), --GetLengthRefUnsafe(ref span));
        item = itemRef!;
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            itemRef = default!;
        }

        return true;
    }

    /// <summary>
    /// Removes and returns the last element of the list.
    /// </summary>
    /// <returns>The last element of the list.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the list has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the list is empty.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Take()
    {
        if (!TryTake(out T? item))
        {
            ListHasNoElements();
        }

        return item!;
    }

    internal void RemoveAtUnsafe(int index)
    {
        int newCount = span.Length - 1;
        ref T data = ref MemoryMarshal.GetReference(span);

        if (index != newCount)
        {
            int length = newCount - index;
            ReadOnlySpan<T> src = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref data, index + 1), length);
            Span<T> dst = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref data, index), length);
            src.CopyTo(dst);
        }

        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            Unsafe.Add(ref data, newCount) = default!;
        }

        GetLengthRefUnsafe(ref span) = newCount;
    }

    /// <summary>
    /// Creates a <see cref="Span{T}"/> over the elements in the list.
    /// </summary>
    /// <remarks>Do not modify the list while using the span.</remarks>
    /// <returns>A span representing the elements in the list.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the list has been disposed.</exception>
    public readonly Span<T> AsSpan()
    {
        ObjectDisposedException.ThrowIf(array is null, typeof(PooledList<T>));
        return span;
    }

    /// <inheritdoc cref="AsSpan(int, int)"/>
    public readonly Span<T> AsSpan(int index)
    {
        ObjectDisposedException.ThrowIf(array is null, typeof(PooledList<T>));
        return span[index..];
    }

    /// <summary>
    /// Creates a <see cref="Span{T}"/> over a range of elements in the list.
    /// </summary>
    /// <remarks>Do not modify the list while using the span.</remarks>
    /// <param name="index">The zero-based starting index of the range.</param>
    /// <param name="length">The number of elements in the range.</param>
    /// <returns>A span representing the specified range of elements in the list.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the list has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index or length is out of range.</exception>
    public readonly Span<T> AsSpan(int index, int length)
    {
        ObjectDisposedException.ThrowIf(array is null, typeof(PooledList<T>));
        return span.Slice(index, length);
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public void Dispose()
    {
        if (array is { })
        {
            PooledArrayFactory.Return<T>(ref array);
        }

        array = null;
    }

    /// <summary>
    /// Copies the elements of the list to the specified destination span.
    /// </summary>
    /// <inheritdoc cref="Span{T}.CopyTo(Span{T})"/>
    /// <exception cref="ObjectDisposedException">Thrown when the list has been disposed.</exception>
    public readonly void CopyTo(Span<T> destination)
    {
        AsSpan().CopyTo(destination);
    }

    /// <summary>
    /// Copies the elements of the list to the specified array starting at the specified index.
    /// </summary>
    /// <param name="startIndex">The zero-based index in the destination array at which copying begins.</param>
    /// <param name="array">The destination array.</param>
    /// <exception cref="ObjectDisposedException">Thrown when the list has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the number of elements in the source list is greater than the available space 
    /// from the specified index to the end of the destination array.
    /// </exception>
    public readonly void CopyTo(int startIndex, T[] array)
    {
        ThrowHelpers.ThrowIfArrayIndexIsOutOfRange(startIndex, array, span.Length);
        AsSpan().CopyTo(array.AsSpan(startIndex));
    }

    /// <summary>
    /// Returns an enumerator that iterates through the elements of the list.
    /// </summary>
    /// <returns>An enumerator for the list.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the list has been disposed.</exception>
    public readonly Span<T>.Enumerator GetEnumerator()
    {
        return AsSpan().GetEnumerator();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void DisposeUnsafe()
    {
        array = null;
        span = default;
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_length")]
    private static extern ref int GetLengthRefUnsafe(ref Span<T> span);

    [StackTraceHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly void ThrowIfIndexOutOfRange(int index, [CallerArgumentExpression(nameof(index))] string? paramName = null)
    {
        if ((uint)index >= (uint)span.Length)
        {
            IndexWasOutOfRange(index, paramName);
        }
    }

    [DoesNotReturn, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
    private static void IndexWasOutOfRange(int index, string? paramName)
    {
        throw new ArgumentOutOfRangeException(paramName, index, "The index is outside the bounds of the collection.");
    }

    [DoesNotReturn, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
    private static void ListHasNoElements()
    {
        throw new InvalidOperationException("The list is empty.");
    }

    /// <summary>
    /// Defines an implicit conversion from a <see cref="PooledList{T}"/> to a <see cref="Span{T}"/>.
    /// </summary>
    /// <param name="value">The <see cref="PooledList{T}"/> to convert.</param>
    public static implicit operator Span<T>(PooledList<T> value) => value.AsSpan();
}

/// <summary>
/// Provides static methods for creating and manipulating <see cref="PooledList{T}"/> instances.
/// </summary>
public static class PooledList
{
    /// <inheritdoc cref="CollectorDelegate{TState}"/>
    public delegate void CollectorDelegate(ref PooledList<Task> list, CancellationToken cancellationToken);
    /// <summary>
    /// Collects Task instances into a pooled list.
    /// </summary>
    /// <typeparam name="TState">The type used to provide state.</typeparam>
    /// <param name="list">The pooled list of <see cref="Task"/> instances to add to.</param>
    /// <param name="state">A readonly reference to the state value.</param>
    /// <param name="cancellationToken">Cancellation token to observe.</param>
    public delegate void CollectorDelegate<TState>(ref PooledList<Task> list, in TState state, CancellationToken cancellationToken)
        where TState : allows ref struct;

    private const int DefaultInitialCapacity = 16;

    /// <summary>
    /// Creates an empty <see cref="PooledList{T}"/> with the specified initial capacity.
    /// </summary>
    /// <typeparam name="T">The type of elements stored in the list.</typeparam>
    /// <param name="initialCapacity">The initial number of elements that the list can contain.</param>
    /// <returns>A new instance of <see cref="PooledList{T}"/> with the specified initial capacity.</returns>
    public static PooledList<T> Empty<T>(int initialCapacity = DefaultInitialCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(initialCapacity);
        return new PooledList<T>(0, initialCapacity);
    }

    /// <summary>
    /// Creates a <see cref="PooledList{T}"/> with the specified number of elements.
    /// </summary>
    /// <typeparam name="T">The type of elements stored in the list.</typeparam>
    /// <param name="count">The number of elements in the list.</param>
    /// <returns>A new instance of <see cref="PooledList{T}"/> with the specified number of elements.</returns>
    public static PooledList<T> Init<T>(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        PooledList<T> result = new(count, count);
        if (!RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            result.Clear();
        }
        return result;
    }

    /// <summary>
    /// Creates a <see cref="PooledList{T}"/> with the specified number of elements while skipping initialization, if possible.
    /// </summary>
    /// <inheritdoc cref="Init{T}"/>
    public static PooledList<T> SkipInit<T>(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return new PooledList<T>(count, count);
    }

    /// <summary>
    /// Creates a <see cref="PooledList{T}"/> from the specified <see cref="List{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements stored in the list.</typeparam>
    /// <param name="source">The source list.</param>
    /// <returns>A new instance of <see cref="PooledList{T}"/> containing the elements of the source list.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PooledList<T> ToPooledList<T>(this List<T> source)
    {
        return ToPooledList(CollectionsMarshal.AsSpan(source));
    }

    /// <summary>
    /// Creates a <see cref="PooledList{T}"/> from the specified array.
    /// </summary>
    /// <typeparam name="T">The type of elements stored in the array.</typeparam>
    /// <param name="source">The source array.</param>
    /// <returns>A new instance of <see cref="PooledList{T}"/> containing the elements of the source array.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PooledList<T> ToPooledList<T>(this T[] source)
    {
        return ToPooledList(source.AsSpan());
    }

    /// <summary>
    /// Creates a <see cref="PooledList{T}"/> from the specified <see cref="ReadOnlyMemory{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements stored in the memory.</typeparam>
    /// <param name="source">The source memory.</param>
    /// <returns>A new instance of <see cref="PooledList{T}"/> containing the elements of the source memory.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PooledList<T> ToPooledList<T>(this ReadOnlyMemory<T> source)
    {
        return ToPooledList(source.Span);
    }

    /// <summary>
    /// Creates a <see cref="PooledList{T}"/> from the specified <see cref="ImmutableArray{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements stored in the immutable array.</typeparam>
    /// <param name="source">The source immutable array.</param>
    /// <returns>A new instance of <see cref="PooledList{T}"/> containing the elements of the source immutable array.</returns> 
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PooledList<T> ToPooledList<T>(this ImmutableArray<T> source)
    {
        return ToPooledList(source.AsSpan());
    }

    /// <summary>
    /// Creates a <see cref="PooledList{T}"/> from the specified <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements stored in the span.</typeparam>
    /// <param name="source">The source span.</param>
    /// <returns>A new instance of <see cref="PooledList{T}"/> containing the elements of the source span.</returns>
    public static PooledList<T> ToPooledList<T>(this ReadOnlySpan<T> source)
    {
        if (source.IsEmpty)
        {
            return new PooledList<T>(0, DefaultInitialCapacity);
        }

        int capacity = ComputeInitialCapacity(source.Length);
        PooledList<T> result = new(source.Length, capacity);
        source.CopyTo(result.AsSpan());
        return result;
    }

    /// <summary>
    /// Creates a <see cref="PooledList{T}"/> from the specified <see cref="IEnumerable{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements stored in the enumerable.</typeparam>
    /// <param name="source">The source enumerable.</param>
    /// <returns>A new instance of <see cref="PooledList{T}"/> containing the elements of the source enumerable.</returns>
    public static PooledList<T> ToPooledList<T>(this IEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source.TryGetNonEnumeratedCount(out int count))
        {
            return CreateFromEnumerableWithCount(source, count);
        }

        return CreateFromEnumerableWithoutCount(source);
    }

    /// <inheritdoc cref="WhenAll{TState}(CollectorDelegate{TState}, in TState, int, CancellationToken)"/>
    public static Task WhenAll(CollectorDelegate collector, int initialCapacity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(collector);
        PooledList<Task> tasks = Empty<Task>(initialCapacity);
        try
        {
            collector(ref tasks, cancellationToken);
            return WhenAll(ref tasks);
        }
        catch (Exception ex)
        {
            return WhenAll(Task.FromException(ex), WhenAll(ref tasks));
        }
    }

    /// <inheritdoc cref="WhenAll{TState}(CollectorDelegate{TState}, in TState, int, CancellationToken)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task WhenAll(CollectorDelegate collector, CancellationToken cancellationToken = default)
    {
        return WhenAll(collector, DefaultInitialCapacity, cancellationToken);
    }

    /// <summary>
    /// Invokes the collector to populate a pooled list of tasks.
    /// </summary>
    /// <typeparam name="TState">The type of state to pass to the <paramref name="collector"/>.</typeparam>
    /// <param name="collector">The method used to populate the pooled list of tasks.</param>
    /// <param name="state">A state parameter to pass to <paramref name="collector"/>.</param>
    /// <param name="initialCapacity">Initial capacity for the pooled task list used to collect tasks.</param>
    /// <param name="cancellationToken">
    /// Cancellation token passed to the <paramref name="collector"/> for cooperative cancellation.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> that represents the combined completion of all tasks added by the 
    /// <paramref name="collector"/>.
    /// </returns>
    public static Task WhenAll<TState>(CollectorDelegate<TState> collector, in TState state, 
        int initialCapacity = DefaultInitialCapacity, 
        CancellationToken cancellationToken = default)
        where TState : allows ref struct
    {
        ArgumentNullException.ThrowIfNull(collector);
        PooledList<Task> tasks = Empty<Task>(initialCapacity);
        try
        {
            collector(ref tasks, in state, cancellationToken);
            return WhenAll(ref tasks);
        }
        catch (Exception ex)
        {
            return WhenAll(Task.FromException(ex), WhenAll(ref tasks));
        }
    }

    /// <inheritdoc cref="WhenAll{TState}(CollectorDelegate{TState}, in TState, int, CancellationToken)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task WhenAll<TState>(CollectorDelegate<TState> collector, in TState state, CancellationToken cancellationToken = default)
        where TState : allows ref struct
    {
        return WhenAll(collector, in state, DefaultInitialCapacity, cancellationToken);
    }

    /// <summary>
    /// Creates a <see cref="Task"/> that completes when both specified tasks have completed.
    /// </summary>
    /// <param name="task1">The first <see cref="Task"/> to wait for.</param>
    /// <param name="task2">The second <see cref="Task"/> to wait for.</param>
    /// <returns>A <see cref="Task"/> that completes when both specified tasks have completed.</returns>
    public static Task WhenAll(Task task1, Task task2)
    {
        PooledList<Task> tasks = SkipInit<Task>(2);
        try
        {
            tasks[0] = task1;
            tasks[1] = task2;
            return WhenAll(ref tasks);
        }
        catch
        {
            tasks.Dispose();
            throw;
        }
    }

    private static PooledList<T> CreateFromEnumerableWithCount<T>(IEnumerable<T> source, int count)
    {
        int capacity = ComputeInitialCapacity(count);
        PooledList<T> result = new(count, capacity);

        try
        {
            ref T data = ref MemoryMarshal.GetReference(result.span);
            foreach (T item in source)
            {
                data = item;
                data = ref Unsafe.Add(ref data, 1);
            }
        }
        catch
        {
            result.Dispose();
            throw;
        }

        return result;
    }

    private static PooledList<T> CreateFromEnumerableWithoutCount<T>(IEnumerable<T> source)
    {
        PooledList<T> result = new(0, DefaultInitialCapacity);

        try
        {
            foreach (T item in source)
            {
                result.Add(item);
            }
        }
        catch
        {
            result.Dispose();
            throw;
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ComputeInitialCapacity(int count)
    {
        return Math.Max(count + (count >> 1), DefaultInitialCapacity);
    }

    internal static Task WhenAll(ref PooledList<Task> tasks)
    {
        if (tasks.array is null)
        {
            return Task.CompletedTask;
        }

        Task wait = Task.WhenAll(tasks);
        Task continuation = wait.ContinueWith(
            Unsafe.As<Action<Task, object?>>((Action<Task, object[]>)ReturnArrayAndPropagateException),
            tasks.array,
            TaskContinuationOptions.ExecuteSynchronously);
        tasks.DisposeUnsafe();
        return continuation;
    }

    private static void ReturnArrayAndPropagateException(Task completedTask, object[] array)
    {
        ArrayPool<object>.Shared.Return(array, clearArray: true);
        completedTask.GetAwaiter().GetResult();
    }
}
