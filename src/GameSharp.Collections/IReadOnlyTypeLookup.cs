using System.Diagnostics.CodeAnalysis;

namespace GameSharp.Collections;

/// <summary>
/// Represents an immutable lookup that associates <see cref="Type"/> keys with object instances.
/// </summary>
public interface IReadOnlyTypeLookup : ICollection<KeyValuePair<Type, object>>
{
    bool ICollection<KeyValuePair<Type, object>>.IsReadOnly => true;

    /// <param name="type">The type of items to retrieve.</param>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="type"/> is <see langword="null"/>.</exception>
    /// <inheritdoc cref="GetAll{T}()"/>
    IEnumerable<object> GetAll([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type);

    /// <summary>
    /// Retrieves all items of the specified type from the collection.
    /// </summary>
    /// <typeparam name="T">The type of items to retrieve from the collection.</typeparam>
    /// <returns>
    /// An enumerable sequence containing all contiguous items of the specified type found in the 
    /// collection; or an empty sequence if no such items are present.
    /// </returns>
    IEnumerable<T> GetAll<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>() where T : class;

    /// <summary>
    /// Retrieves the first object of the specified type from the collection.
    /// </summary>
    /// <returns>The first object of the specified type found in the collection.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if no object of the specified type is found.</exception>
    /// <inheritdoc cref="TryGetOne{T}(out T)"/>
    T GetOne<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>() where T : class;

    /// <typeparam name="T">The type of object to retrieve. Must be a reference type.</typeparam>
    /// <inheritdoc cref="TryGetOne(Type, out object)"/>
    bool TryGetOne<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>([NotNullWhen(true)] out T? result) where T : class;

    /// <summary>
    /// Attempts to retrieve the first object of the specified type from the collection.
    /// </summary>
    /// <param name="type">The type of object to retrieve.</param>
    /// <param name="item">
    /// When this method returns, contains the first instance of the specified type if one is found; otherwise, 
    /// <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if an object of the specified type was found and assigned to <paramref name="item"/>; 
    /// otherwise, <see langword="false"/>.
    /// </returns>
    bool TryGetOne([NotNullWhen(true)] Type? type, [NotNullWhen(true)] out object? item);

    /// <summary>
    /// Retrieves the first object of the specified type from the collection.
    /// </summary>
    /// <returns>The first object of the specified type found in the collection.</returns>
    /// <inheritdoc cref="TryGetOne(Type, out object)"/>
    /// <exception cref="ArgumentException">Thrown if no object of the specified type is found.</exception>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="type"/> is <see langword="null"/>.</exception>
    object GetOne(Type type);

    /// <typeparam name="T">The type of object to check for. Must be a reference type.</typeparam>
    /// <inheritdoc cref="Contains(object, Type)"/>
    bool Contains<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>( T item) where T : class;

    /// <summary>
    /// Determines whether the collection contains the specified item of the given type.
    /// </summary>
    /// <param name="item">The object to locate in the collection.</param>
    /// <param name="type">The type to use when searching for the item.</param>
    /// <returns>
    /// <see langword="true"/> if the item is found in the collection for the specified type; 
    /// otherwise, <see langword="false"/>.
    /// </returns>
    bool Contains(object item, Type type);

    bool ICollection<KeyValuePair<Type, object>>.Contains(KeyValuePair<Type, object> item)
    {
        return Contains(item.Value, item.Key);
    }

    void ICollection<KeyValuePair<Type, object>>.Clear()
    {
        throw new NotSupportedException();
    }

    void ICollection<KeyValuePair<Type, object>>.Add(KeyValuePair<Type, object> item)
    {
        throw new NotSupportedException();
    }

    bool ICollection<KeyValuePair<Type, object>>.Remove(KeyValuePair<Type, object> item)
    {
        throw new NotSupportedException();
    }
}
