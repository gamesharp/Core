using System.Diagnostics.CodeAnalysis;

namespace GameSharp.Collections;

/// <summary>
/// Represents a mutable lookup that stores instances keyed by their runtime type.
/// </summary>
public abstract class MutableTypeLookup : ReadOnlyTypeLookup
{
    /// <inheritdoc cref="ITypeLookup.Add{T}(T)"/>
    public bool Add<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>(T item) where T : class
    {
        ArgumentNullException.ThrowIfNull(item);
        TypeInfo typeInfo = TypeInfo.Get<T>();
        return Add(item, typeInfo);
    }
    /// <inheritdoc cref="ITypeLookup.Add(object, Type)"/>
    public bool Add(object item, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(type);
        TypeInfo typeInfo = TypeInfo.Get(type);
        return Add(item, typeInfo);
    }
    private protected abstract bool Add(object item, TypeInfo typeInfo);

    /// <inheritdoc cref="ITypeLookup.Remove{T}(T)"/>
    public bool Remove<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>(T item) where T : class
    {
        ArgumentNullException.ThrowIfNull(item);
        TypeInfo typeInfo = TypeInfo.Get<T>();
        return Remove(item, typeInfo);
    }
    /// <inheritdoc cref="ITypeLookup.Remove(object, Type)"/>
    public bool Remove(object item, Type type)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(type);
        return TypeInfo.TryGet(type, out TypeInfo? typeInfo) && Remove(item, typeInfo);
    }
    private protected abstract bool Remove(object item, TypeInfo typeInfo);

    /// <inheritdoc cref="ITypeLookup.Replace(object, object, Type)"/>
    public bool Replace(object? existingItem, object newItem, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type)
    {
        ArgumentNullException.ThrowIfNull(newItem);
        TypeInfo typeInfo = TypeInfo.Get(type);
        return existingItem is { } ? Replace(existingItem, newItem, typeInfo) : Add(newItem, typeInfo);
    }
    /// <inheritdoc cref="ITypeLookup.Replace{T}(T, T)"/>
    public bool Replace<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>(T? existingItem, T newItem) where T : class
    {
        ArgumentNullException.ThrowIfNull(newItem);
        TypeInfo typeInfo = TypeInfo.Get<T>();
        return existingItem is { } ? Replace(existingItem, newItem, typeInfo) : Add(newItem, typeInfo);
    }
    private protected abstract bool Replace(object existingItem, object newItem, TypeInfo typeInfo);
}
