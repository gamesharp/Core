using System.Runtime.CompilerServices;
using Xunit;

namespace GameSharp.Collections.Tests.PooledList;

public abstract class Tests<T>
{
    protected const int DefaultCapacity = 4;

    protected abstract T[] TestItems { get; }

    protected virtual IEqualityComparer<T?> Comparer => EqualityComparer<T?>.Default;

    [Fact]
    public void Constructor()
    {
        using PooledList<T> items = Collections.PooledList.Empty<T>();
        Assert.Equal(0, items.Count);
    }

    [Fact]
    public void Add()
    {
        PooledList<T> items = Collections.PooledList.Empty<T>();

        try
        {
            int targetCount = GetArray(ref items)!.Length + 1, i = 0;

            for (; i < targetCount; i++)
            {
                items.Add(TestItems[i % TestItems.Length]);
            }

            i = 0;
            foreach (T item in items)
            {
                Assert.True(Comparer.Equals(item, TestItems[i++ % TestItems.Length]));
            }
        }
        finally
        {
            items.Dispose();
        }
    }

    [Fact]
    public void RemoveAt()
    {
        List<T> expectedItems = [.. TestItems];
        using PooledList<T> items = expectedItems.ToPooledList();

        items.RemoveAt(items.Count - 1);
        expectedItems.RemoveAt(expectedItems.Count - 1);
        Assert.SequenceEqual(in items, expectedItems, Comparer);

        int mid = TestItems.Length / 2;
        items.RemoveAt(mid);
        expectedItems.RemoveAt(mid);
        Assert.SequenceEqual(in items, expectedItems, Comparer);

        items.RemoveAt(0);
        expectedItems.RemoveAt(0);
        Assert.SequenceEqual(in items, expectedItems, Comparer);
    }

    [Fact]
    public void Remove()
    {
        List<T> expectedItems = [.. TestItems];
        PooledList<T> items = expectedItems.ToPooledList();

        try
        {
            items.Remove(expectedItems[^1], Comparer);
            expectedItems.RemoveAt(expectedItems.Count - 1);
            Assert.SequenceEqual(in items, expectedItems, Comparer);

            int mid = TestItems.Length / 2;
            items.Remove(expectedItems[mid], Comparer);
            expectedItems.RemoveAt(mid);
            Assert.SequenceEqual(in items, expectedItems, Comparer);

            items.Remove(expectedItems[0], Comparer);
            expectedItems.RemoveAt(0);
            Assert.SequenceEqual(in items, expectedItems, Comparer);
        }
        finally
        {
            items.Dispose();
        }
    }

    [Fact]
    public void Clear()
    {
        using PooledList<T> items = TestItems.ToPooledList();
        items.Clear();
        Assert.Equal(0, items.Count);
    }

    [Fact]
    public void Insert()
    {
        using PooledList<T> items = Collections.PooledList.Empty<T>(TestItems.Length);
        int mid = TestItems.Length / 2;

        foreach (T item in TestItems.AsSpan(0, mid))
        {
            items.Add(item);
        }

        for (int i = 0; i < TestItems.Length - mid; i++)
        {
            T? item = TestItems[mid + i];
            items.Insert(item, i);
            Assert.Equal(items.IndexOf(item, Comparer), i);
        }
    }

    [Fact]
    public void Contains()
    {
        PooledList<T> items = Collections.PooledList.Empty<T>(0);
        items.Add(TestItems[0]);
        Assert.True(items.Contains(TestItems[0]));
    }

    [Fact]
    public void Indexer()
    {
        using PooledList<T> items = TestItems.ToPooledList();

        for (int i = 0; i < TestItems.Length; i++)
        {
            Assert.True(Comparer.Equals(items[i], TestItems[i]));
        }

        Assert.Throws<ArgumentOutOfRangeException>(AccessNegativeIndex);
        Assert.Throws<ArgumentOutOfRangeException>(AccessIndexOutOfRange);
    }

    [Fact]
    public void Dispose()
    {
        Assert.Throws<ObjectDisposedException>(AddUninitializedList);
        Assert.Throws<ObjectDisposedException>(RemoveAtDisposedList);

        PooledList<T> items = Collections.PooledList.Empty<T>(initialCapacity: 0);
        items.Dispose();
        Assert.Null(GetArray(ref items));
    }

    private void AddUninitializedList()
    {
        PooledList<T> items = default;
        items.Add(TestItems[0]);
    }

    private void RemoveAtDisposedList()
    {
        PooledList<T> items = Collections.PooledList.Empty<T>(initialCapacity: 0);
        items.Dispose();
        items.RemoveAt(0);
    }

    private void AccessNegativeIndex()
    {
        using PooledList<T> items = TestItems.ToPooledList();
        _ = items[-1];
    }

    private void AccessIndexOutOfRange()
    {
        using PooledList<T> items = TestItems.ToPooledList();
        _ = items[TestItems.Length];
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "array")]
    private static extern ref Array? GetArray(scoped ref PooledList<T> list);
}
