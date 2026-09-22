using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace GameSharp.Collections;

internal static partial class LibC
{
    [SupportedOSPlatform("linux"), SupportedOSPlatform("android")]
    public static nint SysConf(int name)
    {
        Marshal.SetLastPInvokeError(0);

        nint result = sysconf(name);

        if (result == -1)
        {
            int error = Marshal.GetLastPInvokeError();
            if (error != 0)
            {
                UnhandledPlatformException(error);
            }
        }

        return result;
    }

    [SupportedOSPlatform("osx")]
    public static unsafe T SysCtlByName<T>(string name) where T : unmanaged
    {
        Unsafe.SkipInit(out T value);
        nuint length = (nuint)sizeof(T);
        if (sysctlbyname(name, &value, ref length, null, 0) != 0)
        {
            int error = Marshal.GetLastPInvokeError();
            UnhandledPlatformException(error);
        }
        return value;
    }

    [LibraryImport("libc"), SupportedOSPlatform("linux"), SupportedOSPlatform("android")]
    private static partial nint sysconf(int name);

    [LibraryImport("libc", StringMarshalling = StringMarshalling.Utf8, SetLastError = true), SupportedOSPlatform("osx")]
    internal static unsafe partial int sysctlbyname(
        string name,
        void* oldp,
        ref nuint oldlenp,
        void* newp,
        nuint newlen);

    [DoesNotReturn, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
    private static void UnhandledPlatformException(int error)
    {
        throw new Win32Exception(error);
    }
}
