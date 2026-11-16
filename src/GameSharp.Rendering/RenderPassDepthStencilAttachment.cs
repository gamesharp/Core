namespace GameSharp.Rendering;

public readonly record struct RenderPassDepthStencilAttachment
{
    public TextureView View { get; init; }

    public LoadOp DepthLoadOp { get; init; }
    public StoreOp DepthStoreOp { get; init; }
    public float ClearDepth { get; init; }
    public bool DepthReadOnly { get; init; }

    public LoadOp StencilLoadOp { get; init; }
    public StoreOp StencilStoreOp { get; init; }
    public byte ClearStencil { get; init; }
    public bool StencilReadOnly { get; init; }
}