namespace GameSharp.Rendering;

public interface IBindGroupLayoutFactory
{
    void Configure<TBuilder>(scoped ref TBuilder builder) where TBuilder : struct, IBindGroupLayoutBuilder, allows ref struct;
}
