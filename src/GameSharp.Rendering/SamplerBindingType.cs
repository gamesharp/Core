namespace GameSharp.Rendering;

public enum SamplerBindingType : byte
{
    /// <summary>
    /// Standard linear/anisotropic sampler
    /// </summary>
    Filtering,
    /// <summary>
    /// Point/nearest-neighbor sampler
    /// </summary>
    NonFiltering,
    /// <summary>
    /// Shadow-map depth comparison sampler
    /// </summary>
    Comparison
}
