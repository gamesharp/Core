using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace GameSharp;

internal sealed class WorkerTaskScheduler(int managedThreadID, 
    BlockingCollection<EnqueuedWorkItem> workItems, 
    CancellationTokenRegistration cancellationTokenRegistration) : 
    TaskScheduler, 
    IDisposable
{
    private bool _disposed;

    public void Dispose()
    {
        if (!Interlocked.Exchange(ref _disposed, true))
        {
            cancellationTokenRegistration.Dispose();
            workItems.Dispose();
        }
    }

    protected override IEnumerable<Task>? GetScheduledTasks()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return workItems.Select(workItem => Unsafe.As<Task>(workItem.State!));
    }

    protected override void QueueTask(Task task)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        workItems.Add(new EnqueuedWorkItem { Callback = Execute, State = task });
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return Environment.CurrentManagedThreadId == managedThreadID && TryExecuteTask(task);
    }

    private void Execute(object? state)
    {
        TryExecuteTask(Unsafe.As<Task>(state!));
    }
}