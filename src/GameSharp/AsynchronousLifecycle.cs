using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace GameSharp;

/// <summary>
/// Provides a base class for objects with asynchronous startup, shutdown, and disposal lifecycles.
/// </summary>
public abstract class AsynchronousLifecycle : IDisposable, IAsyncDisposable
{
    private enum Stage : byte
    {
        Created,
        Startup,
        Shutdown,
        Disposed
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct State
    {
        [FieldOffset(0)]
        public ushort value;

        [FieldOffset(0)]
        public Stage stage;

        [FieldOffset(1)]
        public bool disposalRequested;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator State(ushort value)
        {
            return Unsafe.BitCast<ushort, State>(value);
        }
    }

    private sealed class Dispatch
    {
        public DispatchContext Context { get; set; }

        public void Publish()
        {
            Task proxy = Context.Proxy;
            Task actual = Context.Actual;

            if (!ReferenceEquals(proxy, actual))
            {
                SetResultFromTask(proxy, actual, Context.Exception);
            }

            Context.Dispose();
            DispatchPool.Shared.Return(this);
        }
    }

    private readonly record struct DispatchContext : IDisposable
    {
        public required Task Proxy { get; init; }
        public required Task Actual { get; init; }
        public ExceptionDispatchInfo? Exception { get; init; }
        public required CancellationTokenSource Cts { get; init; }
        public CancellationTokenRegistration CancellationRegistration1 { get; init; }
        public CancellationTokenRegistration CancellationRegistration2 { get; init; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            CancellationRegistration1.Dispose();
            CancellationRegistration2.Dispose();
            CancellationTokenSourcePool.Shared.Return(Cts);
        }
    }

    private sealed class DispatchPool : ObjectPool<Dispatch>
    {
        public static DispatchPool Shared { get; } = new();

        protected override Dispatch Create() => new();

        protected override bool TryReset([NotNullWhen(true)] Dispatch? item)
        {
            if (item is null)
            {
                return false;
            }

            item.Context = default;
            return true;
        }
    }

    private static Task? _cached;

    private Task? _startup;
    /// <summary>
    /// Gets a task that represents startup.
    /// </summary>
    public Task Startup => GetOrInit(ref _startup);

    private Task? _shutdown;
    /// <summary>
    /// Gets a task that represents shutdown.
    /// </summary>
    public Task Shutdown => GetOrInit(ref _shutdown);

    private readonly Task _disposal;
    private ExceptionDispatchInfo? _cancellationException;
    private CancellationToken _abortShutdownToken;

    private State _state;
    /// <summary>
    /// Gets a value indicating whether the object has been disposed.
    /// </summary>
    protected bool IsDisposed
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ((State)Volatile.Read(ref _state.value)).stage == Stage.Disposed;
    }

    private readonly CancellationTokenSource _cts;
    /// <summary>
    /// Gets a cancellation token that signals when stopping is requested.
    /// </summary>
    public CancellationToken StoppingToken => _cts.Token;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsynchronousLifecycle"/> class.
    /// </summary>
    public AsynchronousLifecycle()
    {
        _cts = new();
        _disposal = new(OnDisposing);
    }
    
    /// <summary>
    /// Starts the asynchronous operation.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel startup.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (!TryTransitionTo(Stage.Startup, out _))
        {
            return Startup.WaitAsync(cancellationToken);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            Task startup = GetOrInit(ref _startup);
            TrySetCanceled(startup, cancellationToken);
            return startup;
        }

