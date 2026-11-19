using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace GameSharp.Mathematics;

internal static class ThrowHelpers
{
    [StackTraceHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfIndexOutOfRange(int index, int length)
    {
        if ((uint)index >= (uint)length)
        {
            ThrowIndexOutOfRangeException();
        }
    }

    [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowIndexOutOfRangeException()
    {
        throw new IndexOutOfRangeException();
    }
}