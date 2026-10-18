using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GameSharp.Collections;

internal abstract class ArrayFactory
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T EnsureCapacity<T>(ref T[] array, int count)
    {
        ref T data = ref Unsafe.NullRef<T>();
        EnsureCapacity(ref Unsafe.As<T[], Array>(ref array), ref data, count);
        return ref data;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EnsureCapacity<T>(ref Array array, ref T data, int count)
    {
        if (count > array.Length)
        {
            data = ref Grow<T>(ref array, count);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T SetCount<T>(ref T[] array, int count)
    {
        ref T data = ref MemoryMarshal.GetArrayDataReference(array);
        SetCount(ref Unsafe.As<T[], Array>(ref array), ref data, count);
        return ref data;
    }

    public void SetCount<T>(ref Array array, ref T data, int count)
    {
        if (count > array.Length)
        {
            data = ref Grow<T>(ref array, count);
        }
        else if (RuntimeHelpers.IsReferenceOrContainsReferences<T>() && count < array.Length)
        {
            Span<T> tail = MemoryMarshal.CreateSpan(ref Unsafe.Add(ref data, count), array.Length - count);
            tail.Clear();
        }
    }

    public abstract ref T Get<T>(out Array array, int minimumLength);

    protected abstract ref T Resize<T>(ref Array array, int count, int newSize);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static ref T ReinterpretArrayDataRef<T>(Array array)
    {
        return ref Unsafe.As<byte, T>(ref MemoryMarshal.GetArrayDataReference(array));
    }

    private ref T Grow<T>(ref Array array, int count)
    {
        int capacity = Math.Max(array.Length, 2);
        do
        {
            capacity += capacity >> 1;
        }
        while (capacity < count);
        return ref Resize<T>(ref array, count, capacity);
    }
}
