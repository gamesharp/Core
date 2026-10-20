namespace GameSharp.Collections.Tests.TypeLookup;

public abstract class ClassFixture
{
    protected internal static ImmutableTypeLookup ImmutableWithSingleCar { get; }

    protected internal static ImmutableTypeLookup ImmutableWithAll { get; }

    static ClassFixture()
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

public abstract class ClassFixture<T> : ClassFixture where T : IReadOnlyTypeLookup
{
    public abstract T WithSingleCar { get; }

    public abstract T WithAll { get; }

    public abstract T Empty { get; }
}
