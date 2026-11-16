namespace GameSharp.Rendering;

public readonly struct PassTimestampWrites
{
    public required IQuerySet QuerySet { get; init; }
    public int? BeginningOfPassWriteIndex { get; init; }
    public int? EndOfPassWriteIndex { get; init; }
}
