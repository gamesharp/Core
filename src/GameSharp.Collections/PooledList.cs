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
    private Array? _array;
    internal ref T? data;
    private int _count;

    /// <summary>
    /// Gets the number of elements contained in the list.
    /// </summary>
    public int Count
    {
        readonly get => _count;
        set
        {
            if (value == _count)
            {
                return;
            } 

            ArgumentOutOfRangeException.ThrowIfNegative(value);

            if (value > _count)
            {
                EnsureCapacity(value);
            }
            else if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Span<T?> span = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref data, value), _count - value);
                span.Clear();
            }

            _count = value;
        }
    }

    /// <summary>
    /// Gets or sets the element at the specified index in the list.
    /// </summary>
    /// <param name="index">The zero-based index of the element to get or set.</param>
    /// <returns>The element at the specified index.</returns>
    public T this[int index]
    {
        get
        {
            ObjectDisposedException.ThrowIf(_array is null, typeof(PooledList<T>));
            ThrowIfIndexOutOfRange(index);
            return Unsafe.Add(ref data!, index);
        }
        set
        {
            ObjectDisposedException.ThrowIf(_array is null, typeof(PooledList<T>));
            ThrowIfIndexOutOfRange(index);
            Unsafe.Add(ref data, index) = value;
        }
    }

    internal PooledList(int count, int capacity)
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            Rent(capacity);
        }
        else
        {
            int byteLength = ComputeByteLength(capacity);
            Rent(byteLength);
        }
        _count = count;
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
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            Rent(initialCapacity);
        }
        else
        {
            int byteLength = ComputeByteLength(initialCapacity);
            Rent(byteLength);
        }
        _count = 0;
    }

    /// <summary>
    /// Adds an element to the end of the list.
    /// </summary>
    /// <param name="item">The element to add.</param>
    /// <exception cref="ObjectDisposedException">Thrown when the list has been disposed.</exception>
    public void Add(T item)
    {
        ObjectDisposedException.ThrowIf(_array is null, typeof(PooledList<T>));
        int index = _count, newCount = index + 1;
        EnsureCapacity(newCount);
        Unsafe.Add(ref data, index) = item;
        _count = newCount;
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
        ObjectDisposedException.ThrowIf(_array is null, typeof(PooledList<T>));
        if ((uint)index > (uint)_count)
        {
            IndexWasOutOfRange(index, nameof(index));
        }

        int lastIndex = _count, newCount = lastIndex + 1;
        EnsureCapacity(newCount);

        if (index != lastIndex)
        {
            int length = lastIndex - index;
            ReadOnlySpan<T?> src = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref data, index), length);
            Span<T?> dst = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref data, index + 1), length);
            src.CopyTo(dst);
        }

        Unsafe.Add(ref data, index) = item;
        _count = newCount;
    }

    /// <summary>
    /// Removes the element at the specified index in the list.
    /// </summary>
    /// <param name="index">The zero-based index of the element to remove.</param>
    /// <exception cref="ObjectDisposedException">Thrown when the list has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is out of range.</exception>
    public void RemoveAt(int index)
    {
        ObjectDisposedException.ThrowIf(_array is null, typeof(PooledList<T>));
        ThrowIfIndexOutOfRange(index);
        RemoveAtUnsafe(index);
    }

    /// <summary>
    /// Removes all elements from the list.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the list has been disposed.</exception>
    public void Clear()
    {
        ObjectDisposedException.ThrowIf(_array is null, typeof(PooledList<T>));
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            Span<T?> span = MemoryMarshal.CreateSpan(ref data, _count);
            span.Clear();
        }
        _count = 0;
    }

    internal void RemoveAtUnsafe(int index)
    {
        int newCount = _count - 1;

        if (index != newCount)
        {
            int length = newCount - index;
            ReadOnlySpan<T?> src = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref data, index + 1), length);
            Span<T?> dst = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref data, index), length);
            src.CopyTo(dst);
        }

        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            Unsafe.Add(ref data, newCount) = default!;
        }

        _count = newCount;
    }

    /// <summary>
    /// Creates a <see cref="Span{T}"/> over the elements in the list.
    /// </summary>
    /// <remarks>Do not modify the list while using the span.</remarks>
    /// <returns>A span representing the elements in the list.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the list has been disposed.</exception>
    public readonly Span<T> AsSpan()
    {
        ObjectDisposedException.ThrowIf(_array is null, typeof(PooledList<T>));
        return MemoryMarshal.CreateSpan(ref data!, _count);
    }

    /// <inheritdoc cref="AsSpan(int, int)"/>
    public readonly Span<T> AsSpan(int index)
    {
        ObjectDisposedException.ThrowIf(_array is null, typeof(PooledList<T>));
        if ((uint)index > (uint)_count)
        {
            IndexWasOutOfRange(index, nameof(index));
        }

        return MemoryMarshal.CreateSpan(ref Unsafe.Add(ref data!, index), _count - index);
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
        ObjectDisposedException.ThrowIf(_array is null, typeof(PooledList<T>));
        if ((uint)index > (uint)_count || (uint)length > (uint)(_count - index))
        {
            IndexWasOutOfRange(length, nameof(length));
        }

        return MemoryMarshal.CreateSpan(ref Unsafe.Add(ref data!, index), length);
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public void Dispose()
    {
        if (_array is { })
        {
            Return(_array);
            data = ref Unsafe.NullRef<T?>();
        }

        _array = null;
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
        ThrowHelpers.ThrowIfArrayIndexIsOutOfRange(startIndex, array, _count);
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
    private void EnsureCapacity(int minimumLength)
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            if (_array!.Length < minimumLength)
            {
                Grow(minimumLength);
            }
        }
        else
        {
            int byteLength = ComputeByteLength(minimumLength);
            if (_array!.Length < byteLength)
            {
                Grow(byteLength);
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Grow(int minimumLength)
    {
        int capacity = Math.Max(_array!.Length, 2);
        do
        {
            capacity += capacity >> 1;
        }
        while (capacity < minimumLength);

        Array oldArray = _array;
        ReadOnlySpan<T?> src = MemoryMarshal.CreateReadOnlySpan(ref data, _count);
        Rent(capacity);
        Span<T?> dst = MemoryMarshal.CreateSpan(ref data, _count);
        src.CopyTo(dst);

        Return(oldArray);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Rent(int minimumLength)
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            if (typeof(T).IsValueType)
            {
                RentTypedArray(minimumLength);
            }
            else
            {
                RentObjectArray(minimumLength);
            }
        }
        else
        {
            RentByteArray(minimumLength);
        }
    }

    private void RentTypedArray(int minimumLength)
    {
        T[] itemArray = ArrayPool<T>.Shared.Rent(minimumLength);
        _array = itemArray;

        data = ref MemoryMarshal.GetArrayDataReference(itemArray)!;
    }

    private void RentByteArray(int minimumLength)
    {
        byte[] byteArray = ArrayPool<byte>.Shared.Rent(minimumLength);
        _array = byteArray;

        ref byte byteRef = ref MemoryMarshal.GetArrayDataReference(byteArray);
        data = ref Unsafe.As<byte, T>(ref byteRef)!;
    }

    private void RentObjectArray(int minimumLength)
    {
        object[] objArray = ArrayPool<object>.Shared.Rent(minimumLength);
        _array = objArray;

        ref object objRef = ref MemoryMarshal.GetArrayDataReference(objArray);
        data = ref Unsafe.As<object, T>(ref objRef)!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Return(Array array)
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            if (typeof(T).IsValueType)
            {
                ArrayPool<T>.Shared.Return(Unsafe.As<T[]>(array), clearArray: true);
            }
            else
            {
                ArrayPool<object>.Shared.Return(Unsafe.As<object[]>(array), clearArray: true);
            }
        }
        else
        {
            ArrayPool<byte>.Shared.Return(Unsafe.As<byte[]>(array), clearArray: false);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ComputeByteLength(int elementCount)
    {
        long byteLength = (long)elementCount * Unsafe.SizeOf<T>();
        if (byteLength > int.MaxValue)
        {
            ThrowOutOfMemoryException();
        }
        return (int)byteLength;
    }

    [StackTraceHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly void ThrowIfIndexOutOfRange(int index, [CallerArgumentExpression(nameof(index))] string? paramName = null)
    {
        if ((uint)index >= (uint)_count)
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
    private static void ThrowOutOfMemoryException()
    {
        throw new OutOfMemoryException("The requested capacity exceeds the maximum allowed size.");
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

    private static PooledList<T> CreateFromEnumerableWithCount<T>(IEnumerable<T> source, int count)
    {
        int capacity = ComputeInitialCapacity(count);
        PooledList<T> result = new(count, capacity);

        try
        {
            ref T? data = ref result.data;
            foreach (T item in source)
            {
                data = item;
                data = ref Unsafe.Add<T?>(ref data, 1);
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
}
