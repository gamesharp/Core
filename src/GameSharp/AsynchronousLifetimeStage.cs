using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GameSharp;

/// <inheritdoc/>
public abstract class AsynchronousLifetimeStage : LifetimeStage, ILifetimeStage
{
    private readonly struct TaskFactory : ITaskFactory
    {
        public required AsynchronousLifetimeStage Stage { get; init; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task CreateTask(CancellationToken cancellationToken) => Stage.ExecuteAsync(cancellationToken);
    }

    /// <inheritdoc cref="ILifetimeStage.TryComplete(ref ILifetimeStage?, object?)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryComplete(ref ILifetimeStage? current)
    {
        return TryComplete(ref current, new TaskFactory { Stage = this });
    }

    /// <summary>
    /// Executes the asynchronous operation associated with this lifetime stage.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        switch (Previous)
        {
            case LifetimeStage { Info: var info }: await info; break;
            case { Info: var info }: await info; break;
        }
    }

    bool ILifetimeStage.TryComplete(ref ILifetimeStage? current, object? state)
    {
        return TryComplete(ref current);
    }
}
