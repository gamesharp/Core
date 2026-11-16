namespace GameSharp.Rendering;

public interface IBindGroupFactory
{
    void Configure<TBuilder>(scoped ref TBuilder builder) where TBuilder : struct, IBindGroupBuilder, allows ref struct;
}
