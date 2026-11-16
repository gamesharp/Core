namespace GameSharp.Rendering;

public interface IBuffer<TItem> : IResource, IBindable where TItem : unmanaged
{
    int Length { get; }

    void IBindable.BindTo<TBuilder>(int binding, scoped ref TBuilder builder) 
    {
        builder.AddBuffer(binding, this, 0, Length);
    }
}