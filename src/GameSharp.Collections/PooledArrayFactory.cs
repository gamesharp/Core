using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GameSharp.Collections;

internal sealed class PooledArrayFactory : ArrayFactory
{
    public static PooledArrayFactory Default { get; } = new();

    public override Span<T> Get<T>(out Array array, int length, int capacity)
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            if (typeof(T).IsValueType)
            {
                array = RentFromShared<T>(capacity);
            }
            else
            {
                array = RentFromShared<object>(capacity);
            }
        }
        else
        {
            array = RentFromShared<byte>(ComputeByteLength<T>(capacity));
        }

        return MemoryMarshal.CreateSpan(ref ReinterpretArrayDataRef<T>(array), length);
    }

    protected override void Resize<T>(ref Array array, ref Span<T> span, int newSize)
    {
        Span<T> newSpan = Get<T>(out Array newArray, span.Length, newSize);
        try
        {
            span.CopyTo(newSpan);
            span = newSpan;
        }
        catch
        {
            Return<T>(ref newArray);
            throw;
        }
        finally
        {
            Return<T>(ref array);
            array = newArray;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Return<T>(ref Array array)
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            if (typeof(T).IsValueType)
            {
                ReturnToShared(Unsafe.As<Array, T[]>(ref array), clearArray: true);
            }
            else
            {
                ReturnToShared(Unsafe.As<Array, object[]>(ref array), clearArray: true);
            }
        }
        else
        {
            ReturnToShared(Unsafe.As<Array, byte[]>(ref array), clearArray: false);
        }

        array = ref Unsafe.NullRef<Array>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T[] RentFromShared<T>(int minimumLength)
    {
        return ArrayPool<T>.Shared.Rent(minimumLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ReturnToShared<T>(T[] array, bool clearArray)
    {
        ArrayPool<T>.Shared.Return(array, clearArray);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ComputeByteLength<T>(int elementCount)
    {
        long byteLength = (long)elementCount * Unsafe.SizeOf<T>();
        if (byteLength > int.MaxValue)
        {
            ThrowOutOfMemoryException();
        }
        return (int)byteLength;
    }

    [DoesNotReturn, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowOutOfMemoryException()
    {
        throw new OutOfMemoryException("The requested capacity exceeds the maximum supported array size.");
    }
}
