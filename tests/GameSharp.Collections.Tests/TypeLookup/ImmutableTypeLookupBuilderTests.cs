using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Xunit;

namespace GameSharp.Collections.Tests.TypeLookup;

public sealed class ImmutableTypeLookupBuilderTests(ImmutableTypeLookupBuilderTests.ClassFixture fixture) :
    Tests<ImmutableTypeLookup.Builder>(fixture),
    IClassFixture<ImmutableTypeLookupBuilderTests.ClassFixture> 
{
    public sealed class ClassFixture : ClassFixture<ImmutableTypeLookup.Builder>
    {
        public override ImmutableTypeLookup.Builder WithSingleCar => ImmutableWithSingleCar.AsImmutableBuilder();
        public override ImmutableTypeLookup.Builder WithAll => ImmutableWithAll.AsImmutableBuilder();
        public override ImmutableTypeLookup.Builder Empty => ImmutableTypeLookup.CreateBuilder();
    }

    [Fact(DisplayName = "ToImmutable Returns a ImmutableTypeLookup Corresponding to the Builder's Content")]
    public void ToImmutableReturnsImmutableWithCorrespondingContent()
    {
        ImmutableTypeLookup withAll = TypeLookup.ClassFixture.ImmutableWithAll;
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
        ImmutableTypeLookup.Builder builder = TypeLookup.ClassFixture.ImmutableWithSingleCar.AsImmutableBuilder();
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
