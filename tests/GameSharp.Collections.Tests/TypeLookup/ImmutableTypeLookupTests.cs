using Xunit;

namespace GameSharp.Collections.Tests.TypeLookup;

public sealed class ImmutableTypeLookupTests(ImmutableTypeLookupTests.ClassFixture fixture) : 
    Tests<ImmutableTypeLookup>(fixture), 
    IClassFixture<ImmutableTypeLookupTests.ClassFixture>
{
    public sealed class ClassFixture : ClassFixture<ImmutableTypeLookup>
    {
        public override ImmutableTypeLookup WithSingleCar => ImmutableWithSingleCar;
        public override ImmutableTypeLookup WithAll => ImmutableWithAll;
        public override ImmutableTypeLookup Empty => [];
    }

    protected override bool WasMutatatedCorrectly(bool changeExpected, ImmutableTypeLookup initial, ImmutableTypeLookup result, out string? message)
    {
        if (changeExpected)
        {
            message = "Expected a new instance after mutation, but got the same instance.";
            return !ReferenceEquals(initial, result);
        }

        message = "Expected the same instance after mutation, but got a new instance.";
        return ReferenceEquals(initial, result);
    }

    protected override ImmutableTypeLookup Add<TItem>(ImmutableTypeLookup lookup, TItem item)
    {
        return lookup.Add(item);
    }

    protected override ImmutableTypeLookup Remove<TItem>(ImmutableTypeLookup lookup, TItem item)
    {
        return lookup.Remove(item);
    }

    protected override ImmutableTypeLookup Replace<TItem>(ImmutableTypeLookup lookup, TItem? existing, TItem replacement) where TItem : class
    {
        return lookup.Replace(existing, replacement);
    }
}