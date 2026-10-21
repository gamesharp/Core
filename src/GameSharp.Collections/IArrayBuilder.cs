namespace GameSharp.Collections;

internal interface IArrayBuilder<T>
{
    T[] Array { get; }

    int LogicalCount { get; set; }

    void Mutate();
}
