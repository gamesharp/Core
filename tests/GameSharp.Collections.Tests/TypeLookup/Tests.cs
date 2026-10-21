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

    protected internal static ImmutableTypeLookup ImmutableWithSingleCar { get; }

    protected internal static ImmutableTypeLookup ImmutableWithAll { get; }

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

    static Tests()
    {
        ImmutableTypeLookup empty = [];
        ImmutableWithSingleCar = empty.Add(Car.Default);

        ImmutableTypeLookup.Builder builder = ImmutableTypeLookup.CreateBuilder();

        // Interface inheritance test classes (IVehicle)
        builder.Add(Car.Default);
        builder.Add(Bicycle.Default);
        builder.Add(Aeroplane.Default);

        // Class inheritance test classes (Fruit)
        builder.Add(Fruit.Default);
        builder.Add(Apple.Default);
        builder.Add(Banana.Default);
        builder.Add(Cherry.Default);

        ImmutableWithAll = builder.ToImmutable();
    }
}

public abstract class Tests<TLookup, TSelf> : Tests 
    where TLookup : IReadOnlyTypeLookup
    where TSelf : ITheoryDataProvider<TLookup>
{
#pragma warning disable xUnit1015 // MemberData must reference an existing member
    [Theory(DisplayName = "Constructor Creates an Empty Collection"), MemberData("Empty")]
    public void Constructor(TLookup inital)
    {
        try
        {
            Assert.Empty(inital);
            Assert.False(inital.Contains(Car.Default));
        }
        finally
        {
            if (inital is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    [Theory(DisplayName = "Add Distinct Item"), MemberData("Empty")]
    public void AddDistinctItem(TLookup initial)
    {
        try
        {
            TLookup result = Add(initial, Car.Default);

            Assert.True(WasMutatatedCorrectly(changeExpected: true, initial, result, out string? message), message);
            Assert.Single<TLookup, Car>(result, Car.Default);
        }
        finally
        {
            if (initial is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    [Theory(DisplayName = "Add Duplicate Item"), MemberData("WithSingleCar")]
    public void AddDuplicateItem(TLookup initial)
    {
        try
        {
            TLookup result = Add(initial, Car.Default);

            Assert.True(WasMutatatedCorrectly(changeExpected: false, initial, result, out string? message), message);
            Assert.Single<TLookup, Car>(result, Car.Default);
        }
        finally
        {
            if (initial is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    [Theory(DisplayName = "Remove Existing Item"), MemberData("WithSingleCar")]
    public void RemoveExistingItem(TLookup initial)
    {
        try
        {
            TLookup result = Remove(initial, Car.Default);

            Assert.True(WasMutatatedCorrectly(changeExpected: true, initial, result, out string? message), message);
            Assert.Empty(result);
        }
        finally
        {
            if (initial is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    [Theory(DisplayName = "Remove Missing Item"), MemberData("WithSingleCar")]
    public void RemoveMissingItem(TLookup initial)
    {
        try
        {
            TLookup result = Remove(initial, Apple.Default);
            Assert.True(WasMutatatedCorrectly(changeExpected: false, initial, result, out string? message), message);
            Assert.Single<TLookup, Car>(result, Car.Default);
        }
        finally
        {
            if (initial is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    [Theory(DisplayName = "Get One Existing Item"), MemberData("WithSingleCar")]
    public void GetOneExistingItem(TLookup initial)
    {
        try
        {
            Assert.Same(initial.GetOne<Car>(), Car.Default);
            Assert.Same(initial.GetOne(typeof(Car)), Car.Default);
        }
        finally
        {
            if (initial is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    [Theory(DisplayName = "Get One Missing Item With Empty"), MemberData("Empty")]
    public void GetOneMissingItemWithEmpty(TLookup initial)
    {
        try
        {
            Assert.GetOneThrowsForMissingItems<TLookup, Apple>(initial);
        }
        finally
        {
            if (initial is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    [Theory(DisplayName = "Get One Missing Item With Non-Empty"), MemberData("WithSingleCar")]
    public void GetOneMissingItemWithNonEmpty(TLookup initial)
    {
        try
        {
            Assert.GetOneThrowsForMissingItems<TLookup, Apple>(initial);
        }
        finally
        {
            if (initial is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    [Theory(DisplayName = "Try Get One Existing Item"), MemberData("WithSingleCar")]
    public void TryGetOneExistingItem(TLookup initial)
    {
        try
        {
            Assert.True(initial.TryGetOne(out Car? retreivedPlayer));
            Assert.Same(Car.Default, retreivedPlayer);

            Assert.True(initial.TryGetOne(typeof(Car), out object? retrievedObject));
            Assert.Same(Car.Default, retrievedObject);
        }
        finally
        {
            if (initial is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    [Theory(DisplayName = "Try Get One Missing Item With Empty"), MemberData("Empty")]
    public void TryGetOneMissingItemWithEmpty(TLookup initial)
    {
        try
        {
            Assert.TryGetOneReturnsFalseForMissingItems<TLookup, Apple>(initial);
        }
        finally
        {
            if (initial is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    [Theory(DisplayName = "Try Get One Missing Item With Non-Empty"), MemberData("WithSingleCar")]
    public void TryGetOneMissingItemWithNonEmpty(TLookup initial)
    {
        try
        {
            Assert.TryGetOneReturnsFalseForMissingItems<TLookup, Apple>(initial);
        }
        finally
        {
            if (initial is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    [Theory(DisplayName = "Get All by Base Class Excludes Inheritors"), MemberData("WithAll")]
    public void GetAllExistingByBaseClassExcludesInheritors(TLookup inital)
    {
        try
        {
            Assert.Single(inital.GetAll(typeof(Fruit)), Fruit.Default);
            Assert.Single(inital.GetAll<Fruit>(), Fruit.Default);
        }
        finally
        {
            if (inital is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    [Theory(DisplayName = "Get All by Interface"), MemberData("WithAll")]
    public void GetAllExistingByInterface(TLookup inital)
    {
        try
        {
            Assert.SequenceEqualsUnordered([.. AllInterfaceClasses], inital.GetAll(typeof(IVehicle)));
            Assert.SequenceEqualsUnordered([.. AllInterfaceClasses], inital.GetAll<IVehicle>());
        }
        finally
        {
            if (inital is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    [Theory(DisplayName = "Get All by Base Class"), MemberData("WithAll")]
    public void GetAllExistingByBaseClass(TLookup inital)
    {
        try
        {
            Assert.SequenceEqualsUnordered([.. AllDerivedClasses], inital.GetAll(typeof(Fruit)));
            Assert.SequenceEqualsUnordered([.. AllDerivedClasses], inital.GetAll<Fruit>());
        }
        finally
        {
            if (inital is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    [Theory(DisplayName = "Contains Returns True for Existing Items With One"), MemberData("WithSingleCar")]
    public void ContainsReturnsTrueForExistingItemsWithOne(TLookup inital)
    {
        try
        {
            Assert.True(inital.Contains(Car.Default, typeof(Car)));
            Assert.True(inital.Contains(Car.Default));
        }
        finally
        {
            if (inital is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    [Theory(DisplayName = "Contains Returns True for Existing Items With All"), MemberData("WithAll")]
    public void ContainsReturnsTrueForExistingItemsWithAll(TLookup inital)
    {
        try
        {
            Assert.True(inital.Contains(Apple.Default, typeof(Apple)));
            Assert.True(inital.Contains(Apple.Default));
        }
        finally
        {
            if (inital is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    [Theory(DisplayName = "Contains Returns True for Existing Items with Base Class"), MemberData("WithAll")]
    public void ContainsReturnsTrueForExistingItemsWithBaseClass(TLookup inital)
    {
        try
        {
            Assert.True(inital.Contains(Fruit.Default, typeof(Fruit)));
            Assert.True(inital.Contains(Fruit.Default));
        }
        finally
        {
            if (inital is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    [Theory(DisplayName = "Contains Returns True for Existing Items with Interface"), MemberData("WithAll")]
    public void ContainsReturnsTrueForExistingItemsWithInterface(TLookup inital)
    {
        try
        {
            Assert.True(inital.Contains(Aeroplane.Default, typeof(IVehicle)));
            Assert.True(inital.Contains(Aeroplane.Default));
        }
        finally
        {
            if (inital is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    [Theory(DisplayName = "ICollection Contains Uses Type and Item"), MemberData("WithSingleCar")]
    public void ICollectionContainsUsesTypeAndItem(TLookup inital)
    {
        try
        {
            ICollection<KeyValuePair<Type, object>> withSingle = inital;
            Assert.True(withSingle.Contains(new KeyValuePair<Type, object>(typeof(Car), Car.Default)));
            Assert.False(withSingle.Contains(new KeyValuePair<Type, object>(typeof(Car), new Car())));
        }
        finally
        {
            if (inital is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    [Theory(DisplayName = "ICollection CopyTo Copies All Items"), MemberData("WithAll")]
    public void ICollectionCopyToCopiesAllItems(TLookup inital)
    {
        try
        {
            ICollection<KeyValuePair<Type, object>> withAll = inital;
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
        finally
        {
            if (inital is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    [Theory(DisplayName = "ITypeLookup Is Not ReadOnly"), MemberData("WithAll")]
    public void ITypeLookupIsNotReadOnly(TLookup inital)
    {
        try
        {
            Assert.Equal(!inital.IsReadOnly, inital is ITypeLookup);
        }
        finally
        {
            if (inital is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    [Theory(DisplayName = "ICollection Add and Remove Mutate TypeLookup"), MemberData("Empty")]
    public void ICollectionAddAndRemoveMutateTypeLookup(TLookup inital)
    {
        try
        {
            TLookup lookup = inital;
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
        finally
        {
            if (inital is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    [Theory(DisplayName = "ICollection Clear Empties TypeLookup"), MemberData("WithAll")]
    public void ICollectionClearEmptiesTypeLookup(TLookup inital)
    {
        try
        {
            TLookup lookup = inital;
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
        finally
        {
            if (inital is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    [Theory(DisplayName = "Replace Existing Item Updates Reference"), MemberData("WithSingleCar")]
    public void ReplaceExistingItemUpdatesReference(TLookup inital)
    {
        try
        {
            Car replacement = new();
            TLookup original = inital;
            TLookup updated = Replace(original, Car.Default, replacement);
            Assert.True(WasMutatatedCorrectly(changeExpected: true, original, updated, out string? message), message);
            Assert.Single<TLookup, Car>(updated, replacement);

        }
        finally
        {
            if (inital is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    [Theory(DisplayName = "Replace Missing Item Returns False"), MemberData("WithSingleCar")]
    public void ReplaceMissingItemReturnsFalse(TLookup inital)
    {
        try
        {
            TLookup original = inital;
            TLookup updated = Replace(original, Aeroplane.Default, new Aeroplane());
            Assert.True(WasMutatatedCorrectly(changeExpected: false, original, updated, out string? message), message);
            Assert.Single<TLookup, Car>(updated, Car.Default);
        }
        finally
        {
            if (inital is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    [Theory(DisplayName = "Replace with Null Existing Item Adds New Item"), MemberData("Empty")]
    public void ReplaceWithNullExistingItemAddsNewItem(TLookup inital)
    {
        try
        {
            TLookup original = inital;
            TLookup updated = Replace(original, null, Car.Default);
            Assert.True(WasMutatatedCorrectly(changeExpected: true, original, updated, out string? message), message);
            Assert.Single<TLookup, Car>(updated, Car.Default);
        }
        finally
        {
            if (inital is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    [Theory(DisplayName = "Enumerator Iterates Over All Items"), MemberData("WithAll")]
    public abstract void EnumeratorIteratesOverAllExistingItems(TLookup inital);

    [Theory(DisplayName = "EnumeratorImpl Iterates Over All Items"), MemberData("WithAll")]
#pragma warning restore xUnit1015 // MemberData must reference an existing member
    public void EnumeratorImplIteratesOverAllExistingItems(TLookup inital)
    {
        try
        {
            HashSet<KeyValuePair<Type, object>> expected = AllKeyValuePairs;

            using IEnumerator<KeyValuePair<Type, object>> enumerator = inital.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Removed(expected, enumerator.Current);
            }

            Assert.NoneRemaining(expected);
        }
        finally
        {
            if (inital is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
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
