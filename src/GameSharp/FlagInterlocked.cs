using System.Runtime.CompilerServices;
using System.Threading;

namespace GameSharp;

internal static class FlagInterlocked
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Exchange(ref Flag location1, bool value)
    {
        return ToBoolean(Interlocked.Exchange(ref location1, ToValue(value)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Read(ref Flag location1)
    {
        return ToBoolean(Volatile.Read(ref location1));
    }

    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Flag ToValue(bool value)
    {
#if NET8_0_OR_GREATER
        return value;
#else
        int result;
        unsafe
        {
            *(bool*)&result = value;
        }
        return result;
#endif
    }

    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ToBoolean(Flag value)
    {
#if NET8_0_OR_GREATER
        return value;
#else
        unsafe
        {
            return *(bool*)&value;
        }
#endif
    }
}
