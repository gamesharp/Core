namespace GameSharp.Rendering;

public interface ISampler : IResource, IBindable
{
    void IBindable.BindTo<TBuilder>(int binding, scoped ref TBuilder builder) 
    {
        builder.AddSampler(binding, this);
    }
}