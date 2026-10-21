using System.Runtime.CompilerServices;

namespace GameSharp.Collections.Immutable;

internal readonly struct DelegateTransformation : IImmutableTypeLookupTransformer
{
    public required Func<ImmutableTypeLookup, ImmutableTypeLookup> Transformation { get; init; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ImmutableTypeLookup Apply(ImmutableTypeLookup typeLookup) => Transformation(typeLookup);
}

internal readonly ref struct DelegateTransformation<TState> : IImmutableTypeLookupTransformer
    where TState : allows ref struct
{
    public required TState State { get; init; }
    public required Func<ImmutableTypeLookup, TState, ImmutableTypeLookup> Transformation { get; init; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ImmutableTypeLookup Apply(ImmutableTypeLookup typeLookup) => Transformation(typeLookup, State);
}

internal readonly ref struct DelegateTransformation<TState1, TState2> : IImmutableTypeLookupTransformer
    where TState1 : allows ref struct
    where TState2 : allows ref struct
{
    public required TState1 State1 { get; init; }
    public required TState2 State2 { get; init; }
    public required Func<ImmutableTypeLookup, TState1, TState2, ImmutableTypeLookup> Transformation { get; init; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ImmutableTypeLookup Apply(ImmutableTypeLookup typeLookup) => Transformation(typeLookup, State1, State2);
}