namespace GameSharp.Rendering;

public enum BufferBindingType : byte
{
    /// <summary>
    /// CBV in D3D12 / uniform in WGSL
    /// </summary>
    Uniform,
    /// <summary>
    /// UAV in D3D12 / storage (read_write) in WGSL)
    /// </summary>
    Storage,
    /// <summary>
    /// SRV in D3D12 / storage (read) in WGSL
    /// </summary>
    ReadOnlyStorage
}
