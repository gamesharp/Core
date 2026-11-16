namespace GameSharp.Rendering;

public interface IComputePipelineBuilder
{
    string? Label { get; set; }

    void AddBindGroupLayout(IBindGroupLayout layout);

    void SetShader(ReadOnlySpan<byte> bytecode, ReadOnlySpan<byte> entryPointUtf8 = default);
}
