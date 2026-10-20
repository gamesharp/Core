using Xunit;

namespace GameSharp.Collections.Tests.TypeLookup;

public sealed class ConcurrentTypeLookupTests(ConcurrentTypeLookupTests.ClassFixture fixture) : 
    Tests<Concurrent.ConcurrentTypeLookup>(fixture), 
    IClassFixture<ConcurrentTypeLookupTests.ClassFixture>
{
    public sealed class ClassFixture : ClassFixture<Concurrent.ConcurrentTypeLookup>
    {
        public override Concurrent.ConcurrentTypeLookup WithSingleCar => ImmutableWithSingleCar.AsConcurrentTypeLookup();
        public override Concurrent.ConcurrentTypeLookup WithAll => ImmutableWithAll.AsConcurrentTypeLookup();
        public override Concurrent.ConcurrentTypeLookup Empty => [];
    }

    protected override Concurrent.ConcurrentTypeLookup Add<TItem>(Concurrent.ConcurrentTypeLookup lookup, TItem item)
    {
        lookup.Add(item);
        return lookup;
    }

    protected override Concurrent.ConcurrentTypeLookup Remove<TItem>(Concurrent.ConcurrentTypeLookup lookup, TItem item)
    {
        lookup.Remove(item);
        return lookup;
    }

    protected override Concurrent.ConcurrentTypeLookup Replace<TItem>(Concurrent.ConcurrentTypeLookup lookup, TItem? existing, TItem replacement) where TItem : class
    {
        lookup.Replace(existing, replacement);
        return lookup;
    }
}
