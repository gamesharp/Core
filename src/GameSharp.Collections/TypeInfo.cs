using GameSharp.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace GameSharp.Collections;

internal sealed class TypeInfo
{
    private static readonly TypeRegistryProvider _registryProvider;

    public Type Type { get; }

    public int ID { get; }

    public DerivedTypeCollection Derived { get; }

    static TypeInfo()
    {
        if (RuntimeFeature.IsDynamicCodeSupported)
        {
            _registryProvider = new Collectible.TypeRegistryProvider();
        }
        else
        {
            _registryProvider = new Static.TypeRegistryProvider();
        }
    }

    internal TypeInfo(Type type, int id)
    {
        Type = type;
        ID = id;
        Derived = new DerivedTypeCollection(ID);
    }

    public override bool Equals(object? obj)
    {
        return obj is TypeInfo other && ID == other.ID;
    }

    public override int GetHashCode()
    {
        return ID.GetHashCode();
    }

    public static TypeInfo Get(int id)
    {
        TypeIdentifier identifier = (TypeIdentifier)id;
        TypeRegistry registry = _registryProvider.Get(identifier.AssemblyID);
        return registry.Get(identifier.TypeID);
    }

    public static TypeInfo Get<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>()
    {
        if (RuntimeFeature.IsDynamicCodeSupported && typeof(T).Assembly.IsCollectible)
        {
            return Get(typeof(T));
        }

        return TypeInfo<T>.Default;
    }

    public static TypeInfo Get([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        TypeRegistry registry = _registryProvider.GetOrAdd(type);

        if (registry.GetOrAdd(type, out TypeInfo? typeInfo))
        {
            return typeInfo;
        }

        for (Type? baseType = type.BaseType; baseType is { }; baseType = baseType.BaseType)
        {
            typeInfo.AddDerived(baseType);
        }

        foreach (Type iface in type.GetInterfaces())
        {
            typeInfo.AddDerived(iface);
        }

        return typeInfo;
    }

    public static bool TryGet([NotNullWhen(true)] Type? type, [NotNullWhen(true)] out TypeInfo? typeInfo)
    {
        if (type is { } && _registryProvider.TryGet(type, out TypeRegistry? registry))
        {
            return registry.TryGet(type, out typeInfo);
        }

        typeInfo = null;
        return false;
    }

    private void AddDerived(Type type)
    {
        TypeRegistry registry = _registryProvider.GetOrAdd(type);
        registry.GetOrAdd(type, out TypeInfo typeInfo);
        typeInfo.Derived.Add(ID);
    }
}
