namespace GameSharp.Rendering;

public readonly record struct VertexAttribute(int ShaderLocation, VertexFormat Format, int ByteOffset);
