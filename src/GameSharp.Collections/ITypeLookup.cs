using System.Diagnostics.CodeAnalysis;

namespace GameSharp.Collections;

/// <summary>
/// Represents a mutable lookup that associates <see cref="Type"/> keys with object instances.
/// </summary>
public interface ITypeLookup : IReadOnlyTypeLookup, ICollection<KeyValuePair<Type, object>>
{
    bool ICollection<KeyValuePair<Type, object>>.IsReadOnly => false;

    /// <typeparam name="T"> The type of the item to add. Must be a reference type.</typeparam>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="item"/> is <see langword="null"/>.</exception>
    /// <inheritdoc cref="Add(object, Type)"/>
    bool Add<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>(T item) where T : class;

    /// <summary>
    /// Adds the specified item to the collection.
    /// </summary>
    /// <param name="type">The type of the item to add.</param>
    /// <param name="item">The item to add to the collection. Cannot be <see langword="null"/>.</param>
    /// <returns>
    /// <see langword="true"/> if the item was successfully added; <see langword="false"/> if the item is
    /// already present.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if either the <paramref name="item"/> or the <paramref name="type"/> is <see langword="null"/>.
    /// </exception>
    bool Add(object item, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type);

    /// <typeparam name="T">The type of the item to remove. Must be a reference type.</typeparam>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="item"/> is <see langword="null"/>.</exception>
    /// <inheritdoc cref="Remove(object, Type)"/>
    public bool Remove<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>(T item) where T : class;

    /// <summary>
    /// Removes all occurrences of the specified item from the collection.
    /// </summary>
    /// <param name="type">The type of the item to remove.</param>
    /// <param name="item">The item to remove from the collection.</param>
    /// <returns>
    /// <see langword="true"/> if item was found and removed from the collection; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if either the <paramref name="item"/> or the <paramref name="type"/> is <see langword="null"/>.
    /// </exception> 
    bool Remove(object item, Type type);

    /// <summary>
    /// Replace an existing item with a specified new item.
    /// </summary>
    /// <param name="existingItem">The existing item to replace.</param>
    /// <param name="newItem">The new item to use as the replacement.</param>
    /// <param name="type">The type of the item to replace.</param>
    /// <returns><see langword="true"/> if <paramref name="newItem"/> was added to the collection; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="newItem"/> is <see langword="null"/>.</exception>
    bool Replace(object? existingItem, object newItem, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type);

    /// <inheritdoc cref="Replace(object?, object, Type)"/>
    /// <typeparam name="T">The type of item to replace.</typeparam>
    bool Replace<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>(T? existingItem, T newItem) where T : class;

    /// <inheritdoc cref="ICollection{T}.Clear"/>
    new void Clear();

    void ICollection<KeyValuePair<Type, object>>.Clear()
    {
        Clear();
    }

    void ICollection<KeyValuePair<Type, object>>.Add(KeyValuePair<Type, object> item)
    {
        Add(item.Value, item.Key);
    }

    bool ICollection<KeyValuePair<Type, object>>.Remove(KeyValuePair<Type, object> item)
    {
        return Remove(item.Value, item.Key);
    }
}
