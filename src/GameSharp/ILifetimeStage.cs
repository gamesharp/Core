namespace GameSharp;

/// <summary>
/// Defines a contract for a stage in the lifetime of an object.
/// </summary>
public interface ILifetimeStage
{
    /// <summary>
    /// Gets information about the current stage of the lifetime.
    /// </summary>
    ILifetimeStageInfo Info { get; }

    /// <summary>
    /// Gets the previously completed lifecycle stage, or <see langword="null"/> if this is the first stage.
    /// </summary>
    ILifetimeStage? Previous { get; }

    /// <summary>
    /// Attempts to complete this stage.
    /// </summary>
    /// <param name="current">A reference to the current stage, if any.</param>
    /// <param name="state">An optional state object associated with the completion of the stage.</param>
    /// <returns><see langword="true"/> if the stage was started; otherwise, <see langword="false"/>.</returns>
    bool TryComplete(ref ILifetimeStage? current, object? state);
}
