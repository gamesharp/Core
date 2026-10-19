namespace GameSharp.Collections;

internal ref struct TypeLookupQueryClusterEnumeratorCore
{
    private readonly ReadOnlySpan<object> _items;
    private readonly ReadOnlySpan<IntegerLookup> _lookups;
    private int _lookupsStart, _lookupsEnd, _itemsEnd, _id;
    private DerivedTypeCollection.Enumerator _derivedEnumerator;

    public TypeLookupClusterIndex Current { get; private set; }

    public TypeLookupQueryClusterEnumeratorCore(TypeInfo typeInfo, ReadOnlySpan<IntegerLookup> lookups, ReadOnlySpan<object> items)
    {
        _derivedEnumerator = typeInfo.Derived.GetEnumerator();
        _id = _derivedEnumerator.ID;
        _lookupsEnd = lookups.Length;
        _itemsEnd = items.Length;
        _lookups = lookups;
        _items = items;
    }

    public bool MoveNext()
    {
        for (TypeLookupClusterIndex cluster; _derivedEnumerator.MoveNext(); _lookupsStart += cluster.Index)
        {
            if (_derivedEnumerator.Current < _id)
            {
                _itemsEnd = _lookupsStart < _lookups.Length ? _lookups[_lookupsStart].index : _items.Length;
                (_lookupsStart, _lookupsEnd) = (0, _lookupsStart);
                _id = _derivedEnumerator.Current;
            }

            if (!TypeLookup.TryGetCluster(_lookups[_lookupsStart.._lookupsEnd], _items[.._itemsEnd], _derivedEnumerator.Current, out cluster))
            {
                continue;
            }

            Current = cluster;
            return true;
        }

        return false;
    }
}
