namespace GameSharp;

/// <summary>
/// Defines a contract for information about a stage in the lifetime of an object.
/// </summary>
public interface ILifetimeStageInfo
{
    /// <summary>
    /// Gets a value indicating whether this stage has been started.
    /// </summary>
    bool IsStarted { get; }

    /// <summary>
    /// Gets a value indicating whether this stage has been completed.
    /// </summary>
    bool IsCompleted { get; }

    /// <summary>
    /// Gets a value indicating whether this stage has been completed successfully.
    /// </summary>
    bool IsCompletedSuccessfully { get; }

    /// <summary>
    /// Gets a <see cref="ILifetimeStageAwaiter"/> that can be used to await the completion of this stage.
    /// </summary>
    /// <returns>An <see cref="ILifetimeStageAwaiter"/> instance.</returns>
    ILifetimeStageAwaiter GetAwaiter();
}
