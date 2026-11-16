namespace GameSharp.Rendering;

public enum StoreOp : byte
{
    /// <summary>
    /// Write results out to VRAM.
    /// </summary>
    Store,
    /// <summary>
    /// Discard after pass finishes.
    /// </summary>
    Discard
}
