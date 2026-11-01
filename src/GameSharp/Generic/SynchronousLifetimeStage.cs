using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GameSharp.Generic;

/// <summary>
/// Provides an abstract base class for a stage in the lifetime of an <see cref="Lifecycle"/> object.
/// </summary>
/// <typeparam name="TState">The type of the state associated with the lifetime stage.</typeparam>
public abstract class SynchronousLifetimeStage<TState> : LifetimeStage, ILifetimeStage<TState>
{
    private readonly struct TaskFactory : ITaskFactory
    {
        public required SynchronousLifetimeStage<TState> Stage { get; init; }
        public required TState State { get; init; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task CreateTask(CancellationToken cancellationToken) => Stage.CompleteSynchronously(State);
    }

    /// <inheritdoc cref="ILifetimeStage.TryComplete(ref ILifetimeStage?, object?)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryComplete(ref ILifetimeStage? current, TState state)
    {
        return TryComplete(ref current, new TaskFactory { Stage = this, State = state });
    }

    /// <summary>
    /// Completes the current stage.
    /// </summary>
    /// <param name="state">The state associated with the lifetime stage.</param>
    protected virtual void Complete(TState state)
    {
        switch (Previous)
        {
            case LifetimeStage stage: stage.GetResult(); break;
            case { Info: var info }: info.GetAwaiter().GetResult(); break;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private Task CompleteSynchronously(TState state)
    {
        try
        {
            Complete(state);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            return Task.FromException(ex);
        }
    }

    bool ILifetimeStage.TryComplete(ref ILifetimeStage? current, object? state)
    {
        return LifetimeHelpers.TryCompleteStageAs(this, ref current, state);
    }
}
