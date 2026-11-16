namespace GameSharp.Rendering;

public interface IBindable
{
    void BindTo<TBuilder>(int binding, scoped ref TBuilder builder)
        where TBuilder : struct, IBindGroupBuilder, allows ref struct;
}