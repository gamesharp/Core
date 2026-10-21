using System.Runtime.CompilerServices;

namespace GameSharp.Collections;

internal ref struct TypeLookupQueryEnumeratorCore : IStackOnlyEnumerator<object>
{
    private TypeLookupQueryClusterEnumeratorCore _core;
    private ReadOnlySpan<object>.Enumerator _itemEnumerator;

    public TypeLookupQueryEnumeratorCore(TypeInfo typeInfo, ReadOnlySpan<IntegerLookup> lookups, ReadOnlySpan<object> items)
    {
        _core = new TypeLookupQueryClusterEnumeratorCore(typeInfo, lookups, items);
    }

    /// <inheritdoc/>
    public readonly object Current => _itemEnumerator.Current;

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        if (_itemEnumerator.MoveNext())
        {
            return true;
        }

        if (_core.MoveNext())
        {
            TypeLookupClusterIndex cluster = _core.Current;
            _itemEnumerator = cluster.Items.GetEnumerator();
            return _itemEnumerator.MoveNext();
        }

        return false;
    }
}
