using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GameSharp.Collections;

internal sealed class DerivedTypeCollection : IList<int>, IReadOnlyList<int>
{
    internal readonly ref struct CopyContext(int id, ImmutableArray<int> ids)
    {
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ids.Length + 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CopyTo(Span<int> destination)
        {
            return DerivedTypeCollection.CopyTo(ids.AsSpan(), id, destination);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public ref struct Enumerator
    {
        // _r1 and _r2 must remain adjacent. ReadOnlySpan memory marshaling depends on this layout.
        private readonly Range _r1, _r2;
        private int _rangeIndex;
        private ReadOnlySpan<int>.Enumerator _intEnum;
        private readonly ReadOnlySpan<int> _values;

        internal int ID { get; }

        public int Current => _intEnum.Current;

        internal Enumerator([UnscopedRef] ref readonly int id, ImmutableArray<int> ids)
        {
            ID = id;
            ReadOnlySpan<int> init = MemoryMarshal.CreateReadOnlySpan(in id, 1);
            _intEnum = init.GetEnumerator();

            _values = ids.AsSpan();
            _values.HybridSearch(id, out int byteOffset, out _);
            int pivot = ToElementOffset(byteOffset);
            _r1 = pivot.._values.Length;
            _r2 = 0..pivot;
        }

        public bool MoveNext()
        {
            while (!_intEnum.MoveNext())
            {
                if (_rangeIndex >= 2)
                {
                    return false;
                }

                Range range = Unsafe.Add(ref Unsafe.AsRef(in _r1), _rangeIndex++);
                _intEnum = _values[range].GetEnumerator();
            }

            return true;
        }
    }

    private readonly int _typeID;
    private ImmutableArray<int> _ids = [];

    public int Count => _ids.Length + 1;

    bool ICollection<int>.IsReadOnly => true;

    public int this[int index]
    { 
        get => index == 0 ? _typeID : _ids[index - 1]; 
        set => throw new NotSupportedException();
    }

    internal DerivedTypeCollection(int id)
    {
        _typeID = id;
    }

    public bool Contains(int item)
    {
        if (item == _typeID)
        {
            return true;
        }

        ReadOnlySpan<int> span = _ids.AsSpan();
        span.HybridSearch(item, out int _, out bool exists);
        return exists;
    }

    public int IndexOf(int item)
    {
        ReadOnlySpan<int> span = _ids.AsSpan();
        span.HybridSearch(item, out int byteOffset, out bool exists);
        return exists || item == _typeID ? ToElementOffset(byteOffset) : -1;
    }

    public void CopyTo(int[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);
        ReadOnlySpan<int> ids = _ids.AsSpan();
        ThrowHelpers.ThrowIfArrayIndexIsOutOfRange(arrayIndex, array, ids.Length + 1);
        CopyTo(ids, _typeID, array.AsSpan(arrayIndex));
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(in _typeID, _ids);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal CopyContext GetCopyContext() => new(_typeID, _ids);

    internal bool Add(int typeID)
    {
        if (typeID == _typeID)
        {
            return false;
        }

        return ImmutableInterlocked.Update(ref _ids, InsertSorted, typeID);
    }

    internal bool RemoveAll(short assemblyID)
    {
        return ImmutableInterlocked.Update(ref _ids, RemoveSorted, assemblyID);
    }

    private static int CopyTo(ReadOnlySpan<int> source, int id, Span<int> destination)
    {
        source.HybridSearch(id, out int byteOffset, out _);

        if (byteOffset == 0)
        {
            destination[0] = id;
            int length = Math.Min(destination.Length, source.Length + 1);
            source.CopyTo(destination[1..length]);
            return length;
        }

        int pivot = ToElementOffset(byteOffset);
        if (pivot < destination.Length)
        {
            source[..pivot].CopyTo(destination[..pivot]);
            destination[pivot] = id;

            int next = pivot + 1;
            if (next > destination.Length)
            {
                return next;
            }

            int length = Math.Min(destination.Length, source.Length + 1);
            source[pivot..length].CopyTo(destination[next..]);
            return length;
        }

        source[..destination.Length].CopyTo(destination);
        return destination.Length;
    }

    private static ImmutableArray<int> RemoveSorted(ImmutableArray<int> ids, short assemblyID)
    {
        int count = ids.Length;

        foreach (int typeID in ids)
        {
            if (((TypeIdentifier)typeID).AssemblyID == assemblyID)
            {
                count--;
            }
        }

        if (count == ids.Length)
        {
            return ids;
        }

        int[] newIds = GC.AllocateUninitializedArray<int>(count);
        ref int idRef = ref MemoryMarshal.GetArrayDataReference(newIds);
        foreach (int typeID in ids)
        {
            if (((TypeIdentifier)typeID).AssemblyID == assemblyID)
            {
                continue;
            }

            idRef = typeID;
            idRef = ref Unsafe.Add(ref idRef, 1);
        }

        return ImmutableCollectionsMarshal.AsImmutableArray(newIds);
    }

    private static ImmutableArray<int> InsertSorted(ImmutableArray<int> ids, int id)
    {
        ReadOnlySpan<int> span = ids.AsSpan();
        span.HybridSearch(id, out int byteOffset, out bool exists);

        if (exists)
        {
            return ids;
        }

        int index = ToElementOffset(byteOffset);
        return ids.Insert(index, id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ToElementOffset(int byteOffset) => byteOffset >> 2;

    void ICollection<int>.Add(int item)
    {
        throw new NotSupportedException();
    }

    void ICollection<int>.Clear()
    {
        throw new NotSupportedException();
    }

    bool ICollection<int>.Remove(int item)
    {
        throw new NotSupportedException();
    }

    void IList<int>.Insert(int index, int item)
    {
        throw new NotSupportedException();
    }

    void IList<int>.RemoveAt(int index)
    {
        throw new NotSupportedException();
    }

    IEnumerator<int> IEnumerable<int>.GetEnumerator()
    {
        ImmutableArray<int> ids = _ids;

        if (ids.IsEmpty)
        {
            return ((IEnumerable<int>)[]).GetEnumerator();
        }

        int[] typeInfos = new int[ids.Length + 1];
        ref int typeInfo = ref MemoryMarshal.GetArrayDataReference(typeInfos);
        Enumerator enumerator = new(in _typeID, ids);
        for (; enumerator.MoveNext(); typeInfo = ref Unsafe.Add(ref typeInfo, 1))
        {
            typeInfo = enumerator.Current;
        }

        return ((IEnumerable<int>)typeInfos).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable<int>)this).GetEnumerator();
    }
}
