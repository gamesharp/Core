using System.Runtime.InteropServices;

namespace GameSharp.Collections;

internal sealed class CollectableArrayFactory : ArrayFactory
{
    public static CollectableArrayFactory Default { get; } = new();

    public override Span<T> Get<T>(out Array array, int length, int capacity)
    {
        array = GC.AllocateUninitializedArray<T>(capacity);
        return MemoryMarshal.CreateSpan(ref ReinterpretArrayDataRef<T>(array), length);
    }

    protected override void Resize<T>(ref Array array, ref Span<T> span, int newSize)
    {
        Span<T> newSpan = Get<T>(out array, span.Length, newSize);
        span.CopyTo(newSpan);
        span = newSpan;
    }
}
