using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GameSharp.Collections;

internal sealed class PooledArrayFactory : ArrayFactory
{
    public static PooledArrayFactory Default { get; } = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override ref T Get<T>(out Array array, int minimumLength)
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            if (typeof(T).IsValueType)
            {
                array = RentFromShared<T>(minimumLength);
            }
            else
            {
                array = RentFromShared<object>(minimumLength);
            }
        }
        else
        {
            array = RentFromShared<byte>(ComputeByteLength<T>(minimumLength));
        }

        return ref ReinterpretArrayDataRef<T>(array);
    }

    protected override ref T Resize<T>(ref Array array, int count, int newSize)
    {
        ref T data = ref Get<T>(out Array newArray, newSize);

        try
        {
            ReadOnlySpan<T> src = MemoryMarshal.CreateReadOnlySpan(ref ReinterpretArrayDataRef<T>(array), count);
            Span<T> dst = MemoryMarshal.CreateSpan(ref data, newSize);
            src.CopyTo(dst);
            return ref data;
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
