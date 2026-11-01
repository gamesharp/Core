namespace GameSharp.Generic;

/// <inheritdoc/>
/// <typeparam name="TState">The type of the state associated with the lifetime stage.</typeparam>
public interface ILifetimeStage<TState> : ILifetimeStage
{
    /// <inheritdoc cref="ILifetimeStage.TryComplete(ref ILifetimeStage?, object?)"/>
    bool TryComplete(ref ILifetimeStage? current, TState state);
}
