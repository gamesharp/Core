using GameSharp.Collections.Concurrent;
using Xunit;

namespace GameSharp.Collections.Tests.TypeLookup;

public sealed class ConcurrentTypeLookupTests : Tests<ConcurrentTypeLookup, ConcurrentTypeLookupTests>, ITheoryDataProvider<ConcurrentTypeLookup>
{
    public static TheoryData<ConcurrentTypeLookup> WithSingleCar => PublishedAndUnpublished(ImmutableWithSingleCar);

    public static TheoryData<ConcurrentTypeLookup> WithAll => PublishedAndUnpublished(ImmutableWithAll);

    public static TheoryData<ConcurrentTypeLookup> Empty => [[]];

    public override void EnumeratorIteratesOverAllExistingItems(ConcurrentTypeLookup inital)
    {
        HashSet<KeyValuePair<Type, object>> expected = AllKeyValuePairs;

        using ConcurrentTypeLookup.Enumerator enumerator = inital.GetEnumerator();
        while (enumerator.MoveNext())
        {
            Assert.Removed(expected, enumerator.Current);
        }

        Assert.NoneRemaining(expected);
    }

    protected override ConcurrentTypeLookup Add<TItem>(ConcurrentTypeLookup lookup, TItem item)
    {
        lookup.Add(item);
        return lookup;
    }

    protected override ConcurrentTypeLookup Remove<TItem>(ConcurrentTypeLookup lookup, TItem item)
    {
        lookup.Remove(item);
        return lookup;
    }

    protected override ConcurrentTypeLookup Replace<TItem>(ConcurrentTypeLookup lookup, TItem? existing, TItem replacement) where TItem : class
    {
        lookup.Replace(existing, replacement);
        return lookup;
    }

    private static TheoryData<ConcurrentTypeLookup> PublishedAndUnpublished(ImmutableTypeLookup source)
    {
        ConcurrentTypeLookup published = source.AsConcurrentTypeLookup();
        published.Publish();
        return [source.AsConcurrentTypeLookup(), published];
    }
}
