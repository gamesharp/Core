using System.Runtime.CompilerServices;

namespace GameSharp.Collections.Immutable;

internal readonly struct ReplaceTransformation : IImmutableTypeLookupTransformer
{
    public required object ExistingItem { get; init; }
    public required object NewItem { get; init; }
    public required TypeInfo TypeInfo { get; init; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ImmutableTypeLookup Apply(ImmutableTypeLookup typeLookup) => typeLookup.Replace(ExistingItem, NewItem, TypeInfo);
}
