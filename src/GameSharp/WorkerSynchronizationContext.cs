using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace GameSharp;

internal sealed class WorkerSynchronizationContext(
    BlockingCollection<EnqueuedWorkItem> workItems, 
    Worker worker,
    CancellationTokenRegistration cancellationTokenRegistration) : 
    SynchronizationContext, 
    IDisposable
{
    private sealed class SenderPool : ObjectPool<Sender>
    {
        public static SenderPool Shared { get; } = new();

        protected override Sender Create()
        {
            return new Sender();
        }

        protected override bool TryReset([NotNullWhen(true)] Sender? item)
        {
            if (item is null)
            {
                return false;
            }

            item.Completion.Reset();
            item.Exception = null;
            item.WorkItem = default;

            return true;
        }
    }

    private sealed class Sender : IDisposable
    {
        public ManualResetEventSlim Completion { get; } = new(initialState: false);
        public ExceptionDispatchInfo? Exception { get; set; }
        public EnqueuedWorkItem WorkItem { get; set; }

        public void Dispose()
        {
            Completion.Dispose();
        }
    }

    private bool _disposed;

    public override void Post(SendOrPostCallback d, object? state)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!TryExecuteInline(d, state))
        {
            PostImpl(d, state);
        }
    }

    public override void Send(SendOrPostCallback d, object? state)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!TryExecuteInline(d, state))
        {
            SendImpl(d, state);
        }
    }

    public override SynchronizationContext CreateCopy()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        BlockingCollection<EnqueuedWorkItem> workItems = [];
        CancellationTokenRegistration reg = worker.Register(workItems);
        return new WorkerSynchronizationContext(workItems, worker, reg);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, true))
        {
            return;
        }

        if ((worker.Thread.ThreadState & ThreadState.Stopped) == 0)
        {
            cancellationTokenRegistration.Dispose();
            worker.Unregister(workItems);
        }

        workItems.Dispose();
    }

    private void PostImpl(SendOrPostCallback d, object? state)
    {
        EnqueuedWorkItem workItem = CreateEnqueuedWorkItem(d, state);
        workItems.Add(workItem);
    }

    private void SendImpl(SendOrPostCallback d, object? state)
    {
        Sender sender = SenderPool.Shared.Get();
        sender.WorkItem = CreateEnqueuedWorkItem(d, state);

        try
        {
            PostImpl(ExecuteSend, sender);

            sender.Completion.Wait();
            sender.Exception?.Throw();
        }
        finally
        {
            SenderPool.Shared.Return(sender);
        }
    }

    private bool TryExecuteInline(SendOrPostCallback d, object? state)
    {
        if (Environment.CurrentManagedThreadId == worker.Thread.ManagedThreadId)
        {
            d(state);
            return true;
        }

        return false;
    }

    private static void ExecuteSend(object? state)
    {
        Sender sender = Unsafe.As<Sender>(state!);
        try
        {
            EnqueuedWorkItem workItem = sender.WorkItem;
            workItem.Callback(workItem.State);
        }
        catch (Exception ex)
        {
            sender.Exception = ExceptionDispatchInfo.Capture(ex);
        }
        finally
        {
            sender.Completion.Set();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static EnqueuedWorkItem CreateEnqueuedWorkItem(SendOrPostCallback d, object? state) => new()
    {
        // SendOrPostCallback and Action<object?> have the same signature, so we can safely cast here.
        Callback = Unsafe.As<Action<object?>>(d),
        State = state
    };
}
