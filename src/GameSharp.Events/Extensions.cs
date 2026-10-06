using System.Runtime.CompilerServices;

namespace GameSharp.Events;

internal static class Extensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T UnsafeCast<T>(this object value) where T : class
    {
#if DEBUG
        return (T)value;
#else
        return Unsafe.As<T>(value);
#endif
    }
}