        DispatchStage(ref _startup, OnStartingAsync, StoppingToken, cancellationToken);
        return Startup;
    }

    /// <summary>
    /// Attempts to start the lifecycle.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel startup.</param>
    /// <returns><see langword="true"/> if the lifecycle was started; otherwise, <see langword="false"/>.</returns>
    public bool TryStart(CancellationToken cancellationToken = default)
    {
        if (!TryTransitionTo(Stage.Startup, out _))
        {
            return false;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            SetCancelled(Startup, cancellationToken);
        }
        else
        {
            DispatchStage(ref _startup, OnStartingAsync, StoppingToken, cancellationToken);
        }
        
        return true;
    }

    /// <summary>
    /// Performs a graceful shutdown of the lifecycle.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to signal when the shutdown should no longer be graceful.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!TryTransitionTo(Stage.Shutdown, out State currState))
        {
            return Shutdown.WaitAsync(cancellationToken);
        }

        _abortShutdownToken = cancellationToken;
        return BeginShutdown(currState);
    }

    /// <summary>
    /// Attempts to perform a graceful shutdown of the lifecycle.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to signal when the shutdown should no longer be graceful.</param>
    /// <returns><see langword="true"/> if the lifecycle was stopped; otherwise, <see langword="false"/>.</returns>
    public bool TryStop(CancellationToken cancellationToken = default)
    {
        if (!TryTransitionTo(Stage.Shutdown, out State currState))
        {
            return false;
        }

        _abortShutdownToken = cancellationToken;
        BeginShutdown(currState);
        return true;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (!TryDispose(out State currState))
        {
            await _disposal.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            return;
        }

        Task stop = GetShutdownTask(currState);
        await stop.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        _disposal.RunSynchronously();

        Dispose(disposing: false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the resources used by the <see cref="AsynchronousLifecycle"/> instance.
    /// </summary>
    /// <param name="disposing"><see langword="true"/> to dispose managed resources; <see langword="false"/> to dispose unmanaged resources only.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing || !TryDispose(out State currState))
        {
            return;
        }

        Task stop = GetShutdownTask(currState);
        if (stop.IsCompleted)
        {
            _disposal.RunSynchronously();
            return;
        }

        ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter = GetSuppressThrowingAwaiter(stop);
        awaiter.UnsafeOnCompleted(_disposal.RunSynchronously);
    }

    /// <summary>
    /// Performs asynchronous startup operations.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the startup operation.</param>
    /// <returns>A task that completes when the startup operation is finished.</returns>
    protected abstract Task OnStartingAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Performs a graceful shutdown of the lifecycle.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to signal when the shutdown should no longer be graceful.</param>
    /// <returns>A task that completes when the shutdown operation is finished.</returns>
    protected abstract Task OnStoppingAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Releases managed resources and performs any necessary cleanup before the object is disposed.
    /// </summary>
    protected virtual void OnDisposing()
    {
        State currState = (State)Volatile.Read(ref _state.value);
        Unsafe.SkipInit(out State prevState);
        do
        {
            State newState = currState with { stage = Stage.Disposed };
            (currState, prevState) = ((State)Interlocked.CompareExchange(ref _state.value, newState.value, currState.value), currState);
        }
        while (currState.value != prevState.value);

        _cts.Dispose();
    }

    [SkipLocalsInit]
    private bool TryDispose(out State currState)
    {
        currState = (State)Volatile.Read(ref _state.value);
        State prevState;

        do
        {
            if (currState.disposalRequested)
            {
                return false;
            }

            State newState = currState with { disposalRequested = true };
            (currState, prevState) = ((State)Interlocked.CompareExchange(ref _state.value, newState.value, currState.value), currState);
        }
        while (currState.value != prevState.value);

        return true;
    }

    [SkipLocalsInit]
    private Task GetShutdownTask(State currState)
    {
        State prevState;
        do
        {
            if (currState.stage == Stage.Shutdown)
            {
                return GetOrInit(ref _shutdown);
            }

            State newState = currState with { stage = Stage.Shutdown };
            (currState, prevState) = ((State)Interlocked.CompareExchange(ref _state.value, newState.value, currState.value), currState);
        }
        while (currState.value != prevState.value);
        return BeginShutdown(currState);
    }

    private Task BeginShutdown(State currState)
    {
        try
        {
            _cts.Cancel();
        }
        catch (Exception ex)
        {
            _cancellationException = ExceptionDispatchInfo.Capture(ex);
        }

        if (currState.stage == Stage.Created)
        {
            if (Interlocked.CompareExchange(ref _startup, Task.CompletedTask, null) is { })
            {
                TrySetResult(_startup);
            }

            return DispatchStage(ref _shutdown, OnStoppingAsync, CancellationToken.None, _abortShutdownToken, _cancellationException);
        }

        Task startup = GetOrInit(ref _startup);
        if (startup.IsCompleted)
        {
            return DispatchStage(ref _shutdown, OnStoppingAsync, CancellationToken.None, _abortShutdownToken, _cancellationException);
        }

        ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter = GetSuppressThrowingAwaiter(startup);
        awaiter.UnsafeOnCompleted(Stop);
        return GetOrInit(ref _shutdown);
    }

    private void Stop()
    {
        DispatchStage(ref _shutdown, OnStoppingAsync, CancellationToken.None, _abortShutdownToken, _cancellationException);
    }

    private static DispatchContext GetDispatchContext(ref Task? proxy,
        Func<CancellationToken, Task> taskFactory,
        ExceptionDispatchInfo? exception,
        CancellationToken cancellationToken1,
        CancellationToken cancellationToken2)
    {
        CancellationTokenSource cts = CancellationTokenSourcePool.Shared.Get();
        CancellationTokenRegistration cancellationTokenRegistration1 = Link(cts, cancellationToken1);
        CancellationTokenRegistration cancellationTokenRegistration2 = Link(cts, cancellationToken2);
        try
        {
            Task stageExec = taskFactory(cts.Token);
            return new DispatchContext
            {
                Proxy = Interlocked.CompareExchange(ref proxy, stageExec, null) ?? stageExec,
                Actual = stageExec,
                Exception = exception,
                Cts = cts,
                CancellationRegistration1 = cancellationTokenRegistration1,
                CancellationRegistration2 = cancellationTokenRegistration2
            };
        }
        catch
        {
            cancellationTokenRegistration1.Dispose();
            cancellationTokenRegistration2.Dispose();
            CancellationTokenSourcePool.Shared.Return(cts);
            throw;
        }
    }

    private static Task DispatchStage(ref Task? proxy,
        Func<CancellationToken, Task> taskFactory,
        CancellationToken cancellationToken1,
        CancellationToken cancellationToken2,
        ExceptionDispatchInfo? exception = null)
    {
        try
        {
            DispatchContext dispatchCtx = GetDispatchContext(ref proxy, taskFactory, exception, cancellationToken1, cancellationToken2);

            if (dispatchCtx.Actual.IsCompleted)
            {
                if (!ReferenceEquals(dispatchCtx.Proxy, dispatchCtx.Actual))
                {
                    SetResultFromTask(dispatchCtx.Proxy, dispatchCtx.Actual, dispatchCtx.Exception);
                }

                dispatchCtx.Dispose();
                return dispatchCtx.Proxy;
            }

            Dispatch dispatch = DispatchPool.Shared.Get();
            dispatch.Context = dispatchCtx;

            ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter = GetSuppressThrowingAwaiter(dispatchCtx.Actual);
            awaiter.UnsafeOnCompleted(dispatch.Publish);
            return dispatchCtx.Proxy;
        }
        catch (Exception ex)
        {
            Task currProxy = GetOrInit(ref proxy);
            TrySetException(currProxy, ex);
            return currProxy;
        }
    }

    private static Task GetOrInit(ref Task? task)
    {
        if (Volatile.Read(ref task) is { } currTask)
        {
            return currTask;
        }

        Task newTask = Interlocked.Exchange(ref _cached, null) ?? FromInternalCtor();
        currTask = Interlocked.CompareExchange(ref task, newTask, null);
        if (currTask is null)
        {
            return newTask;
        }

        Interlocked.CompareExchange(ref _cached, newTask, null);

        return currTask;
    }

    private static CancellationTokenRegistration Link(CancellationTokenSource cts, CancellationToken cancellationToken)
    {
        if (cancellationToken.CanBeCanceled)
        {
            return cancellationToken.UnsafeRegister(
                static state => Unsafe.As<CancellationTokenSource>(state!).Cancel(),
                cts);
        }

        return default;
    }

    private static Task FromInternalCtor()
    {
        object obj = RuntimeHelpers.GetUninitializedObject(typeof(Task));
        Task task = Unsafe.As<Task>(obj);
        InternalCtor(task);
        return task;
    }

    private static void SetCancelled(Task task, CancellationToken cancellationToken)
    {
        if (TrySetCanceled(task, cancellationToken) || task.IsCompleted)
        {
            return;
        }

        SpinWait spinner = default;

        do
        {
            spinner.SpinOnce();
        }
        while (!task.IsCompleted);
    }

    private static void SetResultFromTask(Task proxy, Task completedTask, ExceptionDispatchInfo? exception)
    {
        try
        {
            completedTask.GetAwaiter().GetResult();
            if (exception is { })
            {
                TrySetException(proxy, exception);
                return;
            }

            TrySetResult(proxy);
        }
        catch (AggregateException ex1) when (exception?.SourceException is AggregateException ex2)
        {
            IEnumerable<Exception> combined = [.. ex1.InnerExceptions, .. ex2.InnerExceptions];
            TrySetException(proxy, combined);
        }
        catch (AggregateException ex1)
        {
            TrySetException(proxy, ex1.InnerExceptions);
        }
        catch (OperationCanceledException ex1) when (exception?.SourceException is AggregateException ex2)
        {
            IEnumerable<Exception> combined = [ex1, .. ex2.InnerExceptions];
            TrySetException(proxy, combined);
        }
        catch (OperationCanceledException ex1)
        {
            TrySetCanceled(proxy, ex1.CancellationToken);
        }
    }

    [SkipLocalsInit]
    private bool TryTransitionTo(Stage newStage, out State currState)
    {
        currState = (State)Volatile.Read(ref _state.value);
        State prevState;

        do
        {
            ObjectDisposedException.ThrowIf(currState.stage == Stage.Disposed, this);
            if (currState.stage >= newStage)
            {
                return false;
            }

            State newState = currState with { stage = newStage };
            (currState, prevState) = ((State)Interlocked.CompareExchange(ref _state.value, newState.value, currState.value), currState);
        }
        while (currState.value != prevState.value);

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ConfiguredTaskAwaitable.ConfiguredTaskAwaiter GetSuppressThrowingAwaiter(Task task)
    {
        ConfiguredTaskAwaitable awaitable = task.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        return awaitable.GetAwaiter();
    }

    [UnsafeAccessor(UnsafeAccessorKind.Constructor, Name = ".ctor")]
    private extern static void InternalCtor(Task task);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "TrySetResult")]
    private extern static bool TrySetResult(Task task);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "TrySetException")]
    private extern static bool TrySetException(Task task, object exceptionObject /* Exception, ExceptionDispatchInfo or IEnumerable<Exception> */);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "TrySetCanceled")]
    private extern static bool TrySetCanceled(Task task, CancellationToken tokenToRecord);
}
