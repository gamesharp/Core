using GameSharp.Generic;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace GameSharp;

/// <summary>
/// Provides an abstract base class for objects with a manageable lifetime.
/// </summary>
public abstract class Lifecycle
{
    private ILifetimeStage? _lastStage;

    /// <inheritdoc cref="Complete{TState}(ILifetimeStage{TState}, TState, string?)"/>
    protected void Complete(ILifetimeStage stage, object? state = null, [CallerArgumentExpression(nameof(stage))] string? paramName = null)
    {
#if NET6_0_OR_GREATER
        ThrowHelpers.ThrowIfArgIsNull(stage, paramName);
        if (!stage.TryComplete(ref _lastStage, state))
        {
            ThrowHelpers.StageHasStarted(paramName);
        }
#else
        if (stage is null)
        {
            throw new ArgumentNullException(paramName);
        }
        if (!stage.TryComplete(ref _lastStage, state))
        {
            throw ThrowHelpers.StageHasStartedException(paramName);
        }
#endif
    }

    /// <summary>
    /// Completes the specified stage of the lifetime.
    /// </summary>
    /// <typeparam name="TState">The type of the state parameter.</typeparam>
    /// <param name="stage">The stage to be completed.</param>
    /// <param name="state">A state parameter to pass to the stage.</param>
    /// <param name="paramName">The name of the stage parameter.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the <paramref name="stage"/> is <see langword="null"/>.
    /// </exception>
    protected void Complete<TState>(ILifetimeStage<TState> stage, TState state, [CallerArgumentExpression(nameof(stage))] string? paramName = null)
    {
#if NET6_0_OR_GREATER
        ThrowHelpers.ThrowIfArgIsNull(stage, paramName);
        if (!stage.TryComplete(ref _lastStage, state))
        {
            ThrowHelpers.StageHasStarted(paramName);
        }
#else
        if (stage is null)
        {
            throw new ArgumentNullException(paramName);
        }
        if (!stage.TryComplete(ref _lastStage, state))
        {
            throw ThrowHelpers.StageHasStartedException(paramName);
        }
#endif
    }

    /// <inheritdoc cref="TryComplete{TState}(ILifetimeStage{TState}, TState)"/>
    protected bool TryComplete([NotNullWhen(true)] ILifetimeStage? stage, object? state = null)
    {
        return stage?.TryComplete(ref _lastStage, state) ?? false;
    }

    /// <summary>
    /// Attempts to complete the specified stage of the lifetime.
    /// </summary>
    /// <typeparam name="TState">The type of the state parameter.</typeparam>
    /// <param name="stage">The stage to be completed.</param>
    /// <param name="state">A state parameter to pass to the stage.</param>
    /// <returns><see langword="true"/> if the stage was successfully completed; otherwise, <see langword="false"/>.</returns>
    protected bool TryComplete<TState>([NotNullWhen(true)] ILifetimeStage<TState>? stage, TState state)
    {
        return stage?.TryComplete(ref _lastStage, state) ?? false;
    }
}
