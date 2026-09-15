using System.Buffers;
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
    /// <inheritdoc cref="IEnumerator{T}"/>
    public ref struct Enumerator
    {
        private ReadOnlySpan<T>.Enumerator _enumerator;
        private readonly ref readonly int _versionRef;
        private readonly int _version;

        /// <inheritdoc cref="IEnumerator{T}.Current"/>
        public T Current => _enumerator.Current;

        internal Enumerator(ref readonly PooledList<T> list)
        {
            _enumerator = list.AsSpan().GetEnumerator();
            _versionRef = ref list._version;
            _version = list._version;
        }

        /// <inheritdoc cref="System.Collections.IEnumerator.MoveNext()"/>
        /// <exception cref="InvalidOperationException">Thrown when the collection was modified during enumeration.</exception>
        public bool MoveNext()
        {
            if (_version != _versionRef)
            {
                ThrowVersionMismatchException();
            }

            return _enumerator.MoveNext();
        }

        [DoesNotReturn, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowVersionMismatchException()
        {
            throw new InvalidOperationException("The collection was modified during enumeration.");
        }
    }

    private Array? _array;
    private ref T? _dataRef;
    private int _version;

    /// <summary>
    /// Gets the number of elements contained in the list.
    /// </summary>
    public int Count { get; private set; }

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
            return Unsafe.Add(ref _dataRef!, index);
        }
        set
        {
            ObjectDisposedException.ThrowIf(_array is null, typeof(PooledList<T>));
            ThrowIfIndexOutOfRange(index);
            Unsafe.Add(ref _dataRef, index) = value;
            _version++;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PooledList{T}"/> struct with the specified initial capacity.
    /// </summary>
    /// <param name="initialCapacity">The initial number of elements that the list can contain.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="initialCapacity"/> is negative.</exception>
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
        Count = 0;
        _version = int.MinValue;
    }

    /// <summary>
    /// Adds an element to the end of the list.
    /// </summary>
    /// <param name="item">The element to add.</param>
    /// <exception cref="ObjectDisposedException">Thrown when the list has been disposed.</exception>
    public void Add(T item)
    {
        ObjectDisposedException.ThrowIf(_array is null, typeof(PooledList<T>));
        int index = Count, newCount = index + 1;
        EnsureCapacity(newCount);
        Unsafe.Add(ref _dataRef, index) = item;
        Count = newCount;
        _version++;
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
        if ((uint)index > (uint)Count)
        {
            IndexWasOutOfRange(index, nameof(index));
        }

        int lastIndex = Count, newCount = lastIndex + 1;
        EnsureCapacity(newCount);

        if (index != lastIndex)
        {
            int length = lastIndex - index;
            ReadOnlySpan<T?> src = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref _dataRef, index), length);
            Span<T?> dst = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref _dataRef, index + 1), length);
            src.CopyTo(dst);
        }

        Unsafe.Add(ref _dataRef, index) = item;
        Count = newCount;
        _version++;
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
            Span<T?> span = MemoryMarshal.CreateSpan(ref _dataRef, Count);
            span.Clear();
        }
        Count = 0;
        _version++;
    }

    internal void RemoveAtUnsafe(int index)
    {
        int newCount = Count - 1;

        if (index != newCount)
        {
            int length = newCount - index;
            ReadOnlySpan<T?> src = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref _dataRef, index + 1), length);
            Span<T?> dst = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref _dataRef, index), length);
            src.CopyTo(dst);
        }

        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            Unsafe.Add(ref _dataRef, newCount) = default!;
        }

        Count = newCount;
        _version++;
    }

    /// <summary>
    /// Creates a <see cref="ReadOnlySpan{T}"/> over the elements in the list.
    /// </summary>
    /// <remarks>Do not modify the list while using the span.</remarks>
    /// <returns>A span representing the elements in the list.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the list has been disposed.</exception>
    public readonly ReadOnlySpan<T> AsSpan()
    {
        ObjectDisposedException.ThrowIf(_array is null, typeof(PooledList<T>));
        return MemoryMarshal.CreateReadOnlySpan(ref _dataRef!, Count);
    }

    /// <inheritdoc cref="AsSpan(int, int)"/>
    public readonly ReadOnlySpan<T> AsSpan(int index)
    {
        ObjectDisposedException.ThrowIf(_array is null, typeof(PooledList<T>));
        if ((uint)index > (uint)Count)
        {
            IndexWasOutOfRange(index, nameof(index));
        }
        return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref _dataRef!, index), Count - index);
    }

    /// <summary>
    /// Creates a <see cref="ReadOnlySpan{T}"/> over a range of elements in the list.
    /// </summary>
    /// <remarks>Do not modify the list while using the span.</remarks>
    /// <param name="index">The zero-based starting index of the range.</param>
    /// <param name="length">The number of elements in the range.</param>
    /// <returns>A span representing the specified range of elements in the list.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the list has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index or length is out of range.</exception>
    public readonly ReadOnlySpan<T> AsSpan(int index, int length)
    {
        ObjectDisposedException.ThrowIf(_array is null, typeof(PooledList<T>));
        if ((uint)index > (uint)Count || (uint)length > (uint)(Count - index))
        {
            IndexWasOutOfRange(length, nameof(length));
        }
        return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref _dataRef!, index), length);
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public void Dispose()
    {
        if (_array is { })
        {
            Return(_array);
            _dataRef = ref Unsafe.NullRef<T?>();
        }

        _array = null;
    }

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    /// <exception cref="ObjectDisposedException">Thrown when the list has been disposed.</exception>
    [UnscopedRef]
    public readonly Enumerator GetEnumerator()
    {
        return new Enumerator(in this);
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
        ReadOnlySpan<T?> src = MemoryMarshal.CreateReadOnlySpan(ref _dataRef, Count);
        Rent(capacity);
        Span<T?> dst = MemoryMarshal.CreateSpan(ref _dataRef, Count);
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

        _dataRef = ref MemoryMarshal.GetArrayDataReference(itemArray)!;
    }

    private void RentByteArray(int minimumLength)
    {
        byte[] byteArray = ArrayPool<byte>.Shared.Rent(minimumLength);
        _array = byteArray;

        ref byte byteRef = ref MemoryMarshal.GetArrayDataReference(byteArray);
        _dataRef = ref Unsafe.As<byte, T>(ref byteRef)!;
    }

    private void RentObjectArray(int minimumLength)
    {
        object[] objArray = ArrayPool<object>.Shared.Rent(minimumLength);
        _array = objArray;

        ref object objRef = ref MemoryMarshal.GetArrayDataReference(objArray);
        _dataRef = ref Unsafe.As<object, T>(ref objRef)!;
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
        if ((uint)index >= (uint)Count)
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
}
