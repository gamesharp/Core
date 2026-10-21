using System.Runtime.CompilerServices;

namespace GameSharp.Collections.Immutable;

internal readonly struct RemoveTransformation : IImmutableTypeLookupTransformer
{
    public required object Item { get; init; }
    public required TypeInfo TypeInfo { get; init; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ImmutableTypeLookup Apply(ImmutableTypeLookup typeLookup) => typeLookup.Remove(Item, TypeInfo);
}
