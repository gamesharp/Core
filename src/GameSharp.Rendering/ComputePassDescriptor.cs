namespace GameSharp.Rendering;

public readonly struct ComputePassDescriptor
{
    public string? Label { get; init; }
    public PassTimestampWrites? TimestampWrites { get; init; }
}