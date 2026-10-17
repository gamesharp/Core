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

    /// <inheritdoc cref="TypeLookupQueryFactory.Create"/>
    public ReadOnlyTypeLookup.Query<T> Create(ReadOnlyTypeLookup typeLookup)
    {
        ArgumentNullException.ThrowIfNull(typeLookup);
        return new ReadOnlyTypeLookup.Query<T>(typeLookup, _typeInfo);
    }
}
