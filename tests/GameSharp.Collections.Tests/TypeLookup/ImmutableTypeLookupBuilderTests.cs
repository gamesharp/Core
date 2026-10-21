using System.Collections.Concurrent;
using Xunit;

namespace GameSharp.Collections.Tests.TypeLookup;

public sealed class ImmutableTypeLookupBuilderTests : 
    Tests<ImmutableTypeLookup.Builder, ImmutableTypeLookupBuilderTests>, 
    ITheoryDataProvider<ImmutableTypeLookup.Builder>
{
    public static TheoryData<ImmutableTypeLookup.Builder> WithSingleCar => [ImmutableWithSingleCar.AsImmutableBuilder()];

    public static TheoryData<ImmutableTypeLookup.Builder> WithAll => [ImmutableWithAll.AsImmutableBuilder()];

    public static TheoryData<ImmutableTypeLookup.Builder> Empty => [ImmutableTypeLookup.CreateBuilder()];

    public override void EnumeratorIteratesOverAllExistingItems(ImmutableTypeLookup.Builder initial)
    {
        HashSet<KeyValuePair<Type, object>> expected = AllKeyValuePairs;

        Collections.TypeLookup.Enumerator enumerator = initial.GetEnumerator();
        while (enumerator.MoveNext())
        {
            Assert.Removed(expected, enumerator.Current);
        }

        Assert.NoneRemaining(expected);
    }

    [Fact(DisplayName = "ToImmutable Returns a ImmutableTypeLookup Corresponding to the Builder's Content")]
    public void ToImmutableReturnsImmutableWithCorrespondingContent()
    {
        ImmutableTypeLookup withAll = ImmutableWithAll;
        ImmutableTypeLookup.Builder builder = withAll.AsImmutableBuilder();

        HashSet<KeyValuePair<Type, object>> expected = AllKeyValuePairs;

        foreach (KeyValuePair<Type, object> kvp in builder.ToImmutable())
        {
            Assert.Removed(expected, kvp);
        }

        Assert.NoneRemaining(expected);
    }

    [Fact(DisplayName = "ToImmutable Cannot Be Called More Than Once")]
    public async Task ToImmutableCannotBeCalledMoreThanOnce()
    {
        const int ThreadCount = 10;
        ImmutableTypeLookup.Builder builder = ImmutableWithSingleCar.AsImmutableBuilder();
        ConcurrentBag<Exception> exceptions = [];
        using (Barrier barrier = new(ThreadCount))
        {
            void Build()
            {
                barrier.SignalAndWait();
                try
                {
                    builder.ToImmutable();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            Task[] tasks = new Task[ThreadCount];
            foreach (ref Task task in tasks.AsSpan())
            {
                task = Task.Run(Build);
            }

            await Task.WhenAll(tasks);
        }

        Assert.Equal(ThreadCount - 1, exceptions.Count(static ex => ex is InvalidOperationException));
    }

    protected override ImmutableTypeLookup.Builder Add<TItem>(ImmutableTypeLookup.Builder lookup, TItem item)
    {
        lookup.Add(item);
        return lookup;
    }

    protected override ImmutableTypeLookup.Builder Remove<TItem>(ImmutableTypeLookup.Builder lookup, TItem item)
    {
        lookup.Remove(item);
        return lookup;
    }

    protected override ImmutableTypeLookup.Builder Replace<TItem>(ImmutableTypeLookup.Builder lookup, TItem? existing, TItem replacement) where TItem : class
    {
        lookup.Replace(existing, replacement);
        return lookup;
    }
}
