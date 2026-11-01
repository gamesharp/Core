using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GameSharp.Generic;

/// <inheritdoc/>
/// <typeparam name="TState">The type of the state associated with the lifetime stage.</typeparam>
public abstract class AsynchronousLifetimeStage<TState> : LifetimeStage, ILifetimeStage<TState>
{
    private readonly struct TaskFactory : ITaskFactory
    {
        public required AsynchronousLifetimeStage<TState> Stage { get; init; }
        public required TState State { get; init; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task CreateTask(CancellationToken cancellationToken) => Stage.ExecuteAsync(State, cancellationToken);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryComplete(ref ILifetimeStage? current, TState state)
    {
        return TryComplete(ref current, new TaskFactory { Stage = this, State = state });
    }

    /// <summary>
    /// Executes the asynchronous operation associated with this lifetime stage.
    /// </summary>
    /// <param name="state">The state object passed to the asynchronous operation.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual async Task ExecuteAsync(TState state, CancellationToken cancellationToken)
    {
        switch (Previous)
        {
            case LifetimeStage { Info: var info }: await info; break;
            case { Info: var info }: await info; break;
        }
    }

    bool ILifetimeStage.TryComplete(ref ILifetimeStage? current, object? state)
    {
        return LifetimeHelpers.TryCompleteStageAs(this, ref current, state);
    }
}
