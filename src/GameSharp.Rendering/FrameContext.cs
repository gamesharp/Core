namespace GameSharp.Rendering;

public readonly record struct FrameContext
{
    public ulong Index { get; init; }

    public TimeSpan Time { get; init; }

    public TimeSpan DeltaTime { get; init; }
}
