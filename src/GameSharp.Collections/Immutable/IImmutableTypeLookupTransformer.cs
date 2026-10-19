namespace GameSharp.Collections.Immutable;

public interface IImmutableTypeLookupTransformer
{
    ImmutableTypeLookup Apply(ImmutableTypeLookup typeLookup);
}
