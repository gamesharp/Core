using GameSharp.Mathematics;

namespace GameSharp.Rendering;

public readonly record struct TextureView(ITexture Texture, int MipLevel, Int3 Origin, TextureAspect Aspect) : IBindable
{
    public void BindTo<TBuilder>(int binding, scoped ref TBuilder builder) 
        where TBuilder : struct, IBindGroupBuilder, allows ref struct
    {
        builder.AddTexture(binding, in this);
    }
}
