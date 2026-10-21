using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GameSharp.Collections;

[StructLayout(LayoutKind.Explicit, Size = 8)] // Do not change
internal struct IntegerLookup(int key, int index) : IComparable<int>
{
    private static readonly int _structsPerVector;
    private static readonly Vector<int> _altMask;

    [FieldOffset(0)]
    public int key = key;

    [FieldOffset(4)]
    public int index = index;

    static IntegerLookup()
    {
        if (!Vector.IsHardwareAccelerated)
        {
            return;
        }

        _structsPerVector = Vector<int>.Count / 2;

        Span<int> mask = stackalloc int[Vector<int>.Count];
        ref int maskDataRef = ref MemoryMarshal.GetReference(mask);
        for (int i = 0; i < mask.Length; i++)
        {
            Unsafe.Add(ref maskDataRef, i) = i % 2;
        }

        _altMask = new Vector<int>(mask);
    }

    public static void Offset(Span<IntegerLookup> values, int count)
    {
        ref IntegerLookup currLookup = ref MemoryMarshal.GetReference(values);
        ref IntegerLookup endLookup = ref Unsafe.Add(ref currLookup, values.Length);

        if (Vector.IsHardwareAccelerated)
        {
            (int quotient, int remainder) = Math.DivRem(values.Length, _structsPerVector);

            if (quotient > 0)
            {
                Vector<int> offset = _altMask * count;

                ref int currInt = ref Unsafe.As<IntegerLookup, int>(ref currLookup);
                ref int endInt = ref Unsafe.Add(ref currInt, quotient * Vector<int>.Count);

                for (; Unsafe.IsAddressLessThan(ref currInt, ref endInt); currInt = ref Unsafe.Add(ref currInt, Vector<int>.Count))
                {
                    Vector<int> vector = Vector.LoadUnsafe(ref currInt) + offset;
                    vector.StoreUnsafe(ref currInt);
                }

                currLookup = ref Unsafe.Add(ref currLookup, values.Length - remainder);
            }
        }

        for (; Unsafe.IsAddressLessThan(ref currLookup, ref endLookup); currLookup = ref Unsafe.Add(ref currLookup, 1))
        {
            currLookup.index += count;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ElementOffset(int byteOffset) => byteOffset >> 3;

    public readonly int CompareTo(int other)
    {
        return key.CompareTo(other);
    }
}
