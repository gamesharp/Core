using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32.System.SystemInformation;

namespace GameSharp.Collections;

[SupportedOSPlatform("windows")]
internal static partial class Kernel32
{
    public static SYSTEM_LOGICAL_PROCESSOR_INFORMATION[] GetLogicalProcessorInformation()
    {
        uint byteCount = GetLogicalProcessorInformationByteCount();
        long elemCount = byteCount / Unsafe.SizeOf<SYSTEM_LOGICAL_PROCESSOR_INFORMATION>();
        if (elemCount > int.MaxValue)
        {
            TooManyLogicalProcessorInformationEntries();
        }

        SYSTEM_LOGICAL_PROCESSOR_INFORMATION[] result = GC.AllocateUninitializedArray<SYSTEM_LOGICAL_PROCESSOR_INFORMATION>((int)elemCount, pinned: true);
        ref SYSTEM_LOGICAL_PROCESSOR_INFORMATION data = ref MemoryMarshal.GetArrayDataReference(result);
        ref byte address = ref Unsafe.As<SYSTEM_LOGICAL_PROCESSOR_INFORMATION, byte>(ref data);

        if (!GetLogicalProcessorInformation(ref address, ref byteCount))
        {
            int error = Marshal.GetLastWin32Error();
            UnhandledWin32Exception(error);
        }

        return result;
    }

    private static uint GetLogicalProcessorInformationByteCount()
    {
        uint byteCount = 0;

        GetLogicalProcessorInformation(ref Unsafe.NullRef<byte>(), ref byteCount);
        int error = Marshal.GetLastWin32Error();
        if (error != 122) // ERROR_INSUFFICIENT_BUFFER
        {
            UnhandledWin32Exception(error);
        }

        return byteCount;
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetLogicalProcessorInformation(ref byte Buffer, ref uint ReturnLength);

    [DoesNotReturn, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
    private static void TooManyLogicalProcessorInformationEntries()
    {
        throw new OutOfMemoryException("Too many logical processor information entries.");
    }

    [DoesNotReturn, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
    private static void UnhandledWin32Exception(int error)
    {
        throw new Win32Exception(error);
    }
}
