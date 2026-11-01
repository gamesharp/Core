using System.Runtime.CompilerServices;

namespace GameSharp;

internal readonly struct SuppressThrowingAwaitable
{
    public readonly struct SuppressThrowingAwaiter : INotifyCompletion
    {
        public required ConfiguredTaskAwaitable.ConfiguredTaskAwaiter Awaiter { get; init; }

        public bool IsCompleted => Awaiter.IsCompleted;

        public void OnCompleted(System.Action continuation)
        {
            Awaiter.OnCompleted(continuation);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void GetResult()
        {
            try
            {
                Awaiter.GetResult();
            }
            catch
            {
                // Suppress exceptions
            }
        }
    }

    public required ConfiguredTaskAwaitable Awaitable { get; init; }

    public SuppressThrowingAwaiter GetAwaiter()
    {
        return new SuppressThrowingAwaiter { Awaiter = Awaitable.GetAwaiter() };
    }
}
