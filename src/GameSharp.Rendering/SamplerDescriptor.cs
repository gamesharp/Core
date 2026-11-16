namespace GameSharp.Rendering;

public readonly record struct SamplerDescriptor
{
    public FilterMode MinFilter { get; init; }
    public FilterMode MagFilter { get; init; }
    public FilterMode MipmapFilter { get; init; }
    public AddressMode AddressModeU { get; init; }
    public AddressMode AddressModeV { get; init; }
    public AddressMode AddressModeW { get; init; }
    public ushort MaxAnisotropy { get; init; }
    public CompareFunction? Compare { get; init; }
}
