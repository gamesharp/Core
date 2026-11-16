using GameSharp.Mathematics;
using System.Diagnostics;

namespace GameSharp.Rendering;

public abstract class RendererService() : DefaultWorker([with(boundedCapacity: MaxFramesInFlight)], threadName: "Renderer")
{
    protected const int MaxFramesInFlight = 2;
    private ulong _frameIndex;
    private long _startTicks;
    private long _currentTicks;

    public abstract IBuffer<T> CreateBuffer<T>(int length, 
        BufferUsage usage, 
        string? label = null) 
        where T : unmanaged;

    public abstract ITexture CreateTexture(in Int3 size,
        TextureFormat format,
        TextureUsage usage,
        int mipLevel,
        int sampleCount,
        ReadOnlySpan<TextureFormat> viewFormats,
        string? label = null);

    public unsafe abstract void WriteBuffer<T>(IBuffer<T> destination, int destinationIndex, T* source, int length)
        where T : unmanaged;

    public unsafe virtual void WriteBuffer<T>(IBuffer<T> destination, int destinationIndex, ReadOnlySpan<T> source)
        where T : unmanaged
    {
        fixed (T* sourcePtr = source)
        {
            WriteBuffer(destination, destinationIndex, sourcePtr, source.Length);
        }
    }

    public abstract ISampler CreateSampler(in SamplerDescriptor descriptor, string? label = null);

    public abstract IBindGroupLayout CreateBindGroupLayout<TFactory>(scoped ref TFactory factory, string? label = null)
        where TFactory : struct, IBindGroupLayoutFactory, allows ref struct;

    public abstract IBindGroup CreateBindGroup<TFactory>(scoped ref TFactory factory, IBindGroupLayout layout, string? label = null)
        where TFactory : struct, IBindGroupFactory, allows ref struct;

    public abstract IComputePipeline CreateComputePipeline<TFactory>(scoped ref TFactory factory, string? label = null)
        where TFactory : struct, IComputePipelineFactory, allows ref struct;

    public abstract IRenderPipeline CreateRenderPipeline<TFactory>(scoped ref TFactory factory, string? label = null)
        where TFactory : struct, IRenderPipelineFactory, allows ref struct;

    public unsafe abstract void WriteTexture(TextureView destination,
        in Int3 copySize,
        byte* source,
        int sourceRowPitch,
        int sourceImagePitch);

    public unsafe virtual void WriteTexture(TextureView destination,
        in Int3 copySize,
        ReadOnlySpan<byte> source,
        int sourceRowPitch,
        int sourceImagePitch)
    {
        fixed (byte* sourcePtr = source)
        {
            WriteTexture(destination, copySize, sourcePtr, sourceRowPitch, sourceImagePitch);
        }
    }

    public void Enqueue(IFrameRenderer renderer)
    {
        Enqueue(Render, renderer);
    }

    protected internal abstract void Resize(in Int2 size);

    protected abstract void Render(ref readonly FrameContext context, IFrameRenderer renderer);

    protected override Task OnStartingAsync(CancellationToken cancellationToken)
    {
        _startTicks = Stopwatch.GetTimestamp();
        _currentTicks = _startTicks;
        return base.OnStartingAsync(cancellationToken);
    }

    private void Render(IFrameRenderer renderer)
    {
        (_currentTicks, long prevTicks) = (Stopwatch.GetTimestamp(), _currentTicks);
        FrameContext ctx = new()
        {
            Index = _frameIndex++,
            Time = Stopwatch.GetElapsedTime(_startTicks, _currentTicks),
            DeltaTime = Stopwatch.GetElapsedTime(prevTicks, _currentTicks)
        };

        try
        {
            Render(in ctx, renderer);
        }
        finally
        {
            renderer.Dispose();
        }
    }
}
