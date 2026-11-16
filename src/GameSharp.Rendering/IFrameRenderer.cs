namespace GameSharp.Rendering;

public interface IFrameRenderer : IDisposable
{
    RenderPassDescriptor RenderPass { get; }

    ComputePassDescriptor ComputePass { get; }

    /// <summary>
    /// Renders a frame.
    /// </summary>
    /// <typeparam name="T">The type of the render pass encoder.</typeparam>
    /// <param name="surface">The texture view that represents the surface to render to.</param>
    /// <param name="context">The frame context that provides information about the current frame.</param>
    /// <param name="encoder">The render pass encoder that can be used to encode rendering commands.</param>
    void Render<T>(ref readonly TextureView surface, ref readonly FrameContext context, ref T encoder) 
        where T : struct, IRenderPassEncoder, allows ref struct;

    void Compute<T>(ref readonly TextureView surface, ref readonly FrameContext context, ref T encoder) 
        where T : struct, IComputePassEncoder, allows ref struct;
}
