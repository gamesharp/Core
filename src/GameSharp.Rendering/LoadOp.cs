namespace GameSharp.Rendering;

public enum LoadOp : byte
{
    /// <summary>
    /// Keep existing contents in VRAM.
    /// </summary>
    Load,
    /// <summary>
    /// Clear to ClearValue.
    /// </summary>
    Clear,
    /// <summary>
    /// Discard previous contents.
    /// </summary>
    DontCare
}
