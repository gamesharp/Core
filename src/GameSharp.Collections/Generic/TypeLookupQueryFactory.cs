using GameSharp.Collections.Concurrent;
using GameSharp.Collections.Immutable;

namespace GameSharp.Collections.Generic;

/// <inheritdoc cref="TypeLookupQueryFactory"/>
/// <typeparam name="T">The type for which the query will be created.</typeparam>
public readonly struct TypeLookupQueryFactory<T> where T : class
{
    private readonly TypeInfo _typeInfo;

    internal TypeLookupQueryFactory(TypeInfo typeInfo)
    {
        _typeInfo = typeInfo;
    }

    /// <summary>
    /// Creates a <see cref="TypeLookup.Query{T}"/> for the specified type lookup and the current type information.
    /// </summary>
    /// <param name="typeLookup">The <see cref="TypeLookup"/> used to create the query.</param>
    /// <returns>
    /// A <see cref="TypeLookup.Query{T}"/> initialized with <paramref name="typeLookup"/> and the current type
    /// information.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="typeLookup"/> is <see langword="null"/>.</exception>
    public TypeLookup.Query<T> Create(TypeLookup typeLookup)
    {
        ArgumentNullException.ThrowIfNull(typeLookup);
        return new TypeLookup.Query<T>(typeLookup, _typeInfo);
    }

    /// <summary>
    /// Creates a <see cref="ImmutableTypeLookup.Query{T}"/> for the specified type lookup and the current type information.
    /// </summary>
    /// <param name="typeLookup">The <see cref="ImmutableTypeLookup"/> used to create the query.</param>
    /// <returns>
    /// A <see cref="ImmutableTypeLookup.Query{T}"/> initialized with <paramref name="typeLookup"/> and the current type
    /// information.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="typeLookup"/> is <see langword="null"/>.</exception>
    public ImmutableTypeLookup.Query<T> Create(ImmutableTypeLookup typeLookup)
    {
        ArgumentNullException.ThrowIfNull(typeLookup);
        return new ImmutableTypeLookup.Query<T>(typeLookup, _typeInfo);
    }

    /// <summary>
    /// Creates a <see cref="ConcurrentTypeLookup.Query{T}"/> for the specified type lookup and the current type information.
    /// </summary>
    /// <param name="typeLookup">The <see cref="ConcurrentTypeLookup"/> used to create the query.</param>
    /// <returns>
    /// A <see cref="ConcurrentTypeLookup.Query{T}"/> initialized with <paramref name="typeLookup"/> and the current type
    /// information.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="typeLookup"/> is <see langword="null"/>.</exception>
    public ConcurrentTypeLookup.Query<T> Create(ConcurrentTypeLookup typeLookup)
    {
        ArgumentNullException.ThrowIfNull(typeLookup);
        return new ConcurrentTypeLookup.Query<T>(typeLookup, _typeInfo);
    }
}
