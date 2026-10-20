using System.Buffers;
using Xunit;

namespace GameSharp.Collections.Tests.PooledList;

public sealed class PooledMemoryOwnerTests
{
    [Fact]
    public void ToMemoryOwner_TransfersOwnershipAndPreservesItems()
    {
        ReadOnlySpan<int> items = [1, 2, 3];
        PooledList<int> list = Collections.PooledList.Empty<int>(initialCapacity: 4);
        list.AddRange(items);
        using IMemoryOwner<int> owner = list.ToMemoryOwner();
        Assert.Equal(items, owner.Memory.Span);
    }

    [Fact]
    public void GetSpan_ForReferenceType_PreservesReferences()
    {
        string[] items = ["a", "b", "c"];
        PooledList<string> list = Collections.PooledList.Empty<string>(initialCapacity: 3);
        list.AddRange(items);
        using IMemoryOwner<string> owner = list.ToMemoryOwner();
        Assert.Equal(items, owner.Memory.Span);
    }

    [Fact]
    public void Pin_ForReferenceType_ThrowsNotSupportedException()
    {
        string[] items = ["x", "y"];
        PooledList<string> list = Collections.PooledList.Empty<string>(initialCapacity: 2);
        list.AddRange(items);

        using IMemoryOwner<string> owner = list.ToMemoryOwner();
        MemoryManager<string> manager = Assert.IsType<MemoryManager<string>>(owner, exactMatch: false);

        Assert.Throws<NotSupportedException>(() => manager.Pin());
    }

    [Fact]
    public void Pin_ForValueType_AllowsMultiplePinsAndOutOfRangeThrows()
    {
        PooledList<int> list = Collections.PooledList.Empty<int>(initialCapacity: 4);
        list.AddRange([10, 20, 30]);

        using IMemoryOwner<int> owner = list.ToMemoryOwner();
        MemoryManager<int> manager = Assert.IsAssignableFrom<MemoryManager<int>>(owner);

        using MemoryHandle first = manager.Pin(0);
        using MemoryHandle second = manager.Pin(1);

        Assert.Throws<ArgumentOutOfRangeException>(() => manager.Pin(4));
    }

    [Fact]
    public void Dispose_MakesMemoryUnavailable()
    {
        PooledList<int> list = Collections.PooledList.Empty<int>(initialCapacity: 2);
        list.AddRange([7, 8]);

        IMemoryOwner<int> owner = list.ToMemoryOwner();
        owner.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
        {
            _ = owner.Memory.Span;
        });
    }
}