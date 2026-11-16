namespace GameSharp.Rendering;

public interface IRenderPipelineFactory
{
    void Configure<TBuilder>(scoped ref TBuilder builder) where TBuilder : struct, IRenderPipelineBuilder, allows ref struct;
}