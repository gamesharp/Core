using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GameSharp.Collections;

internal abstract class ArrayFactory
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EnsureCapacity<T>(ref T[] array, int count)
    {
        Span<T> span = default;
        EnsureCapacity(ref Unsafe.As<T[], Array>(ref array), ref span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EnsureCapacity<T>(ref Array array, ref Span<T> span)
    {
        if (span.Length > array.Length)
        {
            Grow(ref array, ref span);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetCount<T>(ref T[] array, int count)
    {
        Span<T> span = new(array, 0, count);
        SetCount(ref Unsafe.As<T[], Array>(ref array), ref span);
    }

    public void SetCount<T>(ref Array array, ref Span<T> span)
    {
        if (span.Length > array.Length)
        {
            Grow(ref array, ref span);
        }
        else if (RuntimeHelpers.IsReferenceOrContainsReferences<T>() && span.Length < array.Length)
        {
            Span<T> tail = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref MemoryMarshal.GetReference(span), span.Length), array.Length - span.Length);
            tail.Clear();
        }
    }
    
    public abstract Span<T> Get<T>(out Array array, int length, int capacity);

    protected abstract void Resize<T>(ref Array array, ref Span<T> span, int newSize);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static ref T ReinterpretArrayDataRef<T>(Array array)
    {
        return ref Unsafe.As<byte, T>(ref MemoryMarshal.GetArrayDataReference(array));
    }

    private void Grow<T>(ref Array array, ref Span<T> span)
    {
        int capacity = Math.Max(array.Length, 2);
        do
        {
            capacity += capacity >> 1;
        }
        while (capacity < span.Length);
        Resize(ref array, ref span, capacity);
    }
}
