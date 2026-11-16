namespace GameSharp.Rendering;

[Flags]
public enum ShaderStage : byte
{
    None = 0,
    Vertex = 1 << 0,
    Fragment = 1 << 1,
    Compute = 1 << 2,
    VertexFragment = Vertex | Fragment,
    All = Vertex | Fragment | Compute
}
