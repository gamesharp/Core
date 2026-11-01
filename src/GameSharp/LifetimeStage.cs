using System;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
#if NET9_0_OR_GREATER
using Lock = System.Threading.Lock;
#else
using Lock = System.Object;
#endif

namespace GameSharp;

/// <summary>
/// Represents an asynchronous stage in the lifetime of an object.
/// </summary>
public abstract class LifetimeStage : ILifetimeStage, ILifetimeStageAwaiter
{
    private protected interface ITaskFactory
    {
        Task CreateTask(CancellationToken cancellationToken);
    }

    private Flag _started;
    private Lock? _syncRoot;

    private TaskCompletionSource? _tcs;
    private Task? _task;
    /// <inheritdoc/>
    public Task? Task => Volatile.Read(ref _task);

    /// <inheritdoc cref="ILifetimeStage.Info"/>
    public LifetimeStageInfo Info => new(FlagInterlocked.Read(ref _started), this);
    ILifetimeStageInfo ILifetimeStage.Info => Info;

    /// <inheritdoc/>
    public ILifetimeStage? Previous { get; private set; }

    /// <inheritdoc/>
    public bool IsCompleted => FlagInterlocked.Read(ref _started)
        && _task is { IsCompleted: true };

    /// <summary>
    /// Gets the cancellation token associated with this lifetime stage.
    /// </summary>
    protected virtual CancellationToken CancellationToken => CancellationToken.None;

#if NET8_0_OR_GREATER
    /// <summary>
    /// Gets the <see cref="ConfigureAwaitOptions"/> for configuring await behavior.
    /// </summary>
    protected virtual ConfigureAwaitOptions ConfigureAwaitOptions => ConfigureAwaitOptions.None;
#else
    /// <summary>
    /// Gets a value indicating whether to continue on the captured synchronization context when awaiting.
    /// </summary>
    protected virtual bool ContinueOnCapturedContext => true;
#endif

    private protected bool TryComplete<T>(ref ILifetimeStage? current, in T factory) 
        where T : struct, ITaskFactory
    {
        if (FlagInterlocked.Read(ref _started))
        {
            return false;
        }

        Lock syncRoot = LockPool.Shared.InterlockedInit(ref _syncRoot);
        try
        {
#if NET9_0_OR_GREATER
            syncRoot.Enter();
#else
            Monitor.Enter(syncRoot);
#endif
            if (FlagInterlocked.Exchange(ref _started, true))
            {
                return false;
            }

            Previous = Interlocked.Exchange(ref current, this);

            Task task = factory.CreateTask(CancellationToken);
            if (Volatile.Read(ref _tcs) is { } tcs)
            {
                task.ContinueWith(tcs.SetFromTask, TaskContinuationOptions.ExecuteSynchronously);
            }
            else
            {
                Volatile.Write(ref _task, task);
            }
        }
        finally
        {
            Volatile.Write(ref _syncRoot, null);
#if NET9_0_OR_GREATER
            syncRoot.Exit();
#else
            Monitor.Exit(syncRoot);
#endif
            LockPool.Shared.Return(syncRoot);
        }

        return true;
    }

    /// <inheritdoc/>
    public void OnCompleted(Action continuation)
    {
        Task task = GetOrInitTask();
        TaskAwaiter taskAwaiter = task.GetAwaiter();
        taskAwaiter.OnCompleted(continuation);
    }

    /// <inheritdoc/>
    public void GetResult()
    {
        Task task = GetOrInitTask();
        ConfiguredTaskAwaitable configuredAwaitable =
#if NET8_0_OR_GREATER
            task.ConfigureAwait(ConfigureAwaitOptions);
#else
            task.ConfigureAwait(ContinueOnCapturedContext);
#endif
        ConfiguredTaskAwaitable.ConfiguredTaskAwaiter taskAwaiter = configuredAwaitable.GetAwaiter();
        taskAwaiter.GetResult();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryGetTask([NotNullWhen(true)] out Task? task)
    {
        return (task = Volatile.Read(ref _task)) is { };
    }

    private Task GetOrInitTask()
    {
        if (TryGetTask(out Task? task))
        {
            return task;
        }

        lock (LockPool.Shared.InterlockedInit(ref _syncRoot))
        {
            if (TryGetTask(out task))
            {
                return task;
            }

            TaskCompletionSource tcs = new();
            task = tcs.Task;

            Volatile.Write(ref _tcs, tcs);
            Volatile.Write(ref _task, task);
        }

        return task;
    }

    bool ILifetimeStage.TryComplete(ref ILifetimeStage? current, object? state)
    {
        throw new NotSupportedException();
    }
}
