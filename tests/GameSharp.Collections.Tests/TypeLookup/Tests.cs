using System.Collections.Immutable;
using Xunit;

namespace GameSharp.Collections.Tests.TypeLookup;

public abstract class Tests
{
    protected sealed class KeyValuePairEqualityComparer : IEqualityComparer<KeyValuePair<Type, object>>
    {
        public static KeyValuePairEqualityComparer Default { get; } = new();

        public bool Equals(KeyValuePair<Type, object> x, KeyValuePair<Type, object> y)
        {
            return x.Key == y.Key && ReferenceEquals(x.Value, y.Value);
        }

        public int GetHashCode(KeyValuePair<Type, object> obj)
        {
            return HashCode.Combine(obj.Key, obj.Value);
        }
    }

    protected static ImmutableArray<object> AllInterfaceClasses { get; } = [Car.Default, Bicycle.Default, Aeroplane.Default];

    protected static ImmutableArray<object> AllDerivedClasses { get; } = [Apple.Default, Banana.Default, Cherry.Default, Fruit.Default];

    private static readonly KeyValuePair<Type, object>[] _allKeyValuePairs = [
        new KeyValuePair<Type, object>(typeof(Car), Car.Default),
        new KeyValuePair<Type, object>(typeof(Bicycle), Bicycle.Default),
        new KeyValuePair<Type, object>(typeof(Aeroplane), Aeroplane.Default),
        new KeyValuePair<Type, object>(typeof(Fruit), Fruit.Default),
        new KeyValuePair<Type, object>(typeof(Apple), Apple.Default),
        new KeyValuePair<Type, object>(typeof(Banana), Banana.Default),
        new KeyValuePair<Type, object>(typeof(Cherry), Cherry.Default)
    ];
    protected static HashSet<KeyValuePair<Type, object>> AllKeyValuePairs => new(_allKeyValuePairs, KeyValuePairEqualityComparer.Default);
}

public abstract class Tests<TLookup>(ClassFixture<TLookup> fixture) : Tests where TLookup : IReadOnlyTypeLookup
{
    [Fact(DisplayName = "Constructor Creates an Empty Collection")]
    public void Constructor()
    {
        TLookup collection = fixture.Empty;

        Assert.Empty(collection);
        Assert.False(collection.Contains(Car.Default));
    }

    [Fact(DisplayName = "Add Distinct Item")]
    public void AddDistinctItem()
    {
        TLookup initial = fixture.Empty;
        TLookup result = Add(initial, Car.Default);

        Assert.True(WasMutatatedCorrectly(changeExpected: true, initial, result, out string? message), message);
        Assert.Single<TLookup, Car>(result, Car.Default);
    }

    [Fact(DisplayName = "Add Duplicate Item")]
    public void AddDuplicateItem()
    {
        TLookup initial = fixture.WithSingleCar;
        TLookup result = Add(initial, Car.Default);

        Assert.True(WasMutatatedCorrectly(changeExpected: false, initial, result, out string? message), message);
        Assert.Single<TLookup, Car>(result, Car.Default);
    }

    [Fact(DisplayName = "Remove Existing Item")]
    public void RemoveExistingItem()
    {
        TLookup initial = fixture.WithSingleCar;
        TLookup result = Remove(initial, Car.Default);

        Assert.True(WasMutatatedCorrectly(changeExpected: true, initial, result, out string? message), message);
        Assert.Empty(result);
    }

    [Fact(DisplayName = "Remove Missing Item")]
    public void RemoveMissingItem()
    {
        TLookup initial = fixture.WithSingleCar;
        Apple weapon = new();
        TLookup result = Remove(initial, weapon);

        Assert.True(WasMutatatedCorrectly(changeExpected: false, initial, result, out string? message), message);
        Assert.Single<TLookup, Car>(result, Car.Default);
    }

    [Fact(DisplayName = "Get One Existing Item")]
    public void GetOneExistingItem()
    {
        TLookup initial = fixture.WithSingleCar;

        Assert.Same(initial.GetOne<Car>(), Car.Default);
        Assert.Same(initial.GetOne(typeof(Car)), Car.Default);
    }

    [Fact(DisplayName = "Get One Missing Item")]
    public void GetOneMissingItem()
    {
        Assert.GetOneThrowsForMissingItems<TLookup, Apple>(fixture.Empty);
        Assert.GetOneThrowsForMissingItems<TLookup, Apple>(fixture.WithSingleCar);
    }

