using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GameSharp;

/// <summary>
/// Provides an abstract base class for objects with a manageable asynchronous lifetime.
/// </summary>
/// <inheritdoc/>
public abstract class AsyncLifecycle : AsyncLifetime<AsyncLifecycle>, IDisposable, IAsyncDisposable
{
    private sealed class LifecycleStage(AsyncLifecycle lifetime, Func<Task> taskFactory) : AsyncStage(lifetime)
    {
        protected override async Task ExecuteAsync()
        {
            Task complete = base.ExecuteAsync();
            await complete.ConfigureAwait(false);

            Task main = taskFactory();
            await main.ConfigureAwait(false);
        }
    }

    private sealed class StartingStage(AsyncLifecycle lifetime) : AsyncStage(lifetime)
    {
        protected override async Task ExecuteAsync()
        {
            Task complete = base.ExecuteAsync();
            await complete.ConfigureAwait(false);

            Task starting = Lifetime.OnStartingAsync();
            await starting.ConfigureAwait(false);

            Lifetime.Complete(Lifetime.Main.Stage, "Main");
        }
    }

    private sealed class DisposingStage(AsyncLifecycle lifetime) : AsyncStage(lifetime)
    {
        protected override async Task ExecuteAsync()
        {
            Task complete = base.ExecuteAsync();
#if NET9_0_OR_GREATER
            await complete.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
#else
            try
            {
                await complete.ConfigureAwait(false);
            }
            catch { }
#endif

            Task disposing = Lifetime.OnDisposingAsync();
            await disposing.ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Represents information about a specific stage of the asynchronous lifetime.
    /// </summary>
    public readonly struct StageInfo
    {
        internal AsyncStage Stage { get; init; }

        /// <inheritdoc cref="Lifetime{TSelf}.Stage.IsStarted"/>
        public bool IsStarted => Stage.IsStarted;

        /// <inheritdoc cref="AsyncLifetime{TSelf}.AsyncStage.IsCompleted"/>
        public bool IsCompleted => Stage.IsCompleted;

        /// <summary>
        /// Gets an awaiter for the specified stage of the lifetime.
        /// </summary>
        /// <returns>An <see cref="AsyncLifetime{TSelf}.AsyncStage"/> that can be awaited.</returns>
        public AsyncStage GetAwaiter() => Stage;
    }

    private CancellationTokenSource Cts { get; }

    /// <summary>
    /// Gets information about the startup stage of the asynchronous lifetime.
    /// </summary>
    protected StageInfo Startup { get; }

    /// <summary>
    /// Gets information about the main stage of the asynchronous lifetime.
    /// </summary>
    protected StageInfo Main { get; }

    /// <summary>
    /// Gets information about the stopping stage of the asynchronous lifetime.
    /// </summary>
    protected StageInfo Shutdown { get; }

    /// <summary>
    /// Gets information about the disposal stage of the asynchronous lifetime.
    /// </summary>
    protected StageInfo Disposal { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncLifecycle"/> class.
    /// </summary>
    public AsyncLifecycle()
    {
        Cts = new CancellationTokenSource();
        Startup = new StageInfo { Stage = new StartingStage(this) };
        Main = new StageInfo { Stage = new LifecycleStage(this, ExecuteAsync) };
        Shutdown = new StageInfo { Stage = new LifecycleStage(this, OnStoppingAsync) };
        Disposal = new StageInfo { Stage = new DisposingStage(this) };
    }

    /// <summary>
    /// Gets a <see cref="CancellationToken"/> that can be used to request cancellation of the asynchronous operation.
    /// </summary>
    public CancellationToken StoppingToken => Cts.Token;

    /// <summary>
    /// Attempts to start the lifetime of the object, returning a boolean indicating success or failure.
    /// </summary>
    /// <returns><see langword="true"/> if the lifetime was successfully started; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryStart() => TryComplete(Startup.Stage);

    /// <summary>
    /// Starts the lifetime of the object.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Start() => Complete(Startup.Stage);

    /// <summary>
    /// Starts the lifetime of the object asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous start operation.</returns>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        using CancellationTokenRegistration reg = cancellationToken.Register(Stop);
        TryComplete(Startup.Stage);
        await Startup.Stage.Task.ConfigureAwait(false);
    }

    /// <summary>
    /// Attempts to start the stopping stage of the lifetime, allowing for graceful shutdown of asynchronous operations.
    /// </summary>
    /// <returns><see langword="true"/> if the stopping stage was successfully started; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryStop() => TryComplete(Shutdown.Stage);

    /// <summary>
    /// Signals the stopping stage of the lifetime, allowing for graceful shutdown of asynchronous operations.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Stop() => Complete(Shutdown.Stage);

    /// <inheritdoc cref="StopAsync(CancellationToken)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task StopAsync()
    {
        TryComplete(Shutdown.Stage);
        return Shutdown.Stage.Task;
    }

    /// <summary>
    /// Signals the stopping stage of the lifetime asynchronously, allowing for graceful shutdown of asynchronous operations.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous stop operation.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
#if NET6_0_OR_GREATER
        Task shutdown = StopAsync();
        return shutdown.WaitAsync(cancellationToken);
#else
        if (cancellationToken.CanBeCanceled)
        {
            return Task.Run(StopAsync, cancellationToken);
        }
        
        return StopAsync();
#endif
    }

