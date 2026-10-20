using Xunit;

namespace GameSharp.Collections.Tests.TypeLookup;

public sealed class TypeLookupTests(TypeLookupTests.ClassFixture fixture) : 
    Tests<Collections.TypeLookup>(fixture), 
    IClassFixture<TypeLookupTests.ClassFixture>
{
    public sealed class ClassFixture : ClassFixture<Collections.TypeLookup>
    {
        public override Collections.TypeLookup WithSingleCar => ImmutableWithSingleCar.AsTypeLookup();
        public override Collections.TypeLookup WithAll => ImmutableWithAll.AsTypeLookup();
        public override Collections.TypeLookup Empty => [];
    }

    protected override Collections.TypeLookup Add<TItem>(Collections.TypeLookup lookup, TItem item)
    {
        lookup.Add(item);
        return lookup;
    }

    protected override Collections.TypeLookup Remove<TItem>(Collections.TypeLookup lookup, TItem item)
    {
        lookup.Remove(item);
        return lookup;
    }

    protected override Collections.TypeLookup Replace<TItem>(Collections.TypeLookup lookup, TItem? existing, TItem replacement) where TItem : class
    {
        lookup.Replace(existing, replacement);
        return lookup;
    }
}