    [Fact(DisplayName = "Try Get One Existing Item")]
    public void TryGetOneExistingItem()
    {
        TLookup initial = fixture.WithSingleCar;

        Assert.True(initial.TryGetOne(out Car? retreivedPlayer));
        Assert.Same(Car.Default, retreivedPlayer);

        Assert.True(initial.TryGetOne(typeof(Car), out object? retrievedObject));
        Assert.Same(Car.Default, retrievedObject);
    }

    [Fact(DisplayName = "Try Get One Missing Item")]
    public void TryGetOneMissingItem()
    {
        Assert.TryGetOneReturnsFalseForMissingItems<TLookup, Apple>(fixture.Empty);
        Assert.TryGetOneReturnsFalseForMissingItems<TLookup, Apple>(fixture.WithSingleCar);
    }

    [Fact(DisplayName = "Get All by Base Class Excludes Inheritors")]
    public void GetAllExistingByBaseClassExcludesInheritors()
    {
        TLookup withAll = fixture.WithAll;
        Assert.Single(withAll.GetAll(typeof(Fruit)), Fruit.Default);
        Assert.Single(withAll.GetAll<Fruit>(), Fruit.Default);
    }

    [Fact(DisplayName = "Get All by Interface")]
    public void GetAllExistingByInterface()
    {
        TLookup withAll = fixture.WithAll;
        Assert.SequenceEqualsUnordered([.. AllInterfaceClasses], withAll.GetAll(typeof(IVehicle)));
        Assert.SequenceEqualsUnordered([.. AllInterfaceClasses], withAll.GetAll<IVehicle>());
    }

    [Fact(DisplayName = "Get All by Base Class")]
    public void GetAllExistingByBaseClass()
    {
        TLookup withAll = fixture.WithAll;
        Assert.SequenceEqualsUnordered([.. AllDerivedClasses], withAll.GetAll(typeof(Fruit)));
        Assert.SequenceEqualsUnordered([.. AllDerivedClasses], withAll.GetAll<Fruit>());
    }

    [Fact(DisplayName = "Contains Returns True for Existing Items")]
    public void ContainsReturnsTrueForExistingItems()
    {
        TLookup withSingle = fixture.WithSingleCar;
        Assert.True(withSingle.Contains(Car.Default, typeof(Car)));
        Assert.True(withSingle.Contains(Car.Default));

        TLookup withAll = fixture.WithAll;
        Assert.True(withAll.Contains(Banana.Default, typeof(Banana)));
        Assert.True(withAll.Contains(Banana.Default));
    }

    [Fact(DisplayName = "Contains Returns True for Existing Items with Base Class")]
    public void ContainsReturnsTrueForExistingItemsWithBaseClass()
    {
        TLookup withAll = fixture.WithAll;
        Assert.True(withAll.Contains(Fruit.Default, typeof(Fruit)));
        Assert.True(withAll.Contains(Fruit.Default));
    }

    [Fact(DisplayName = "Contains Returns True for Existing Items with Interface")]
    public void ContainsReturnsTrueForExistingItemsWithInterface()
    {
        TLookup withAll = fixture.WithAll;
        Assert.True(withAll.Contains(Aeroplane.Default, typeof(IVehicle)));
        Assert.True(withAll.Contains(Aeroplane.Default));
    }

    [Fact(DisplayName = "ICollection Contains Uses Type and Item")]
    public void ICollectionContainsUsesTypeAndItem()
    {
        ICollection<KeyValuePair<Type, object>> withSingle = fixture.WithSingleCar;
        Assert.True(withSingle.Contains(new KeyValuePair<Type, object>(typeof(Car), Car.Default)));
        Assert.False(withSingle.Contains(new KeyValuePair<Type, object>(typeof(Car), new Car())));
    }

    [Fact(DisplayName = "ICollection CopyTo Copies All Items")]
    public void ICollectionCopyToCopiesAllItems()
    {
        ICollection<KeyValuePair<Type, object>> withAll = fixture.WithAll;
        KeyValuePair<Type, object>[] actual = new KeyValuePair<Type, object>[withAll.Count + 1];

        withAll.CopyTo(actual, 1);

        Assert.Equal(default, actual[0]);

        HashSet<KeyValuePair<Type, object>> expected = AllKeyValuePairs;
        foreach (KeyValuePair<Type, object> entry in actual.Skip(1))
        {
            Assert.Removed(expected, entry);
        }

        Assert.NoneRemaining(expected);
    }

