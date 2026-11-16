using GameSharp.Mathematics;
using System.Runtime.CompilerServices;

namespace GameSharp.Rendering;

public static class CommandEncoderExtensions
{
    extension<TItem>(IBuffer<TItem> source) where TItem : unmanaged
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo<TEncoder>(scoped ref TEncoder encoder, IBuffer<TItem> destination, int sourceIndex, int destinationIndex, int count)
            where TEncoder : struct, ICommandEncoder, allows ref struct
        {
            encoder.CopyBufferToBuffer(source, destination, sourceIndex, destinationIndex, count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo<TEncoder>(scoped ref TEncoder encoder, in TextureView destination, in Int3 count)
            where TEncoder : struct, ICommandEncoder, allows ref struct
        {
            encoder.CopyBufferToTexture(source, destination, in count);
        }
    }

    extension(in TextureView source)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo<TEncoder>(scoped ref TEncoder encoder, in TextureView destination, in Int3 count)
            where TEncoder : struct, ICommandEncoder, allows ref struct
        {
            encoder.CopyTextureToTexture(in source, in destination, in count);
        }
    }

    extension<TItem>(in TextureView source) where TItem : unmanaged
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo<TEncoder>(scoped ref TEncoder encoder, IBuffer<TItem> destination, in Int3 count)
            where TEncoder : struct, ICommandEncoder, allows ref struct
        {
            encoder.CopyTextureToBuffer(source, destination, in count);
        }
    }
}
