using System;

namespace GameSharp;

/// <summary>
/// Provides helper methods for throwing exceptions.
/// </summary>
public static class ThrowHelpers
{
#if NET6_0_OR_GREATER
    /// <summary>
    /// Ensures that the lifetime stage has not yet started.
    /// </summary>
    /// <param name="stage">The lifetime stage to check.</param>
    /// <param name="paramName">The name of the parameter.</param>
    /// <exception cref="InvalidOperationException">Thrown if the lifetime stage has already started.</exception>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    [System.Diagnostics.StackTraceHidden]
    public static void ThrowIfStarted(this LifetimeStageInfo stage, 
        [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(stage))] string? paramName = null)
    {
        if (stage.IsStarted)
        {
            StageHasStarted(paramName);
        }
    }

    /// <summary>
    /// Ensures that the lifetime stage has started.
    /// </summary>
    /// <param name="stage">The lifetime stage to check.</param>
    /// <param name="paramName">The name of the parameter.</param>
    /// <exception cref="InvalidOperationException">Thrown if the lifetime stage has not yet started.</exception>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    [System.Diagnostics.StackTraceHidden]
    public static void ThrowIfPending(this LifetimeStageInfo stage, 
        [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(stage))] string? paramName = null)
    {
        if (stage.IsStarted)
        {
            StageIsPending(paramName);
        }
    }

    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    [System.Diagnostics.StackTraceHidden]
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    internal static void StageHasStarted(string? paramName)
    {
        throw StageHasStartedException(paramName);
    }

    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    [System.Diagnostics.StackTraceHidden]
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    internal static void StageIsPending(string? paramName)
    {
        throw StageIsPendingException(paramName);
    }

#endif
    internal static ArgumentException ArgIsWrongTypeException(Type expectedType, string? paramName = null)
    {
        return new ArgumentException($"Must be of type {expectedType.FullName}.", paramName);
    }

    internal static InvalidOperationException StageHasStartedException(string? paramName = null)
    {
        const string Message = "The lifetime stage has already started.";
        return new InvalidOperationException(Message, new ArgumentException(Message, paramName));
    }

    internal static InvalidOperationException StageIsPendingException(string? paramName = null)
    {
        const string Message = "The lifetime stage has not yet started.";
        return new InvalidOperationException(Message, new ArgumentException(Message, paramName));
    }

#if !PRESERVE_STACK_TRACE
#if NET6_0_OR_GREATER
    [System.Diagnostics.StackTraceHidden]
#endif
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfArgIsNull<T>([System.Diagnostics.CodeAnalysis.NotNull] T? value,
        [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is null)
        {
            ArgIsNull(paramName);
        }
    }

    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static void ArgIsNull(string? paramName)
    {
        throw new ArgumentNullException(paramName);
    }

    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
#if NET6_0_OR_GREATER
    [System.Diagnostics.StackTraceHidden]
#endif
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    internal static void ArgIsWrongType(Type expectedType, string? paramName)
    {
        throw ArgIsWrongTypeException(expectedType, paramName);
    }

#if NET6_0_OR_GREATER
    [System.Diagnostics.StackTraceHidden]
#endif
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfDisposed([System.Diagnostics.CodeAnalysis.DoesNotReturnIf(true)] bool condition, object obj)
    {
        if (condition)
        {
            ObjectIsDisposed(obj);
        }
    }

    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
#if NET6_0_OR_GREATER
    [System.Diagnostics.StackTraceHidden]
#endif
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    internal static void ObjectIsDisposed(object obj)
    {
        throw new ObjectDisposedException(obj.GetType().FullName);
    }
#endif
}
