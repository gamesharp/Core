using GameSharp.Generic;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
#if NET9_0_OR_GREATER
using Lock = System.Threading.Lock;
#else
using Lock = System.Object;
#endif

namespace GameSharp;

/// <summary>
/// Provides an abstract base class for objects with a manageable asynchronous lifetime.
/// </summary>
/// <inheritdoc/>
public abstract class AsynchronousLifecycle : Lifecycle, IDisposable, IAsyncDisposable
{
    private interface IStageFactory<T> where T : ILifetimeStage
    {
        T Create();
    }

    private readonly struct IntermediateStageFactory : IStageFactory<IntermediateStage>
    {
        public required CancellationToken CancellationToken { get; init; }
        public required Func<Task> TaskFactory { get; init; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntermediateStage Create() => new(TaskFactory, CancellationToken);
    }

    private readonly struct StoppingStageFactory : IStageFactory<StoppingStage>
    {
        public required AsynchronousLifecycle Lifecycle { get; init; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StoppingStage Create() => new(Lifecycle);
    }

#if NET9_0_OR_GREATER
    [InlineArray(2)]
    private struct Task2
    {
        private Task _value;
    }
#endif

    private abstract class Stage(CancellationToken cancellationToken) : AsynchronousLifetimeStage
    {
        protected override CancellationToken CancellationToken { get; } = cancellationToken;
#if NET8_0_OR_GREATER
        protected override ConfigureAwaitOptions ConfigureAwaitOptions => ConfigureAwaitOptions.None;
#else
        protected override bool ContinueOnCapturedContext => false;
#endif
    }

    private sealed class IntermediateStage(Func<Task> taskFactory, CancellationToken cancellationToken) : Stage(cancellationToken)
    {
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Task prev = base.ExecuteAsync(cancellationToken);
            await prev.ConfigureAwait(false);

            Task curr = taskFactory();
            await curr.ConfigureAwait(false);
        }
    }

    private sealed class StartingStage(AsynchronousLifecycle lifecycle, CancellationToken cancellationToken) : 
        AsynchronousLifetimeStage<CancellationToken>
    {
        protected override CancellationToken CancellationToken { get; } = cancellationToken;
#if NET8_0_OR_GREATER
        protected override ConfigureAwaitOptions ConfigureAwaitOptions => ConfigureAwaitOptions.None;
#else
        protected override bool ContinueOnCapturedContext => false;
#endif
        protected override async Task ExecuteAsync(CancellationToken cancellationToken1, CancellationToken cancellationToken2)
        {
            CancellationTokenSource cts = CancellationTokenSourcePool.Shared.Get();
            cancellationToken1.Register(cts.Cancel);
            cancellationToken2.Register(cts.Cancel);

            Task curr, prev = base.ExecuteAsync(CancellationToken.None, cts.Token);
            await prev.SuppressThrowing();
            if (prev.IsCompletedSuccessfully)
            {
                curr = lifecycle.StartAsync(cts.Token);
                await curr.SuppressThrowing();
            }
            else
            {
                curr = prev;
            }
            CancellationTokenSourcePool.Shared.Return(cts);
            curr.GetAwaiter().GetResult();
        }
    }

    private sealed class StoppingStage(AsynchronousLifecycle lifecycle) : 
        AsynchronousLifetimeStage<CancellationToken>
    {
#if NET8_0_OR_GREATER
        protected override ConfigureAwaitOptions ConfigureAwaitOptions => ConfigureAwaitOptions.None;
#else
        protected override bool ContinueOnCapturedContext => false;
#endif

        protected override async Task ExecuteAsync(CancellationToken cancellationToken1, CancellationToken cancellationToken2)
        {
            Task main = base.ExecuteAsync(CancellationToken.None, cancellationToken2);
            await main.SuppressThrowing();
#if NET8_0_OR_GREATER
            Task2 tasks = default;
            tasks[0] = main;
            tasks[1] = lifecycle.StopAsync(cancellationToken2);
            await Task.WhenAll(tasks);
#else
            await Task.WhenAll(main, lifecycle.StopAsync(cancellationToken2));
#endif
        }
    }

    private readonly CancellationTokenSource _stoppingCts;
    private CancellationTokenSource? _disposingCts;

    private Lock? _syncRoot;

    private readonly StartingStage _startup;
    /// <summary>
    /// Gets information about the startup stage of the asynchronous lifetime.
    /// </summary>
    public LifetimeStageInfo Startup => _startup.Info;

    private IntermediateStage? _main;
    /// <summary>
    /// Gets information about the main stage of the asynchronous lifetime.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    protected LifetimeStageInfo Main
    {
        get
        {
#if PRESERVE_STACK_TRACE
            if (IsDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
#else
            ThrowHelpers.ThrowIfDisposed(IsDisposed, this);
#endif
            return GetOrInitMain().Info;
        }
    }

    private StoppingStage? _shutdown;
    /// <summary>
    /// Gets information about the stopping stage of the asynchronous lifetime.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    public LifetimeStageInfo Shutdown
    {
        get
        {
#if PRESERVE_STACK_TRACE
            if (IsDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
#else
            ThrowHelpers.ThrowIfDisposed(IsDisposed, this);
#endif
            return GetOrInitShutdown().Info;
        }
    }

    private Flag _disposed;
    /// <summary>
    /// Gets a value indicating whether the instance has been disposed.
    /// </summary>
    protected bool IsDisposed => FlagInterlocked.Read(ref _disposed);

    /// <summary>
    /// Initializes a new instance of the <see cref="AsynchronousLifecycle"/> class.
    /// </summary>
    public AsynchronousLifecycle()
    {
        _stoppingCts = new CancellationTokenSource();
        _startup = new StartingStage(this, _stoppingCts.Token);
    }

    /// <summary>
    /// Gets a <see cref="CancellationToken"/> that can be used to request cancellation of the asynchronous operation.
    /// </summary>
    public CancellationToken StoppingToken => _stoppingCts.Token;

    /// <summary>
    /// Starts the <see cref="AsynchronousLifecycle"/> if it has not already been started.
    /// </summary>
    /// <returns><see langword="true"/> if the lifetime was started; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryStart(CancellationToken cancellationToken = default)
    {
#if PRESERVE_STACK_TRACE
        if (IsDisposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
#else
        ThrowHelpers.ThrowIfDisposed(IsDisposed, this);
#endif
        return TryComplete(_startup, cancellationToken);
    }

    /// <returns/>
    /// <inheritdoc cref="StartAsync(CancellationToken)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Start(CancellationToken cancellationToken = default)
    {
#if PRESERVE_STACK_TRACE
        if (IsDisposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
#else
        ThrowHelpers.ThrowIfDisposed(IsDisposed, this);
#endif
        Complete(_startup, cancellationToken);
    }

    /// <summary>
    /// Starts the <see cref="AsynchronousLifecycle"/>.
    /// </summary>
    /// <inheritdoc cref="TryStartAsync(CancellationToken)"/>
    /// <exception cref="InvalidOperationException">Thrown if the lifetime has already been started.</exception>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
#if PRESERVE_STACK_TRACE
        if (IsDisposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
#else
        ThrowHelpers.ThrowIfDisposed(IsDisposed, this);
#endif

        Complete(_startup, cancellationToken);
        return _startup.Task!;
    }

    /// <summary>
    /// Starts the <see cref="AsynchronousLifecycle"/> if it has not already been started.
    /// </summary>
    /// <remarks>The lifetime will run until interrupted.</remarks>
    /// <param name="cancellationToken">Used to abort lifetime start.</param>
    /// <returns>A task that will be completed when the lifetime starts.</returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    public async ValueTask<bool> TryStartAsync(CancellationToken cancellationToken = default)
    {
#if PRESERVE_STACK_TRACE
        if (IsDisposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
#else
        ThrowHelpers.ThrowIfDisposed(IsDisposed, this);
#endif

        if (TryComplete(_startup, cancellationToken))
        {
            Task task = _startup.Task!;
            await task.SuppressThrowing();
            return task.IsCompletedSuccessfully;
        }

        return false;
    }

    /// <summary>
    /// Stops the <see cref="AsynchronousLifecycle"/> if it has not already been stopped.
    /// </summary>
    /// <returns><see langword="true"/> if the lifecycle was stopped; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryStop(CancellationToken cancellationToken = default)
    {
#if PRESERVE_STACK_TRACE
        if (IsDisposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
#else
        ThrowHelpers.ThrowIfDisposed(IsDisposed, this);
#endif
        return TryComplete(GetOrInitShutdown(), cancellationToken);
    }

    /// <returns/>
    /// <inheritdoc cref="StopAsync(CancellationToken)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Stop(CancellationToken cancellationToken = default)
    {
#if PRESERVE_STACK_TRACE
        if (IsDisposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
#else
        ThrowHelpers.ThrowIfDisposed(IsDisposed, this);
#endif
        Complete(GetOrInitShutdown(), GetOrInitLinkedToken(ref _disposingCts, cancellationToken));
    }

    /// <summary>
    /// Stops the <see cref="AsynchronousLifecycle"/>.
    /// </summary>
    /// <inheritdoc cref="TryStopAsync(CancellationToken)"/>
    /// <exception cref="InvalidOperationException">Thrown if the lifecycle has already been stopped.</exception>
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
#if PRESERVE_STACK_TRACE
        if (IsDisposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
#else
        ThrowHelpers.ThrowIfDisposed(IsDisposed, this);
#endif
        StoppingStage shutdown = GetOrInitShutdown();
        Complete(shutdown, GetOrInitLinkedToken(ref _disposingCts, cancellationToken));
        return shutdown.Task!;
    }

    /// <summary>
    /// Stops the <see cref="AsynchronousLifecycle"/> if it has not already been stopped.
    /// </summary>
    /// <param name="cancellationToken">Used to indicate when stop should no longer be graceful.</param>
    /// <returns>A task that will be completed when the lifecycle has stopped.</returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    public async ValueTask<bool> TryStopAsync(CancellationToken cancellationToken = default)
    {
#if PRESERVE_STACK_TRACE
        if (IsDisposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
#else
        ThrowHelpers.ThrowIfDisposed(IsDisposed, this);
#endif

        StoppingStage shutdown = GetOrInitShutdown();

        if (TryComplete(shutdown, GetOrInitLinkedToken(ref _disposingCts, cancellationToken)))
        {
            Task task = shutdown.Task!;
            await task.SuppressThrowing();
            return task.IsCompletedSuccessfully;
        }

        return false;
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
        if (FlagInterlocked.Exchange(ref _disposed, true))
        {
            return;
        }

        try
        {
            ValueTask release = ReleaseManagedResourcesAsync();
            await release.ConfigureAwait(false);
        }
        finally
        {
            Dispose(disposing: false);
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Called when the starting stage of the lifetime is reached.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to request cancellation of the starting operation.</param>
    /// <returns>A task that represents the asynchronous starting operation.</returns>
    protected virtual Task OnStartingAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the main stage of the lifetime is reached.
    /// </summary>
    /// <returns>A task that represents the main asynchronous operation.</returns>
    protected virtual async Task ExecuteAsync()
    {
        await Shutdown;
    }

    /// <summary>
    /// Attempts a graceful shutdown of the lifetime.
    /// </summary>
    /// <param name="cancellationToken">Used to indicate when stop should no longer be graceful.</param>
    /// <returns>A task that completes when the <see cref="AsynchronousLifecycle"/> stops.</returns>
    protected virtual async Task OnStoppingAsync(CancellationToken cancellationToken)
    {
        Task cancel = _stoppingCts.CancelAsync(cancellationToken);
        await cancel.SuppressThrowing();
    }

    /// <summary>
    /// Completes the shutdown stage, waits for shutdown work to finish, and releases managed synchronization and
    /// cancellation resources.
    /// </summary>
    protected virtual void ReleaseManagedResources()
    {
        StoppingStage shutdown = GetOrInitShutdown();
        if (TryComplete(shutdown, new CancellationToken(canceled: true)))
        {
            WaitSyncSwallowing(shutdown.Task!);
        }
        else if (shutdown.IsCompleted)
        {
            CancellationTokenSourcePool.Shared.Return(Volatile.Read(ref _disposingCts)!);
        }
        else
        {
            CancelAndDisposeSwallowing(Volatile.Read(ref _disposingCts)!);
            WaitSyncSwallowing(shutdown.Task!);
        }

        LockPool.Shared.InterlockedReturn(ref _syncRoot);

        _stoppingCts.Dispose();
    }

    /// <returns>A task that completes when the shutdown stage is complete and managed resources have been released.</returns>
    /// <inheritdoc cref="ReleaseManagedResources"/>
    protected virtual async ValueTask ReleaseManagedResourcesAsync()
    {
        StoppingStage shutdown = GetOrInitShutdown();
        if (TryComplete(shutdown, new CancellationToken(canceled: true)))
        {
            await shutdown.Task!.SuppressThrowing();
        }
        else if (shutdown.IsCompleted)
        {
            CancellationTokenSourcePool.Shared.Return(Volatile.Read(ref _disposingCts)!);
        }
        else
        {
#if NET8_0_OR_GREATER
            Task cancel = Volatile.Read(ref _disposingCts)!.CancelAsync();
#else
            Task cancel = Task.Run(Volatile.Read(ref _disposingCts)!.Cancel);
#endif
            await cancel.SuppressThrowing();
            await shutdown.Task!.SuppressThrowing();
        }

        LockPool.Shared.InterlockedReturn(ref _syncRoot);

        _stoppingCts.Dispose();
    }

    /// <summary>
    /// Disposes the resources used by the <see cref="AsynchronousLifecycle"/> instance.
    /// </summary>
    /// <param name="disposing"><see langword="true"/> to dispose managed resources; <see langword="false"/> to dispose unmanaged resources only.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (FlagInterlocked.Exchange(ref _disposed, true))
        {
            return;
        }

        if (disposing)
        {
            ReleaseManagedResources();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static CancellationToken GetOrInitLinkedToken(ref CancellationTokenSource? location1, CancellationToken cancellationToken)
    {
        CancellationTokenSource cts = CancellationTokenSourcePool.Shared.InterlockedInit(ref location1);
        cancellationToken.Register(cts.Cancel);
        return cts.Token;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WaitSyncSwallowing(Task task)
    {
        task.SuppressThrowing().GetAwaiter().GetResult();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CancelAndDisposeSwallowing(CancellationTokenSource cts)
    {
        try
        {
            cts.Cancel();
        }
        catch (AggregateException) { }
        finally
        {
            cts.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IntermediateStage GetOrInitMain()
    {
        if (Volatile.Read(ref _main) is not { } main)
        {
            IntermediateStageFactory factory = new() { CancellationToken = StoppingToken, TaskFactory = ExecuteAsync };
            main = InitStage(ref _main, in factory, ref _syncRoot);
        }

        return main;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private StoppingStage GetOrInitShutdown()
    {
        if (Volatile.Read(ref _shutdown) is not { } shutdown)
        {
            StoppingStageFactory factory = new() { Lifecycle = this };
            shutdown = InitStage(ref _shutdown, factory, ref _syncRoot);
        }

        return shutdown;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static TStage InitStage<TStage, TFactory>(ref TStage? stage, in TFactory factory, ref Lock? syncRoot)
        where TStage : class, ILifetimeStage
        where TFactory : struct, IStageFactory<TStage>
    {
        lock (LockPool.Shared.InterlockedInit(ref syncRoot))
        {
            stage = Volatile.Read(ref stage);

            if (stage is null)
            {
                stage = factory.Create();
                Volatile.Write(ref stage, stage);
            }
        }

        return stage;
    }
}
