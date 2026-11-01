using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GameSharp;

/// <inheritdoc/>
public abstract class SynchronousLifetimeStage : LifetimeStage, ILifetimeStage
{
    private readonly struct TaskFactory : ITaskFactory
    {
        public required SynchronousLifetimeStage Stage { get; init; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task CreateTask(CancellationToken cancellationToken) => Stage.CompleteSynchronously();
    }

    /// <inheritdoc cref="ILifetimeStage.TryComplete(ref ILifetimeStage?, object?)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryComplete(ref ILifetimeStage? current)
    {
        return TryComplete(ref current, new TaskFactory { Stage = this });
    }

    /// <summary>
    /// Completes the current stage.
    /// </summary>
    protected virtual void Complete()
    {
        switch (Previous)
        {
            case LifetimeStage stage: stage.GetResult(); break;
            case { Info: var info }: info.GetAwaiter().GetResult(); break;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private Task CompleteSynchronously()
    {
        try
        {
            Complete();
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            return Task.FromException(ex);
        }
    }

    bool ILifetimeStage.TryComplete(ref ILifetimeStage? current, object? state) => TryComplete(ref current);
}
