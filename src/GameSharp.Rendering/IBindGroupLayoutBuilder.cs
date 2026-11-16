namespace GameSharp.Rendering;

public interface IBindGroupLayoutBuilder
{
    string? Label { get; set; }

    void AddBuffer(
        int binding,
        ShaderStage visibility,
        BufferBindingType type = BufferBindingType.Uniform,
        bool hasDynamicOffset = false,
        ulong minBindingSize = 0);

    void AddTexture(
        int binding,
        ShaderStage visibility,
        TextureSampleType sampleType = TextureSampleType.Float,
        bool isMultisampled = false);

    void AddStorageTexture(
        int binding,
        ShaderStage visibility,
        TextureFormat format,
        StorageTextureAccess access = StorageTextureAccess.WriteOnly);

    void AddSampler(
        int binding,
        ShaderStage visibility,
        SamplerBindingType type = SamplerBindingType.Filtering);
}