using Xunit;

namespace GameSharp.Collections.Tests.TypeLookup;

public sealed class TypeLookupTests : Tests<Collections.TypeLookup, TypeLookupTests>, ITheoryDataProvider<Collections.TypeLookup>
{
    public static TheoryData<Collections.TypeLookup> WithSingleCar => [ImmutableWithSingleCar.AsTypeLookup()];

    public static TheoryData<Collections.TypeLookup> WithAll => [ImmutableWithAll.AsTypeLookup()];

    public static TheoryData<Collections.TypeLookup> Empty => [[]];

    public override void EnumeratorIteratesOverAllExistingItems(Collections.TypeLookup initial)
    {
        HashSet<KeyValuePair<Type, object>> expected = AllKeyValuePairs;

        Collections.TypeLookup.Enumerator enumerator = initial.GetEnumerator();
        while (enumerator.MoveNext())
        {
            Assert.Removed(expected, enumerator.Current);
        }

        Assert.NoneRemaining(expected);
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
