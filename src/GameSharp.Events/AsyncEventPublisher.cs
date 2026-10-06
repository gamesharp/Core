using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GameSharp.Events;

/// <summary>
/// Provides factory methods for creating asynchronous event publishers.
/// </summary>
public static class AsyncEventPublisher
{
    private interface IAsyncListenerOperation<TSender, TEventArgs>
    {
        ValueTask ExecuteAsync(AsyncEventListener<TSender, TEventArgs> listener);
    }

    private readonly struct DisposeOperation<TSender, TEventArgs> : IAsyncListenerOperation<TSender, TEventArgs>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask ExecuteAsync(AsyncEventListener<TSender, TEventArgs> listener) => listener.DisposeAsync();
    }

    private readonly struct InvokeOperation<TSender, TEventArgs>(TSender sender, TEventArgs args, CancellationToken cancellationToken) : 
        IAsyncListenerOperation<TSender, TEventArgs>
    {
        public readonly TSender sender = sender;

        public readonly TEventArgs eventArgs = args;

        public readonly CancellationToken cancellationToken = cancellationToken;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask ExecuteAsync(AsyncEventListener<TSender, TEventArgs> listener)
        {
            var typedListener = listener.UnsafeCast<IAsyncEventListener<TSender, TEventArgs>>();
            return typedListener.InvokeAsync(sender, eventArgs, cancellationToken);
        }
    }

    private readonly struct ParameterlessInvokeOperation<TSender, TEventArgs>(TSender sender, CancellationToken cancellationToken) : 
        IAsyncListenerOperation<TSender, TEventArgs>
    {
        public readonly TSender sender = sender;

        public readonly CancellationToken cancellationToken = cancellationToken;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask ExecuteAsync(AsyncEventListener<TSender, TEventArgs> listener)
        {
            var typedListener = listener.UnsafeCast<IAsyncEventListener<TSender>>();
            return typedListener.InvokeAsync(sender, cancellationToken);
        }
    }

    /// <summary>
    /// Creates a parameterless asynchronous event publisher that allows publishing events to subscribed listeners.
    /// </summary>
    /// <param name="logger">The logger to use for logging events.</param>
    /// <returns>A parameterless asynchronous event publisher.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ParameterlessAsyncEventPublisher<object?, TimeSpan> Create(ILogger logger)
    {
        return Create<object?>(logger);
    }

    /// <summary>
    /// Creates a parameterless asynchronous event publisher that allows publishing events to subscribed listeners.
    /// </summary>
    /// <typeparam name="TSender">The type of the sender.</typeparam>
    /// <param name="logger">The logger to use for logging events.</param>
    /// <returns>A parameterless asynchronous event publisher.</returns>
    public static ParameterlessAsyncEventPublisher<TSender, TimeSpan> Create<TSender>(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        AsyncEventBus<TSender, TimeSpan> eventBus = new(TemporalAsyncEventListenerFactory.Default, logger);
        return new ParameterlessAsyncEventPublisher<TSender, TimeSpan>(eventBus, logger);
    }

    /// <inheritdoc cref="CreateQueue{TSender, TEventArgs}(ILogger)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AsyncEventPublisher<object?, TEventArgs> CreateQueue<TEventArgs>(ILogger logger)
    {
        return CreateQueue<object?, TEventArgs>(logger);
    }

    /// <summary>
    /// Creates an asynchronous event publisher that allows publishing events to subscribed listeners.
    /// </summary>
    /// <typeparam name="TSender">The type of the sender.</typeparam>
    /// <typeparam name="TEventArgs">The type of the event arguments.</typeparam>
    /// <param name="logger">The logger to use for logging events.</param>
    /// <returns>An asynchronous event publisher.</returns>
    public static AsyncEventPublisher<TSender, TEventArgs> CreateQueue<TSender, TEventArgs>(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        AsyncEventBus<TSender, TEventArgs> eventBus = new(QueueAsyncEventListenerFactory<TEventArgs>.Default, logger);
        return new AsyncEventPublisher<TSender, TEventArgs>(eventBus);
    }

    /// <inheritdoc cref="CreateConflating{TSender, TEventArgs}(ILogger)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AsyncEventPublisher<object?, TEventArgs> CreateConflating<TEventArgs>(ILogger logger)
    {
        return CreateConflating<object?, TEventArgs>(logger);
    }

    /// <summary>
    /// Creates an asynchronous event publisher that allows publishing events to subscribed listeners, with conflation support.
    /// </summary>
    /// <typeparam name="TSender">The type of the sender.</typeparam>
    /// <typeparam name="TEventArgs">The type of the event arguments.</typeparam>
    /// <param name="logger">The logger to use for logging events.</param>
    /// <returns>An asynchronous event publisher.</returns>
    public static AsyncEventPublisher<TSender, TEventArgs> CreateConflating<TSender, TEventArgs>(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        AsyncEventBus<TSender, TEventArgs> eventBus = new(ConflatingAsyncEventListenerFactory<TEventArgs>.Default, logger);
        return new AsyncEventPublisher<TSender, TEventArgs>(eventBus);
    }

    internal static void Dispose<TSender, TEventArgs>(AsyncEventBus<TSender, TEventArgs> eventBus)
    {
        foreach (AsyncEventListener<TSender, TEventArgs> listener in eventBus)
        {
            listener.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ValueTask DisposeAsync<TSender, TEventArgs>(AsyncEventBus<TSender, TEventArgs> eventBus)
    {
        return DoForAll(eventBus, default(DisposeOperation<TSender, TEventArgs>));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ValueTask InvokeAsync<TSender, TEventArgs>(
        AsyncEventBus<TSender, TEventArgs> eventBus, 
        TSender sender, 
        TEventArgs e, 
        CancellationToken cancellationToken)
    {
        InvokeOperation<TSender, TEventArgs> operation = new(sender, e, cancellationToken);
        return DoForAll(eventBus, operation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ValueTask InvokeAsync<TSender, TEventArgs>(
        AsyncEventBus<TSender, TEventArgs> eventBus, 
        TSender sender, 
        CancellationToken cancellationToken)
    {
        ParameterlessInvokeOperation<TSender, TEventArgs> operation = new(sender, cancellationToken);
        return DoForAll(eventBus, operation);
    }

    private static async ValueTask DoForAll<TSender, TEventArgs, TOperation>(AsyncEventBus<TSender, TEventArgs> eventBus, TOperation operation)
        where TOperation : struct, IAsyncListenerOperation<TSender, TEventArgs>
    {
        ImmutableArray<AsyncEventListener<TSender, TEventArgs>> listeners = eventBus.CurrentListeners;
        ArrayPool<Task> sharedPool = ArrayPool<Task>.Shared;
        Task[] tasks = sharedPool.Rent(listeners.Length);
        try
        {
            ref Task arrayDataRef = ref MemoryMarshal.GetArrayDataReference(tasks);
            int count = 0;
            foreach (AsyncEventListener<TSender, TEventArgs> listener in listeners)
            {
                ValueTask valueTask = operation.ExecuteAsync(listener);
                if (valueTask.IsCompleted)
                {
                    valueTask.GetAwaiter().GetResult();
                    continue;
                }
                Unsafe.Add(ref arrayDataRef, count++) = valueTask.AsTask();
            }
            if (count > 0)
            {
                Span<Task> taskSpan = tasks.AsSpan(0, count);
                await Task.WhenAll(taskSpan);
            }
        }
        finally
        {
            sharedPool.Return(tasks, clearArray: true);
        }
    }
}

/// <summary>
/// Represents an asynchronous event publisher that allows publishing events to subscribed listeners.
/// </summary>
/// <typeparam name="TSender">The type of the sender.</typeparam>
/// <typeparam name="TEventArgs">The type of the event arguments.</typeparam>
public readonly struct AsyncEventPublisher<TSender, TEventArgs> : IDisposable, IAsyncDisposable
{
    private readonly AsyncEventBus<TSender, TEventArgs> _eventBus;

    /// <summary>
    /// Gets the asynchronous event that can be subscribed to by listeners.
    /// </summary>
    public AsyncEvent<TSender, TEventArgs> Event { get; }

    internal AsyncEventPublisher(AsyncEventBus<TSender, TEventArgs> eventBus)
    {
        _eventBus = eventBus;
        Event = new AsyncEvent<TSender, TEventArgs>(eventBus);
    }

    /// <summary>
    /// Publishes an event with the specified arguments to all subscribers.
    /// </summary>
    /// <inheritdoc cref="AsyncEventHandler{TSender, TEventArgs}"/>
    public void Publish(TSender sender, TEventArgs e, CancellationToken cancellationToken = default)
    {
        foreach (AsyncEventListener<TSender, TEventArgs> listener in _eventBus)
        {
            var typedListener = listener.UnsafeCast<IAsyncEventListener<TSender, TEventArgs>>();
            typedListener.Invoke(sender, e, cancellationToken);
        }
    }

    /// <summary>
    /// Publishes an event with the specified arguments to all subscribers.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> that represents the asynchronous publish operation.</returns>
    /// <inheritdoc cref="AsyncEventHandler{TSender, TEventArgs}"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask PublishAsync(TSender sender, TEventArgs e, CancellationToken cancellationToken = default)
    {
        return AsyncEventPublisher.InvokeAsync(_eventBus, sender, e, cancellationToken);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        AsyncEventPublisher.Dispose(_eventBus);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async ValueTask DisposeAsync()
    {
        await AsyncEventPublisher.DisposeAsync(_eventBus);
    }
}
