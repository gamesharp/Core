using Xunit;

namespace GameSharp.Collections.Tests.TypeLookup;

public interface ITheoryDataProvider<T> where T : IReadOnlyTypeLookup
{
    static abstract TheoryData<T> WithSingleCar { get; }

    static abstract TheoryData<T> WithAll { get; }

    static abstract TheoryData<T> Empty { get; }
}
