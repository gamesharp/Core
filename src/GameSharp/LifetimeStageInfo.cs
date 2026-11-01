using System.Threading.Tasks;

namespace GameSharp;

/// <summary>
/// Represents information about a specific stage of an asynchronous lifetime.
/// </summary>
public readonly struct LifetimeStageInfo : ILifetimeStageInfo
{
    private readonly ILifetimeStageAwaiter _awaiter;

    /// <inheritdoc/>
    public bool IsStarted { get; }

    /// <inheritdoc/>
    public bool IsCompleted => IsStarted && _awaiter.IsCompleted;

    /// <inheritdoc/>
    public bool IsCompletedSuccessfully => IsStarted && _awaiter.Task is { Status: TaskStatus.RanToCompletion };

    internal LifetimeStageInfo(bool isStarted, ILifetimeStageAwaiter awaiter)
    {
        IsStarted = isStarted;
        _awaiter = awaiter;
    }

    /// <inheritdoc/>
    public ILifetimeStageAwaiter GetAwaiter() => _awaiter;
}
