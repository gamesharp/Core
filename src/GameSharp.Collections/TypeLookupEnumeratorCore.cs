using System.Collections;
using System.Runtime.CompilerServices;

namespace GameSharp.Collections;

internal ref struct TypeLookupEnumeratorCore<T>(T clusterEnum) : IStackOnlyEnumerator<KeyValuePair<Type, object>>
    where T : struct, IStackOnlyEnumerator<TypeLookupCluster>, allows ref struct
{
    private TypeLookupCluster _current;
    private T _clusterEnum = clusterEnum;
    private ReadOnlySpan<object>.Enumerator _itemEnum;

    /// <inheritdoc cref="IEnumerator{T}.Current"/>
    public readonly KeyValuePair<Type, object> Current => new(_current.Type, _itemEnum.Current);

    /// <inheritdoc cref="IEnumerator.MoveNext()"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        while (!_itemEnum.MoveNext())
        {
            if (_clusterEnum.MoveNext())
            {
                _current = _clusterEnum.Current;
                _itemEnum = _current.Items.GetEnumerator();
                continue;
            }

            return false;
        }

        return true;
    }
}
