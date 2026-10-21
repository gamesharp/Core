using System.Runtime.CompilerServices;

namespace GameSharp.Collections.Immutable;

internal readonly struct AddTransformation : IImmutableTypeLookupTransformer
{
    public required object Item { get; init; }
    public required TypeInfo TypeInfo { get; init; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ImmutableTypeLookup Apply(ImmutableTypeLookup typeLookup) => typeLookup.Add(Item, TypeInfo);
}
