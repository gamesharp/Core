using GameSharp.Mathematics;

namespace GameSharp.Rendering;

public readonly record struct RenderPassColorAttachment
{
    public TextureView View { get; init; }
    public TextureView? ResolveTarget { get; init; }
    public LoadOp LoadOp { get; init; }
    public StoreOp StoreOp { get; init; }
    public Pixel128 ClearColor { get; init; }
}