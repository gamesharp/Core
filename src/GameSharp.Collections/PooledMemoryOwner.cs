using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GameSharp.Collections;

internal class PooledMemoryOwner<T>([DisallowNull] Array? array, int length) : MemoryManager<T>, IMemoryOwner<T>
{
    private static readonly int _sizeInBytes;
    private readonly Lock _syncRoot = new();
    private GCHandle _handle;
    private int _refCount;
    private bool _disposalRequested;

    static PooledMemoryOwner()
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            return;
        }

        _sizeInBytes = Unsafe.SizeOf<T>();
    }

    public override Span<T> GetSpan()
    {
        ObjectDisposedException.ThrowIf(_disposalRequested, this);
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            if (typeof(T).IsValueType)
            {
                return Reinterpret<T>(array!, length);
            }
            else
            {
                return Reinterpret<object>(array!, length);
            }
        }
        else
        {   
            return Reinterpret<byte>(array!, length);
        }
    }

    public override MemoryHandle Pin(int elementIndex = 0)
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            throw new NotSupportedException();
        }
        ObjectDisposedException.ThrowIf(_disposalRequested, this);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)elementIndex, (uint)length);

        lock (_syncRoot)
        {
            ObjectDisposedException.ThrowIf(_disposalRequested, this);
            if (_refCount++ == 0)
            {
                _handle = GCHandle.Alloc(array!, GCHandleType.Pinned);
            }
        }

        unsafe
        {
            return new MemoryHandle((void*)(_handle.AddrOfPinnedObject() + (elementIndex * _sizeInBytes)), default, this);
        }
    }

    public override void Unpin()
    {
        ObjectDisposedException.ThrowIf(array is null, this);

        lock (_syncRoot)
        {
            ObjectDisposedException.ThrowIf(array is null, this);

            if (--_refCount != 0)
            {
                return;
            }

            _handle.Free();
            _handle = default;

            if (_disposalRequested)
            {
                ReturnToPool();
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (array is null)
        {
            return;
        }

        if (!disposing)
        {
            ReturnToPool();
            return;
        }

        lock (_syncRoot)
        {
            if (array is null)
            {
                return;
            }

            _disposalRequested = true;

            if (_refCount != 0)
            {
                ReturnToPool();
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ReturnToPool()
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            if (typeof(T).IsValueType)
            {
                ArrayPool<T>.Shared.Return(Unsafe.As<T[]>(array!), clearArray: true);
            }
            else
            {
                ArrayPool<object>.Shared.Return(Unsafe.As<object[]>(array!), clearArray: true);
            }
        }
        else
        {
            ArrayPool<byte>.Shared.Return(Unsafe.As<byte[]>(array!), clearArray: false);
        }

        array = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Span<T> Reinterpret<TFrom>(Array array, int length)
    {
        ref T data = ref Unsafe.As<TFrom, T>(ref MemoryMarshal.GetArrayDataReference(Unsafe.As<TFrom[]>(array)));
        return MemoryMarshal.CreateSpan(ref data, length);
    }
}
