using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace GameSharp;

public abstract partial class Worker : AsynchronousLifecycle
{
    private const string DefaultThreadName = "Unnamed";
    protected internal abstract Thread Thread { get; }
    protected virtual ILogger Logger => NullLogger.Instance;
    private BlockingCollection<EnqueuedWorkItem>[] _workItems;
    private readonly BlockingCollection<EnqueuedWorkItem> _queue;

    private readonly WorkerSynchronizationContext _synchronizationContext;
    public SynchronizationContext SynchronizationContext => _synchronizationContext;

    private readonly WorkerTaskScheduler _taskScheduler;
    public TaskScheduler TaskScheduler => _taskScheduler;

    public Worker(BlockingCollection<EnqueuedWorkItem> queue)
    {
        ArgumentNullException.ThrowIfNull(queue);
        RegisterCompleteAdding(queue, StoppingToken);
        _queue = queue;

        BlockingCollection<EnqueuedWorkItem> syncCtxWorkItems = [];
        CancellationTokenRegistration syncCtxReg = RegisterCompleteAdding(syncCtxWorkItems, StoppingToken);
        _synchronizationContext = new WorkerSynchronizationContext(syncCtxWorkItems, this, syncCtxReg);

        BlockingCollection<EnqueuedWorkItem> taskSchedulerWorkItems = [];
        CancellationTokenRegistration taskSchedulerReg = RegisterCompleteAdding(taskSchedulerWorkItems, StoppingToken);
        _taskScheduler = new WorkerTaskScheduler(Thread.ManagedThreadId, taskSchedulerWorkItems, taskSchedulerReg);

        _workItems = [queue, syncCtxWorkItems, taskSchedulerWorkItems];
    }

    /// <inheritdoc/>
    override protected Task OnStartingAsync(CancellationToken cancellationToken)
    {
        Thread.Start();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override async Task OnStoppingAsync(CancellationToken cancellationToken)
    {
        Thread.Join();
    }

    /// <inheritdoc/>
    protected override void OnDisposing()
    {
        base.OnDisposing();
        LogThreadDisposed(Logger, Thread.Name ?? DefaultThreadName, Thread.ManagedThreadId);
        _synchronizationContext.Dispose();
        _taskScheduler.Dispose();
        foreach (BlockingCollection<EnqueuedWorkItem> workItems in _workItems)
        {
            workItems.Dispose();
        }
    }

    protected virtual void DoWork()
    {
        string threadName = Thread.Name ?? DefaultThreadName;
        LogThreadStarted(Logger, threadName, Thread.ManagedThreadId);
        SynchronizationContext.SetSynchronizationContext(_synchronizationContext);
        try
        {
            while (BlockingCollection<EnqueuedWorkItem>.TryTakeFromAny(_workItems, out EnqueuedWorkItem workItem, -1) != -1)
            {
                try
                {
                    workItem.Callback(workItem.State);
                }
                catch (Exception ex)
                {
                    LogThreadException(Logger, threadName, Thread.ManagedThreadId, ex);
                }
            }
        }
        catch (Exception ex)
        {
            LogThreadCrashed(Logger, threadName, Thread.ManagedThreadId, ex);
            throw;
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(null);
            LogThreadStopped(Logger, threadName, Thread.ManagedThreadId);
        }
    }

    public void Enqueue(Action<object?> callback, object? state)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        EnqueuedWorkItem workItem = new()
        {
            Callback = callback,
            State = state
        };

        _queue.Add(workItem, StoppingToken);
    }

    public void Enqueue<T>(Action<T> callback, T state) where T : class?
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        EnqueuedWorkItem workItem = new()
        {
            Callback = Unsafe.As<Action<object?>>(callback),
            State = state
        };

        _queue.Add(workItem, StoppingToken);
    }

    internal CancellationTokenRegistration Register(BlockingCollection<EnqueuedWorkItem> workItems)
    {
        BlockingCollection<EnqueuedWorkItem>[] newArr = [], currArr = Volatile.Read(ref _workItems), prevArr;
        int newLength = currArr.Length + 1;
        CancellationTokenRegistration registration = RegisterCompleteAdding(workItems, StoppingToken);
        try
        {
            do
            {
                ObjectDisposedException.ThrowIf(IsDisposed, this);
                if (newArr.Length != newLength)
                {
                    newArr = new BlockingCollection<EnqueuedWorkItem>[newLength];
                }
                currArr.CopyTo(newArr, 0);
                currArr[^1] = workItems;
                (currArr, prevArr) = (Interlocked.CompareExchange(ref _workItems, newArr, currArr), currArr);
                newLength = currArr.Length + 1;
            }
            while (!ReferenceEquals(currArr, prevArr));
            return registration;
        }
        catch
        {
            registration.Dispose();
            throw;
        }
    }

    internal void Unregister(BlockingCollection<EnqueuedWorkItem> workItems)
    {
        BlockingCollection<EnqueuedWorkItem>[] newArr = [], currArr = Volatile.Read(ref _workItems), prevArr;
        int newLength = currArr.Length - 1;

        do
        {
            int index = Array.IndexOf(currArr, workItems);
            if (index == -1)
            {
                return;
            }

            if (newArr.Length != newLength)
            {
                newArr = new BlockingCollection<EnqueuedWorkItem>[newLength];
            }

            Array.Copy(currArr, newArr, index);
            Array.Copy(currArr, index + 1, newArr, index, newLength - index);
            (currArr, prevArr) = (Interlocked.CompareExchange(ref _workItems, newArr, currArr), currArr);
            newLength = currArr.Length - 1;
        }
        while (!ReferenceEquals(currArr, prevArr));
    }

    private static CancellationTokenRegistration RegisterCompleteAdding(BlockingCollection<EnqueuedWorkItem> workItems, CancellationToken cancellationToken)
    {
        return cancellationToken.UnsafeRegister(
            static state => Unsafe.As<BlockingCollection<EnqueuedWorkItem>>(state!).CompleteAdding(),
            workItems);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "{ThreadName} thread ({ManagedThreadID}) has started.")]
    private static partial void LogThreadStarted(ILogger logger, string threadName, int managedThreadID);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "{ThreadName} thread ({ManagedThreadID}) has stopped.")]
    private static partial void LogThreadStopped(ILogger logger, string threadName, int managedThreadID);

    [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "{ThreadName} thread ({ManagedThreadID}) has been disposed.")]
    private static partial void LogThreadDisposed(ILogger logger, string threadName, int managedThreadID);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "{ThreadName} thread ({ManagedThreadID}) encountered an unhandled exception.")]
    private static partial void LogThreadException(ILogger logger, string threadName, int managedThreadID, Exception exception);

    [LoggerMessage(EventId = 5, Level = LogLevel.Critical, Message = "{ThreadName} thread ({ManagedThreadID}) has crashed.")]
    private static partial void LogThreadCrashed(ILogger logger, string threadName, int managedThreadID, Exception exception);
}
