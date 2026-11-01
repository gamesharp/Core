namespace GameSharp.Generic;

internal static class LifetimeHelpers
{
    public static bool TryCompleteStageAs<TState>(ILifetimeStage<TState> stage, ref ILifetimeStage? current, object? state)
    {
        if (state is TState typedState)
        {
            return stage.TryComplete(ref current, typedState);
        }
        else
        {
#if NET6_0_OR_GREATER
            ThrowHelpers.ThrowIfArgIsNull(state);
            ThrowHelpers.ArgIsWrongType(typeof(TState), nameof(state));
            return false;
#else
            if (state is null)
            {
                throw new System.ArgumentNullException(nameof(state));
            }
            throw ThrowHelpers.ArgIsWrongTypeException(typeof(TState), nameof(state));
#endif
        }
    }
}
