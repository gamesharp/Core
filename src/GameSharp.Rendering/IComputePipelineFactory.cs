namespace GameSharp.Rendering;

public interface IComputePipelineFactory
{
    void Configure<TBuilder>(scoped ref TBuilder builder) where TBuilder : struct, IComputePipelineBuilder, allows ref struct;
}
