using System;

namespace GameSharp;

internal static class ThrowHelpers
{
    internal static InvalidOperationException LockClosingOrClosedException()
    {
        return new InvalidOperationException("Cannot enter the lock because it is closing or has been closed.");
    }

#if !PRESERVE_STACK_TRACE
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
#if NET6_0_OR_GREATER
    [System.Diagnostics.StackTraceHidden]
#endif
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    internal static void LockClosingOrClosed()
    {
        throw LockClosingOrClosedException();
    }
#endif
}
