using GameSharp.Mathematics;

namespace GameSharp.Rendering;

public interface ICommandEncoder : IDisposable
{
    void CopyTextureToBuffer<T>(in TextureView source, IBuffer<T> destination, in Int3 count)
        where T : unmanaged;

    void CopyTextureToTexture(in TextureView source, in TextureView destination, in Int3 count);

    void CopyBufferToBuffer<T>(IBuffer<T> source, IBuffer<T> destination, int sourceIndex, int destinationIndex, int count)
        where T : unmanaged;

    void CopyBufferToTexture<T>(IBuffer<T> source, in TextureView destination, in Int3 count) 
        where T : unmanaged;

    void Clear<T>(IBuffer<T> buffer, int startIndex, int count) where T : unmanaged;
}
