using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GameSharp.Collections;

internal ref struct TypeLookupClusterEnumeratorCore : IStackOnlyEnumerator<TypeLookupCluster>
{
    private readonly ReadOnlySpan<object> _items;
    private readonly ref IntegerLookup _last;
    private ref IntegerLookup _current;

    /// <inheritdoc cref="IEnumerator{T}.Current"/>
    public TypeLookupCluster Current { readonly get; private set; }

    internal TypeLookupClusterEnumeratorCore(ReadOnlySpan<IntegerLookup> lookups, ReadOnlySpan<object> items)
    {
        _current = ref MemoryMarshal.GetReference(lookups);
        _last = ref Unsafe.Add(ref _current, lookups.Length - 1);
        _items = items;
    }

    /// <inheritdoc cref="IEnumerator.MoveNext()"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        if (Unsafe.IsAddressGreaterThan(in _current, in _last))
        {
            return false;
        }

        ref IntegerLookup nextRef = ref Unsafe.Add(ref _current, 1);
        TypeInfo typeInfo = TypeInfo.Get(_current.key);
        if (Unsafe.AreSame(in _current, in _last))
        {
            Current = new TypeLookupCluster(typeInfo, _items[_current.index..]);
        }
        else
        {
            Current = new TypeLookupCluster(typeInfo, _items[_current.index..nextRef.index]);
        }
        _current = ref nextRef;

        return true;
    }
}
