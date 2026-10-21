using System.Collections;
using System.Runtime.InteropServices;

namespace GameSharp.Collections;

internal class TypeLookupEnumerator : IEnumerator<KeyValuePair<Type, object>>
{
    private readonly ReadOnlyMemory<object> _items;
    private readonly ReadOnlyMemory<IntegerLookup> _lookups;
    private readonly int _lastIndex;
    private IEnumerator<object> _enumerator;
    private int _index;
    private Type? _type;

    public KeyValuePair<Type, object> Current => new(_type!, _enumerator.Current);
    object IEnumerator.Current => Current;

    internal TypeLookupEnumerator(ReadOnlyMemory<IntegerLookup> lookups, ReadOnlyMemory<object> items)
    {
        _items = items;
        _lookups = lookups;
        _lastIndex = lookups.Length - 1;
        IEnumerable<object> empty = [];
        _enumerator = empty.GetEnumerator();
    }

    public virtual bool MoveNext()
    {
        while (!_enumerator.MoveNext())
        {
            if (_index > _lastIndex)
            {
                return false;
            }

            ReadOnlySpan<IntegerLookup> lookups = _lookups.Span;
            IntegerLookup lookup = lookups[_index];
            int nextIndex = _index + 1;
            _type = TypeInfo.Get(lookup.key).Type;
            if (nextIndex > _lastIndex)
            {
                IEnumerable<object> cluster = MemoryMarshal.ToEnumerable(_items[lookup.index..]);
                _enumerator = cluster.GetEnumerator();
            }
            else
            {
                IEnumerable<object> cluster = MemoryMarshal.ToEnumerable(_items[lookup.index..lookups[nextIndex].index]);
                _enumerator = cluster.GetEnumerator();
            }
            _index = nextIndex;
        }

        return true;
    }

    void IDisposable.Dispose() { }

    void IEnumerator.Reset() => throw new NotSupportedException();
}
