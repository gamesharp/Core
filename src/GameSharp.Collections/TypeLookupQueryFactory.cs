namespace GameSharp.Collections;

/// <summary>
/// Factory that creates <see cref="ReadOnlyTypeLookup.Query"/> instances for a specific type.
/// </summary>
public readonly struct TypeLookupQueryFactory
{
    private readonly TypeInfo _typeInfo;

    internal TypeLookupQueryFactory(TypeInfo typeInfo)
    {
        _typeInfo = typeInfo;
    }

    /// <summary>
    /// Creates a <see cref="ReadOnlyTypeLookup.Query"/> that encapsulates the provided <see cref="ReadOnlyTypeLookup"/> and the current type
    /// information.
    /// </summary>
    /// <param name="typeLookup">The <see cref="ReadOnlyTypeLookup"/> to use as the source for the query.</param>
    /// <returns>A new <see cref="ReadOnlyTypeLookup.Query"/> initialized with the specified type lookup and the current type information.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="typeLookup"/> is null.</exception>
    public ReadOnlyTypeLookup.Query Create(ReadOnlyTypeLookup typeLookup)
    {
        ArgumentNullException.ThrowIfNull(typeLookup);
        return new ReadOnlyTypeLookup.Query(typeLookup, _typeInfo);
    }
}
