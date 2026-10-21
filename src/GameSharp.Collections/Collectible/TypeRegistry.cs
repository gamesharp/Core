namespace GameSharp.Collections.Collectible;

internal abstract class TypeRegistry(short id) : Collections.TypeRegistry
{
    public short AssemblyID { get; } = id;

    protected override int NextID
    {
        get
        {
            TypeIdentifier identifier = new(AssemblyID, base.NextID);
            return identifier.CompositeKey;
        }
    }

    public virtual void Purge(short assemblyID)
    {
        foreach (TypeInfo info in Registrations.Values)
        {
            DerivedTypeCollection derived = info.Derived;
            derived.RemoveAll(assemblyID);
        }
    }
}
