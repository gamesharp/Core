using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace GameSharp;

public abstract class ObjectPool<TItem> where TItem : class
{
    private const int DisposedSentinel = -1;
    private static readonly int _maxCapacity = (Environment.ProcessorCount * 2) - 1; // array + fast item
    private readonly TItem?[] _items;
    private TItem? _fastItem;
    private int _count;

    public ObjectPool(int maxiumumRetained)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxiumumRetained);
        _items = new TItem?[maxiumumRetained];
    }

    public ObjectPool()
    {
        _items = new TItem?[_maxCapacity];
    }

    public TItem Get()
    {
        if (Interlocked.Exchange(ref _fastItem, null) is not { } item)
        {
            item = GetFromArray();
        }

        return item;
    }

    public void Return(TItem? item)
    {
        if (!TryReset(item))
        {
            Discard(item);
        }
        else if (Interlocked.CompareExchange(ref _fastItem, item, null) is { })
        {
            ReturnToArray(item);
        }
    }

    protected abstract TItem Create();

    protected abstract bool TryReset([NotNullWhen(true)] TItem? item);

    protected virtual void Dispose(bool disposing)
    {
        int count = Interlocked.Exchange(ref _count, DisposedSentinel);

        if (!disposing || count == DisposedSentinel)
        {
            return;
        }

        foreach (object? item in _items.AsSpan(0, count - 1))
        {
            Discard(item);
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    [SkipLocalsInit]
    private TItem GetFromArray()
    {
        int newCount, currCount = Volatile.Read(ref _count), prevCount;
        do
        {
            ObjectDisposedException.ThrowIf(_count == -1, this);

            if (currCount == 0)
            {
                return Create();
            }

            newCount = currCount - 1;
            (currCount, prevCount) = (Interlocked.CompareExchange(ref _count, newCount, currCount), currCount);
        }
        while (currCount != prevCount);
        return Interlocked.Exchange(ref _items[newCount], null)!;
    }

    [SkipLocalsInit]
    private void ReturnToArray(TItem? item)
    {
        int currCount = Volatile.Read(ref _count), prevCount;

        while (currCount != _items.Length)
        {
            ObjectDisposedException.ThrowIf(_count == DisposedSentinel, this);

            if (Interlocked.CompareExchange(ref _items[currCount], item, null) is { })
            {
                currCount = Volatile.Read(ref _count);
                continue;
            }

            (currCount, prevCount) = (Interlocked.CompareExchange(ref _count, currCount + 1, currCount), currCount);
            if (currCount == prevCount)
            {
                return;
            }
        }

        Discard(item);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Discard(object? obj)
    {
        if (obj is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}