    [Fact(DisplayName = "ITypeLookup Is Not ReadOnly")]
    public void ITypeLookupIsNotReadOnly()
    {
        Assert.Equal(!fixture.Empty.IsReadOnly, fixture.Empty is ITypeLookup);
    }

    [Fact(DisplayName = "ICollection Add and Remove Mutate TypeLookup")]
    public void ICollectionAddAndRemoveMutateTypeLookup()
    {
        TLookup lookup = fixture.Empty;
        ICollection<KeyValuePair<Type, object>> collection = lookup;
        KeyValuePair<Type, object> entry = new(typeof(Car), Car.Default);
        if (lookup is ITypeLookup)
        {
            collection.Add(entry);
            Assert.True(lookup.Contains(Car.Default, typeof(Car)));

            Assert.True(collection.Remove(entry));
            Assert.False(lookup.Contains(Car.Default, typeof(Car)));
        }
        else
        {
            Assert.Throws<NotSupportedException>(() => collection.Add(entry));
            Assert.Throws<NotSupportedException>(() => collection.Remove(entry));
            Assert.Throws<NotSupportedException>(collection.Clear);
        }
    }

    [Fact(DisplayName = "ICollection Clear Empties TypeLookup")]
    public void ICollectionClearEmptiesTypeLookup()
    {
        TLookup lookup = fixture.WithAll;
        ICollection<KeyValuePair<Type, object>> collection = lookup;
        if (lookup is ITypeLookup)
        {
            collection.Clear();
            Assert.Empty(lookup);
        }
        else
        {
            Assert.Throws<NotSupportedException>(collection.Clear);
        }
    }

    [Fact(DisplayName = "Replace Existing Item Updates Reference")]
    public void ReplaceExistingItemUpdatesReference()
    {
        Car replacement = new();
        TLookup original = fixture.WithSingleCar;
        TLookup updated = Replace(original, Car.Default, replacement);
        Assert.True(WasMutatatedCorrectly(changeExpected: true, original, updated, out string? message), message);
        Assert.Single<TLookup, Car>(updated, replacement);
    }

    [Fact(DisplayName = "Replace Missing Item Returns False")]
    public void ReplaceMissingItemReturnsFalse()
    {
        TLookup original = fixture.WithSingleCar;
        TLookup updated = Replace(original, Aeroplane.Default, new Aeroplane());
        Assert.True(WasMutatatedCorrectly(changeExpected: false, original, updated, out string? message), message);
        Assert.Single<TLookup, Car>(updated, Car.Default);
    }

    [Fact(DisplayName = "Replace with Null Existing Item Adds New Item")]
    public void ReplaceWithNullExistingItemAddsNewItem()
    {
        TLookup original = fixture.Empty;
        TLookup updated = Replace(original, null, Car.Default);
        Assert.True(WasMutatatedCorrectly(changeExpected: true, original, updated, out string? message), message);
        Assert.Single<TLookup, Car>(updated, Car.Default);
    }

    [Fact(DisplayName = "Enumerator Iterates Over All Items")]
    public void EnumeratorIteratesOverAllExistingItems()
    {
        HashSet<KeyValuePair<Type, object>> expected = AllKeyValuePairs;

        IEnumerator<KeyValuePair<Type, object>> enumerator = fixture.WithAll.GetEnumerator();
        while (enumerator.MoveNext())
        {
            Assert.Removed(expected, enumerator.Current);
        }

        Assert.NoneRemaining(expected);
    }

    [Fact(DisplayName = "EnumeratorImpl Iterates Over All Items")]
    public void EnumeratorImplIteratesOverAllExistingItems()
    {
        HashSet<KeyValuePair<Type, object>> expected = AllKeyValuePairs;

        using IEnumerator<KeyValuePair<Type, object>> enumerator = fixture.WithAll.GetEnumerator();
        while (enumerator.MoveNext())
        {
            Assert.Removed(expected, enumerator.Current);
        }

        Assert.NoneRemaining(expected);
    }

    protected virtual bool WasMutatatedCorrectly(bool changeExpected, TLookup initial, TLookup result, out string? message)
    {
        if (!changeExpected || ReferenceEquals(initial, result))
        {
            message = null;
            return true;
        }

        message = "The collection was not modified in place.";
        return false;
    }

    protected abstract TLookup Add<TItem>(TLookup lookup, TItem item) where TItem : class;

    protected abstract TLookup Remove<TItem>(TLookup lookup, TItem item) where TItem : class;

    protected abstract TLookup Replace<TItem>(TLookup lookup, TItem? existing, TItem replacement) where TItem : class;
}
