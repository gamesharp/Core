using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace GameSharp.Collections;

/// <summary>
/// Provides extension methods for the <see cref="PooledList{T}"/> type.
/// </summary>
public static class PooledListExtensions
{
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
}
