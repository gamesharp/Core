using GameSharp.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace GameSharp.Collections.Immutable;

/// <summary>
/// Provides extension methods for converting between <see cref="TypeLookup"/>, <see cref="ImmutableTypeLookup"/>, 
/// <see cref="ConcurrentTypeLookup"/>, and <see cref="ImmutableTypeLookup.Builder"/> representations.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Creates an <see cref="ImmutableTypeLookup.Builder"/> initialized with the contents of the specified immutable
    /// lookup.
    /// </summary>
    /// <param name="values">The immutable lookup whose entries are used to initialize the builder.</param>
    /// <returns>
    /// A builder containing the entries in <paramref name="values"/>, or an empty builder when 
    /// <paramref name="values"/> is empty.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> is <see langword="null"/>.</exception>
    public static ImmutableTypeLookup.Builder AsImmutableBuilder(this ImmutableTypeLookup values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return values.Count == 0 ? [] : new ImmutableTypeLookup.Builder(values.lookups[..values.lookupCount], values.items[..values.itemCount]);
    }
    
    /// <summary>
    /// Creates an <see cref="ImmutableTypeLookup"/> initialized with the contents of the specified type lookup.
    /// </summary>
    /// <param name="values">The type lookup whose entries are used to initialize the immutable lookup.</param>
    /// <returns>
    /// An immutable lookup containing the entries in <paramref name="values"/>, or an empty immutable lookup when 
    /// <paramref name="values"/> is empty.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> is <see langword="null"/>.</exception>
    public static ImmutableTypeLookup AsImmutableTypeLookup(this TypeLookup values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return values.Count == 0 ? [] : new ImmutableTypeLookup(values.lookups[..values.lookupCount], values.items[..values.itemCount]);
    }

    /// <summary>
    /// Creates a <see cref="TypeLookup"/> initialized with the contents of the specified immutable lookup.
    /// </summary> 
    /// <param name="values">The immutable lookup whose entries are used to initialize the type lookup.</param>
    /// <returns>
    /// A type lookup containing the entries in <paramref name="values"/>, or an empty type lookup when 
    /// <paramref name="values"/> is empty.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> is <see langword="null"/>.</exception>
    public static TypeLookup AsTypeLookup(this ImmutableTypeLookup values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return values.Count == 0 ? [] : new TypeLookup(values.lookups[..values.lookupCount], values.items[..values.itemCount]);
    }

    /// <summary>
    /// Creates an <see cref="ImmutableTypeLookup"/> initialized with the contents of the specified concurrent lookup.
    /// </summary> 
    /// <param name="values">The concurrent lookup whose entries are used to initialize the immutable lookup.</param>
    /// <returns>
    /// An immutable lookup containing the entries in <paramref name="values"/>, or an empty immutable lookup when 
    /// <paramref name="values"/> is empty.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> is <see langword="null"/>.</exception>
    public static ImmutableTypeLookup AsImmutableTypeLookup(this ConcurrentTypeLookup values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return values.syncRoot.TryEnterReadLock() is { IsHeld: true } handle ? LockedClone(values, handle) : Volatile.Read(ref values.lookup);
    }
    
    /// <summary>
    /// Creates a <see cref="ConcurrentTypeLookup"/> initialized with the contents of the specified immutable lookup.
    /// </summary> 
    /// <param name="values">The immutable lookup whose entries are used to initialize the concurrent lookup.</param>
    /// <returns>
    /// A concurrent lookup containing the entries in <paramref name="values"/>, or an empty concurrent lookup when 
    /// <paramref name="values"/> is empty.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> is <see langword="null"/>.</exception>
    public static ConcurrentTypeLookup AsConcurrentTypeLookup(this ImmutableTypeLookup values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return values.Count == 0 ? [] : new ConcurrentTypeLookup(values.Clone());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ImmutableTypeLookup LockedClone(ConcurrentTypeLookup values, ReadLockHandle handle)
    {
        using (handle)
        {
            return values.lookup.Clone();
        }
    }
}