    /// <summary>
    /// Gets an awaiter for the stopping stage of the lifetime.
    /// </summary>
    /// <returns>An <see cref="AsyncLifetime{TSelf}.AsyncStage"/> that can be awaited.</returns>
    public AsyncStage GetAwaiter() => Shutdown.Stage;

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        TryComplete(Shutdown.Stage);

        if (TryComplete(Disposal.Stage))
        {
            Task disposal = Disposal.Stage.Task;
#if NET9_0_OR_GREATER
            await disposal.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
#else
            try
            {
                await disposal.ConfigureAwait(false);
            }
            catch { }
#endif
        }

        Dispose(disposing: false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Called when the starting stage of the lifetime is reached.
    /// </summary>
    /// <returns>A task that represents the asynchronous starting operation.</returns>
    protected virtual Task OnStartingAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the main stage of the lifetime is reached.
    /// </summary>
    /// <returns>A task that represents the main asynchronous operation.</returns>
    protected virtual Task ExecuteAsync()
    {
        return Shutdown.Stage.Task;
    }

    /// <summary>
    /// Called when the stopping stage of the lifetime is reached, allowing for graceful shutdown of asynchronous operations.
    /// </summary>
    /// <returns>A task that represents the asynchronous stopping operation.</returns>
    protected virtual Task OnStoppingAsync()
    {
#if NET8_0_OR_GREATER
        return Cts.CancelAsync();
#else
        return Task.Run(Cts.Cancel);
#endif
    }

    /// <summary>
    /// Called when the disposing stage of the lifetime is reached, allowing for cleanup of resources.
    /// </summary>
    /// <returns>A task that represents the asynchronous disposing operation.</returns>
    protected virtual Task OnDisposingAsync()
    {
        Cts.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Disposes the resources used by the <see cref="AsyncLifetime{TSelf}"/> instance.
    /// </summary>
    /// <param name="disposing"><see langword="true"/> to dispose managed resources; <see langword="false"/> to dispose unmanaged resources only.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            TryComplete(Shutdown.Stage);
            TryComplete(Disposal.Stage);
        }
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> if the specified stage has already been started.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the specified stage has already been started.</exception>
    /// <inheritdoc cref="ThrowIfStageNotStarted(StageInfo, string?)"/>
    [System.Diagnostics.StackTraceHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static void ThrowIfStageStarted(StageInfo stage, [CallerArgumentExpression(nameof(stage))] string? paramName = null)
    {
        if (stage.IsStarted)
        {
            Throw(new InvalidOperationException($"Stage '{paramName}' has already been started."));
        }
    }

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> if the specified stage has not been started yet.
    /// </summary>
    /// <param name="stage">The stage to check.</param>
    /// <param name="paramName">The name of the parameter.</param>
    /// <exception cref="InvalidOperationException">Thrown if the specified stage has not been started yet.</exception>
    [System.Diagnostics.StackTraceHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static void ThrowIfStageNotStarted(StageInfo stage, [CallerArgumentExpression(nameof(stage))] string? paramName = null)
    {
        if (!stage.IsStarted)
        {
            Throw(new InvalidOperationException($"Stage '{paramName}' has not been started yet."));
        }
    }
#endif
}
