using GameSharp.Collections.Concurrent;
using GameSharp.Collections.Immutable;

namespace GameSharp.Collections;

/// <summary>
/// Factory that creates query instances for a specific type.
/// </summary>
public readonly struct TypeLookupQueryFactory
{
    private readonly TypeInfo _typeInfo;

    internal TypeLookupQueryFactory(TypeInfo typeInfo)
    {
        _typeInfo = typeInfo;
    }

    /// <summary>
    /// Creates a <see cref="TypeLookup.Query"/> for the specified type lookup and the current type information.
    /// </summary>
    /// <param name="typeLookup">The <see cref="TypeLookup"/> used to create the query.</param>
    /// <returns>
    /// A <see cref="TypeLookup.Query"/> initialized with <paramref name="typeLookup"/> and the current type
    /// information.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="typeLookup"/> is <see langword="null"/>.</exception>
    public TypeLookup.Query Create(TypeLookup typeLookup)
    {
        ArgumentNullException.ThrowIfNull(typeLookup);
        return new TypeLookup.Query(typeLookup, _typeInfo);
    }

    /// <summary>
    /// Creates a <see cref="ImmutableTypeLookup.Query"/> for the specified type lookup and the current type information.
    /// </summary>
    /// <param name="typeLookup">The <see cref="ImmutableTypeLookup"/> used to create the query.</param>
    /// <returns>
    /// A <see cref="ImmutableTypeLookup.Query"/> initialized with <paramref name="typeLookup"/> and the current type
    /// information.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="typeLookup"/> is <see langword="null"/>.</exception>
    public ImmutableTypeLookup.Query Create(ImmutableTypeLookup typeLookup)
    {
        ArgumentNullException.ThrowIfNull(typeLookup);
        return new ImmutableTypeLookup.Query(typeLookup, _typeInfo);
    }

    /// <summary>
    /// Creates a <see cref="ConcurrentTypeLookup.Query"/> for the specified type lookup and the current type information.
    /// </summary>
    /// <param name="typeLookup">The <see cref="ConcurrentTypeLookup"/> used to create the query.</param>
    /// <returns>
    /// A <see cref="ConcurrentTypeLookup.Query"/> initialized with <paramref name="typeLookup"/> and the current type
    /// information.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="typeLookup"/> is <see langword="null"/>.</exception>
    public ConcurrentTypeLookup.Query Create(ConcurrentTypeLookup typeLookup)
    {
        ArgumentNullException.ThrowIfNull(typeLookup);
        return new ConcurrentTypeLookup.Query(typeLookup, _typeInfo);
    }
}
