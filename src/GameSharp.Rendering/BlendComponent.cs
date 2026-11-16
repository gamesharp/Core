namespace GameSharp.Rendering;

public readonly record struct BlendComponent(BlendOperation Operation, BlendFactor SrcFactor, BlendFactor DstFactor)
{
    public static BlendComponent Replace => new(BlendOperation.Add, BlendFactor.One, BlendFactor.Zero);
}
