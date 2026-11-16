using System.Collections.Immutable;

namespace GameSharp.Rendering;

public readonly struct RenderPassDescriptor
{
    public string? Label { get; init; }
    public ImmutableArray<RenderPassColorAttachment> ColorAttachments { get; init; }
    public RenderPassDepthStencilAttachment? DepthStencilAttachment { get; init; }
}