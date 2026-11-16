namespace GameSharp.Rendering;

public interface ITexture : IResource
{
    TextureFormat Format { get; }
    long Width { get; }
    long Height { get; }
}
