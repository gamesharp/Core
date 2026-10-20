using GameSharp.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace GameSharp.Collections.Concurrent;

/// <summary>
/// Provides extension methods for converting between <see cref="TypeLookup"/> and 
/// <see cref="ConcurrentTypeLookup"/> representations.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Creates a <see cref="TypeLookup"/> initialized with the contents of the specified concurrent
    /// lookup.
    /// </summary>
    /// <param name="values">The concurrent lookup whose entries are used to initialize the type lookup.</param>
    /// <returns>
    /// A type lookup containing the entries in <paramref name="values"/>, or an empty type lookup when 
    /// <paramref name="values"/> is empty.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> is <see langword="null"/>.</exception>
    public static TypeLookup AsTypeLookup(this ConcurrentTypeLookup values)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (values.syncRoot.TryEnterReadLock() is { IsHeld: true } handle)
        {
            return AsTypeLookupLocked(values, handle);
        }

        return values.lookup.AsTypeLookup();
    }
    
    /// <summary>
    /// Creates a <see cref="ConcurrentTypeLookup"/> initialized with the contents of the specified type lookup.
    /// </summary>
    /// <param name="values">The type lookup whose entries are used to initialize the concurrent lookup.</param>
    /// <returns>
    /// A concurrent lookup containing the entries in <paramref name="values"/>, or an empty concurrent lookup when 
    /// <paramref name="values"/> is empty.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> is <see langword="null"/>.</exception>
    public static ConcurrentTypeLookup AsConcurrentTypeLookup(this TypeLookup values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return new ConcurrentTypeLookup(values.AsImmutableTypeLookup());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static TypeLookup AsTypeLookupLocked(ConcurrentTypeLookup values, ReadLockHandle handle)
    {
        using (handle)
        {
            return values.lookup.AsTypeLookup();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ImmutableTypeLookup AsImmutableTypeLookup(this TypeLookup values)
    {
        return values.Count == 0 ? [] : new ImmutableTypeLookup(values.lookups[..values.lookupCount], values.items[..values.itemCount]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TypeLookup AsTypeLookup(this ImmutableTypeLookup values)
    {
        return values.Count == 0 ? [] : new TypeLookup(values.lookups[..values.lookupCount], values.items[..values.itemCount]);
    }
}
