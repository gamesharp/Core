using System.Runtime.InteropServices;

namespace GameSharp.Collections;

internal sealed class CollectableArrayFactory : ArrayFactory
{
    public static CollectableArrayFactory Default { get; } = new();

    public override ref T Get<T>(out Array array, int minimumLength)
    {
        array = GC.AllocateUninitializedArray<T>(minimumLength);
        return ref ReinterpretArrayDataRef<T>(array);
    }

    protected override ref T Resize<T>(ref Array array, int count, int newSize)
    {
        ReadOnlySpan<T> src = MemoryMarshal.CreateReadOnlySpan(ref ReinterpretArrayDataRef<T>(array), count);
        ref T data = ref Get<T>(out array, newSize);
        Span<T> dst = MemoryMarshal.CreateSpan(ref data, newSize);
        src.CopyTo(dst);
        return ref data;
    }
}
