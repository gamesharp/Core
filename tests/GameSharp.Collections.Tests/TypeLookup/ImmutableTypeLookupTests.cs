using Xunit;

namespace GameSharp.Collections.Tests.TypeLookup;

public sealed class ImmutableTypeLookupTests : Tests<ImmutableTypeLookup, ImmutableTypeLookupTests>, ITheoryDataProvider<ImmutableTypeLookup>
{
    public static TheoryData<ImmutableTypeLookup> WithSingleCar => [ImmutableWithSingleCar];

    public static TheoryData<ImmutableTypeLookup> WithAll => [ImmutableWithAll];

    public static TheoryData<ImmutableTypeLookup> Empty => [[]];

    public override void EnumeratorIteratesOverAllExistingItems(ImmutableTypeLookup inital)
    {
        HashSet<KeyValuePair<Type, object>> expected = AllKeyValuePairs;

        ImmutableTypeLookup.Enumerator enumerator = inital.GetEnumerator();
        while (enumerator.MoveNext())
        {
            Assert.Removed(expected, enumerator.Current);
        }

        Assert.NoneRemaining(expected);
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