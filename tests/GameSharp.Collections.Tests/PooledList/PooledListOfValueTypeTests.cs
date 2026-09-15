namespace GameSharp.Collections.Tests.PooledList;

public sealed class PooledListOfValueTypeTests : Tests<int>
{
    private static readonly int[] _testItems = [..Enumerable.Range(1, 100)];
    protected override int[] TestItems => _testItems;
}
