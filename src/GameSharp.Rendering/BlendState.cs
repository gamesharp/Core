namespace GameSharp.Rendering;

public readonly record struct BlendState(BlendComponent Color, BlendComponent Alpha)
{
    public static BlendState AlphaBlending => new(
        Color: new(BlendOperation.Add, BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha),
        Alpha: new(BlendOperation.Add, BlendFactor.One, BlendFactor.OneMinusSrcAlpha));
}