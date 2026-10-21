using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.System.SystemInformation;

namespace GameSharp.Collections;

/// <summary>
/// An unsafe class that provides extension methods for working with spans of unmanaged types.
/// </summary>
public static class UnsafeSpanExtensions
{
    /// <summary>
    /// Gets an estimate of the size of a cache line in bytes.
    /// </summary>
    public const int CacheLineSizeEstimate = 64;

    /// <summary>
    /// Gets the size of a cache line in bytes or <see cref="CacheLineSizeEstimate"/> if it cannot be determined.
    /// </summary>
    public static int CacheLineSize { get; }

    static UnsafeSpanExtensions()
    {
        if (OperatingSystem.IsWindows())
        {
            foreach (SYSTEM_LOGICAL_PROCESSOR_INFORMATION info in Kernel32.GetLogicalProcessorInformation())
            {
                if (info.Relationship != LOGICAL_PROCESSOR_RELATIONSHIP.RelationCache)
                {
                    continue;
                }

                CacheLineSize = info.Anonymous.Cache.LineSize;
                return;
            }
        }
        else if (OperatingSystem.IsLinux())
        {
            // _SC_LEVEL1_DCACHE_LINESIZE
            CacheLineSize = checked((int)LibC.SysConf(190));
            if (CacheLineSize != -1)
            {
                return;
            }
        }
        else if (OperatingSystem.IsAndroid())
        {
            // The same value as _SC_LEVEL1_DCACHE_LINESIZE on Linux.
            CacheLineSize = checked((int)LibC.SysConf(168));
            if (CacheLineSize != -1)
            {
                return;
            }
        }
        else if (OperatingSystem.IsMacOS()
            || OperatingSystem.IsIOS()
            || OperatingSystem.IsMacCatalyst()
            || OperatingSystem.IsTvOS()
            || OperatingSystem.IsFreeBSD())
        {
            CacheLineSize = checked((int)LibC.SysCtlByName<long>("hw.cachelinesize"));
            return;
        }

        CacheLineSize = CacheLineSizeEstimate;
    }

    internal static unsafe ref TItem HybridSearch<TItem, TComparand>(ref TItem low, ref TItem high, TComparand value, out bool exists)
        where TItem : unmanaged, IComparable<TComparand>
    {
        int divisor = sizeof(TItem) * 2;

        for (nint byteOffset; (byteOffset = Unsafe.ByteOffset(ref low, ref high)) > CacheLineSize;)
        {
            ref TItem mid = ref Unsafe.Add(ref low, byteOffset / divisor);
            switch (mid.CompareTo(value))
            {
                case 0: exists = true; return ref mid;
                case < 0: low = ref Unsafe.Add(ref mid, 1); break;
                default: high = ref Unsafe.Subtract(ref mid, 1); break;
            }
        }

        for (; Unsafe.IsAddressLessThanOrEqualTo(ref low, ref high); low = ref Unsafe.Add(ref low, 1))
        {
            switch (low.CompareTo(value))
            {
                case 0: exists = true; return ref low;
                case < 0: continue;
            }

            break;
        }

        exists = false;
        return ref low;
    }

    /// <summary>
    /// Attempts to find the specified value in a sorted span of items.
    /// </summary>
    /// <remarks>
    /// Performs a binary search until the search range is small enough to fit in a cache line, then performs a linear search.
    /// </remarks>
    /// <typeparam name="TItem">The type of elements in the span.</typeparam>
    /// <typeparam name="TComparand">The type of the value to compare.</typeparam>
    /// <param name="items">The sorted span of items to search.</param>
    /// <param name="value">The value to search for.</param>
    /// <param name="byteOffset">
    /// When this method returns, contains the byte offset of the item if it was found; 
    /// otherwise, the byte offset of the position where it would be inserted.
    /// </param>
    /// <param name="exists">
    /// When this method returns, contains <see langword="true"/> if the value was found; otherwise, <see langword="false"/>.
    /// </param>
    /// <returns>A reference to the item if it was found; otherwise, a reference to the position where it would be inserted.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref TItem GetValueRefOrInsertionPoint<TItem, TComparand>(Span<TItem> items, TComparand value, out int byteOffset, out bool exists)
        where TItem : unmanaged, IComparable<TComparand>
    {
        return ref GetValueRefOrInsertionPoint(ref MemoryMarshal.GetReference(items), items.Length, value, out byteOffset, out exists);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref TItem GetValueRefOrInsertionPoint<TItem, TComparand>(this ReadOnlySpan<TItem> items, TComparand value, out int byteOffset, out bool exists)
        where TItem : unmanaged, IComparable<TComparand>
    {
        return ref GetValueRefOrInsertionPoint(ref MemoryMarshal.GetReference(items), items.Length, value, out byteOffset, out exists);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref TItem GetValueRefOrInsertionPoint<TItem, TComparand>(ref TItem data, int length, TComparand value, out int byteOffset, out bool exists)
        where TItem : unmanaged, IComparable<TComparand>
    {
        ref TItem low = ref data, high = ref Unsafe.Add(ref data, length - 1);
        ref TItem result = ref HybridSearch(ref low, ref high, value, out exists);
        byteOffset = (int)Unsafe.ByteOffset(in data, in result);
        return ref result;
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_length")]
    internal static extern ref int GetLengthRef<T>(this ref Span<T> span);
}
