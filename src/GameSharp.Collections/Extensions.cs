using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GameSharp.Collections;

/// <summary>
/// Provides extension methods for the <see cref="PooledList{T}"/> class.
/// </summary>
public static class Extensions
{
    private static readonly int _l1ExclSize = ProcessorInfo.Default.LineCacheSizeInBytes - 1;

    /// <inheritdoc cref="Contains{T}(ref PooledList{T}, T, IEqualityComparer{T})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains<T>(this scoped ref PooledList<T> list, T? item) where T : IEquatable<T?>
    {
        return list.IndexOf(item) != -1;
    }

    /// <summary>
    /// Determines whether the <see cref="PooledList{T}"/> contains a specific value.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="list">The list in which to search for the item.</param>
    /// <param name="item">The item to locate in the list.</param>
    /// <param name="comparer">An optional equality comparer to use for comparing items.</param>
    /// <returns><see langword="true"/> if the item is found in the list; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains<T>(this scoped ref PooledList<T> list, T? item, IEqualityComparer<T?>? comparer = null)
    {
        return list.IndexOf(item, comparer) != -1;
    }

    /// <inheritdoc cref="Remove{T}(ref PooledList{T}, T, IEqualityComparer{T}?)"/>
    public static bool Remove<T>(this scoped ref PooledList<T> list, [NotNullWhen(true)] T? item) where T : IEquatable<T?>
    {
        int index = list.IndexOf(item);

        if (index != -1)
        {
            list.RemoveAtUnsafe(index);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Removes the first occurrence of a specific object from the <see cref="PooledList{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="list">The list from which to remove the item.</param>
    /// <param name="item">The item to remove.</param>
    /// <param name="comparer">An optional equality comparer to use for comparing items.</param>
    /// <returns><see langword="true"/> if the item was successfully removed; otherwise, <see langword="false"/>.</returns>
    /// <inheritdoc cref="IndexOf{T}(ref readonly PooledList{T}, T, IEqualityComparer{T}?)"/>
    public static bool Remove<T>(this scoped ref PooledList<T> list, [NotNullWhen(true)] T? item, IEqualityComparer<T?>? comparer = null)
    {
        int index = list.IndexOf(item, comparer);

        if (index != -1)
        {
            list.RemoveAtUnsafe(index);
            return true;
        }

        return false;
    }

    /// <inheritdoc cref="IndexOf{T}(ref readonly PooledList{T}, T, IEqualityComparer{T}?)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOf<T>(this scoped ref readonly PooledList<T> list, T? item) where T : IEquatable<T?>
    {
        ReadOnlySpan<T> span = list.AsSpan();
        return span.IndexOf(item);
    }

    /// <summary>
    /// Finds the index of the first occurrence of a specific object in the <see cref="PooledList{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="list">The list in which to search for the item.</param>
    /// <param name="item">The item to locate in the list.</param>
    /// <param name="comparer">An optional equality comparer to use for comparing items.</param>
    /// <returns>The zero-based index of the first occurrence of the item if found; otherwise, -1.</returns>
    /// <inheritdoc cref="PooledList{T}.GetEnumerator()"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOf<T>(this scoped ref readonly PooledList<T> list, T? item, IEqualityComparer<T?>? comparer = null)
    {
        ReadOnlySpan<T> span = list.AsSpan();
        return span.IndexOf(item, comparer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe ref TItem HybridSearch<TItem, TComparand>(ref TItem low, ref TItem high, TComparand value, out bool exists)
        where TItem : unmanaged, IComparable<TComparand>
    {
        int divisor = sizeof(TItem) * 2;

        for (nint byteOffset; (byteOffset = Unsafe.ByteOffset(ref low, ref high)) > _l1ExclSize;)
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

    internal static ref TItem HybridSearch<TItem, TComparand>(this ReadOnlySpan<TItem> items, TComparand value, out int byteOffset, out bool exists)
        where TItem : unmanaged, IComparable<TComparand>
    {
        ref TItem data = ref MemoryMarshal.GetReference(items), low = ref data, high = ref Unsafe.Add(ref data, items.Length - 1);
        ref TItem result = ref HybridSearch(ref low, ref high, value, out exists);
        byteOffset = (int)Unsafe.ByteOffset(in data, in result);
        return ref result;
    }
}
