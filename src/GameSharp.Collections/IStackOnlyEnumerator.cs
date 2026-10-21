namespace GameSharp.Collections;

internal interface IStackOnlyEnumerator<T> where T : allows ref struct
{
    T Current { get; }

    bool MoveNext();
}
