namespace GameSharp.Collections.Immutable;

/// <summary>
/// Defines a transformation that creates a new <see cref="ImmutableTypeLookup"/> from an existing one.
/// </summary>
public interface IImmutableTypeLookupTransformer
{
    /// <summary>
    /// Applies the current operation to the specified immutable type lookup.
    /// </summary>
    /// <remarks>This method should be side-effect free, as it may run multiple times when races occur with other threads.</remarks>
    /// <param name="typeLookup">The immutable type lookup to apply the operation to.</param>
    /// <returns>A new <see cref="ImmutableTypeLookup"/> instance that reflects the applied operation.</returns>
    ImmutableTypeLookup Apply(ImmutableTypeLookup typeLookup);
}
