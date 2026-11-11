using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace GameSharp;

public abstract class AsynchronousLifecycle : IDisposable, IAsyncDisposable
{
    [StructLayout(LayoutKind.Explicit)]
    private struct State
    {
        [FieldOffset(0)]
        public ushort value;

        [FieldOffset(0)]
        public byte stage;

        [FieldOffset(1)]
        public bool disposed;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator State(ushort value)
        {
            return Unsafe.BitCast<ushort, State>(value);
        }
    }

    private readonly record struct DispatchContext
    {
        public required Task Task { get; init; }
        public required Action<CancellationToken> Action { get; init; }
        public required CancellationTokenSource Cts { get; init; }
        public CancellationTokenRegistration CancellationRegistration1 { get; init; }
        public CancellationTokenRegistration CancellationRegistration2 { get; init; }
    }

    private sealed class DispatchContextPool : ObjectPool<StrongBox<DispatchContext>>
    {
        public static DispatchContextPool Shared { get; } = new();

        protected override StrongBox<DispatchContext> Create() => new();

        protected override bool TryReset([NotNullWhen(true)] StrongBox<DispatchContext>? item)
        {
            if (item is null)
            {
                return false;
            }

            item.Value = default;
            return true;
        }
    }

    private const int StartupStage = 1;
    private const int ShutdownStage = 2;

    public Task Startup { get; } = FromInternalCtor();
    public Task Shutdown { get; } = FromInternalCtor();

    private State _state;
    protected bool IsDisposed
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ((State)Volatile.Read(ref _state.value)).disposed;
    }

    private readonly CancellationTokenSource _cts = new();
    public CancellationToken StoppingToken => _cts.Token;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (!TryTransitionTo(StartupStage))
        {
            return Startup.WaitAsync(cancellationToken);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            OperationCanceledException ex = new(cancellationToken);
            TrySetException(Startup, ex);
            return Startup;
        }

        DispatchStage(Startup, OnStartingAsync, cancellationToken);

        return Startup;
    }

    public bool TryStart(CancellationToken cancellationToken = default)
    {
        if (!TryTransitionTo(StartupStage))
        {
            return false;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            SetCancelled(Startup, cancellationToken);
        }
        else
        {
            DispatchStage(Startup, OnStartingAsync, cancellationToken);
        }
        
        return true;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!TryTransitionTo(ShutdownStage))
        {
            return Shutdown.WaitAsync(cancellationToken);
        }

        DispatchStage(Shutdown, OnStoppingAsync, cancellationToken);
        return Shutdown;
    }

    public bool TryStop(CancellationToken cancellationToken = default)
    {
        if (!TryTransitionTo(ShutdownStage))
        {
            return false;
        }

        DispatchStage(Shutdown, OnStoppingAsync, cancellationToken);
        return true;
    }

    private void DispatchStage(Task task, Action<CancellationToken> action, CancellationToken cancellationToken)
    {
        CancellationTokenSource cts = CancellationTokenSourcePool.Shared.Get();
        StrongBox<DispatchContext> boxedCtx = DispatchContextPool.Shared.Get();
        boxedCtx.Value = new DispatchContext
        {
            Task = task,
            Action = action,
            Cts = cts,
            CancellationRegistration1 = Link(cts, StoppingToken),
            CancellationRegistration2 = Link(cts, cancellationToken)
        };

        ThreadPool.QueueUserWorkItem(CompleteStage, boxedCtx, preferLocal: false);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (!TryDispose())
        {
            return;
        }

        Task stop = StopAsync();
        await stop.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        try
        {
            OnDisposing();
        }
        finally
        {
            Dispose(disposing: false);
            GC.SuppressFinalize(this);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing || !TryDispose())
        {
            return;
        }

        Task stop = StopAsync();

        if (stop.IsCompleted)
        {
            OnDisposing();
            return;
        }

        ConfiguredTaskAwaitable awaitable = stop.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter = awaitable.GetAwaiter();
        awaiter.OnCompleted(OnDisposing);
    }

    protected abstract void OnStartingAsync(CancellationToken cancellationToken);
    
    protected virtual void OnStoppingAsync(CancellationToken cancellationToken)
    {
        _cts.Cancel();
    }

    protected virtual void OnDisposing()
    {
        _cts.Dispose();
    }

    [SkipLocalsInit]
    private bool TryDispose()
    {
        State currState = (State)Volatile.Read(ref _state.value), prevState;

        do
        {
            if (currState.disposed)
            {
                return false;
            }

            State newState = currState with { disposed = true };
            (currState, prevState) = ((State)Interlocked.CompareExchange(ref _state.value, newState.value, currState.value), currState);
        }
        while (currState.value != prevState.value);

        return true;
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

    private static void CompleteStage(StrongBox<DispatchContext> boxedCtx)
    {
        DispatchContext ctx = boxedCtx.Value;
        try
        {
            CancellationToken cancellationToken = ctx.Cts.Token;
            cancellationToken.ThrowIfCancellationRequested();
            ctx.Action(cancellationToken);
            TrySetResult(ctx.Task);
        }
        catch (OperationCanceledException ex)
        {
            TrySetCanceled(ctx.Task, ex.CancellationToken);
        }
        catch (Exception ex)
        {
            ExceptionDispatchInfo dispatchInfo = ExceptionDispatchInfo.Capture(ex);
            TrySetException(ctx.Task, dispatchInfo);
        }
        finally
        {
            ctx.CancellationRegistration1.Dispose();
            ctx.CancellationRegistration2.Dispose();
            CancellationTokenSourcePool.Shared.Return(ctx.Cts);
            DispatchContextPool.Shared.Return(boxedCtx);
        }
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

    [SkipLocalsInit]
    private bool TryTransitionTo(byte newStage)
    {
        State currState = (State)Volatile.Read(ref _state.value), prevState;

        do
        {
            ObjectDisposedException.ThrowIf(currState.disposed, this);
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

    [UnsafeAccessor(UnsafeAccessorKind.Constructor, Name = ".ctor")]
    private extern static void InternalCtor(Task task);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "TrySetResult")]
    private extern static bool TrySetResult(Task task);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "TrySetException")]
    private extern static bool TrySetException(Task task, object exceptionObject /* Exception, ExceptionDispatchInfo or IEnumerable<Exception> */);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "TrySetCanceled")]
    private extern static bool TrySetCanceled(Task task, CancellationToken tokenToRecord);
}