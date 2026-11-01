using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GameSharp;

internal static class ThreadingExtensions
{
    public static Task CancelAsync(this CancellationTokenSource cts, CancellationToken cancellationToken)
    {
#if NET8_0_OR_GREATER
        Task cancel = cts.CancelAsync();
        return cancel.WaitAsync(cancellationToken);
#else
        return Task.Run(cts.Cancel, cancellationToken);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if NET8_0_OR_GREATER
    public static ConfiguredTaskAwaitable SuppressThrowing(this Task task)
    {
        return task.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
    }
#else
    public static SuppressThrowingAwaitable SuppressThrowing(this Task task)
    {
        return new SuppressThrowingAwaitable { Awaitable = task.ConfigureAwait(false) };
    }

    extension (Task task)
    {
        public bool IsCompletedSuccessfully
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => task.Status == TaskStatus.RanToCompletion;
        }
    }

    public static Task WaitAsync(this Task task, CancellationToken cancellationToken)
    {
        if (!task.IsCompleted && cancellationToken.CanBeCanceled)
        {
            TaskCompletionSource tcs = new(TaskContinuationOptions.ExecuteSynchronously);
            CancellationTokenRegistration reg = cancellationToken.Register(static (state) => ((TaskCompletionSource)state!).TrySetCanceled(), tcs);
            tcs.Task.ContinueWith(static (task, state) => ((CancellationTokenRegistration)state!).Dispose(), reg);
            task.ContinueWith(static (task, state) => ((TaskCompletionSource)state!).TrySetFromTask(task), tcs);
            return tcs.Task;
        }

        return task;
    }
#endif
}
