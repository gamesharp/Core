namespace GameSharp.Collections;

internal readonly ref struct TypeLookupClusterIndex
{
    public int Index { get; init; }

    public IntegerLookup Lookup { get; init; }

    public ReadOnlySpan<object> Items { get; init; }
}
