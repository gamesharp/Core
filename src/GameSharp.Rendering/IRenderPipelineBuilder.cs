namespace GameSharp.Rendering;

public interface IRenderPipelineBuilder
{
    string? Label { get; set; }

    void AddBindGroupLayout(IBindGroupLayout layout);

    void SetVertexShader(ReadOnlySpan<byte> bytecode, ReadOnlySpan<byte> entryPointUtf8 = default);

    void SetFragmentShader(ReadOnlySpan<byte> bytecode, ReadOnlySpan<byte> entryPointUtf8 = default);

    void AddVertexBuffer(int strideInBytes, VertexStepMode stepMode, ReadOnlySpan<VertexAttribute> attributes);

    void SetPrimitiveState(
        PrimitiveTopology topology = PrimitiveTopology.TriangleList,
        CullMode cullMode = CullMode.Back,
        FrontFace frontFace = FrontFace.Ccw,
        bool unclippedDepth = false);

    void SetMultisampleState(int sampleCount = 1, uint sampleMask = uint.MaxValue, bool alphaToCoverageEnabled = false);

    void SetDepthStencilState(TextureFormat format, bool depthWriteEnabled = true, CompareFunction depthCompare = CompareFunction.Less);

    void AddColorTarget(TextureFormat format, BlendState? blend = null);
}
