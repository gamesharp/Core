namespace GameSharp.Rendering;

public interface IBindGroupBuilder
{
    void AddBuffer<T>(int binding, IBuffer<T> buffer, int elementOffset, int elementCount) where T : unmanaged;

    void AddTexture(int binding, in TextureView view);

    void AddSampler(int binding, ISampler sampler);
}
