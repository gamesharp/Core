using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GameSharp.Collections;

/// <summary>
/// Represents a contiguous cluster of items of the same type.
/// </summary>
public readonly ref struct TypeLookupCluster
{
    internal ReadOnlySpan<object> Items { get; }

    private readonly TypeInfo _typeInfo;
    /// <summary>
    /// Gets the type of the items in the cluster.
    /// </summary>
    public Type Type => _typeInfo.Type;

    /// <summary>
    /// Gets the number of items in the cluster.
    /// </summary>
    public int Count => Items.Length;

    internal TypeLookupCluster(TypeInfo typeInfo, ReadOnlySpan<object> items)
    {
        _typeInfo = typeInfo;
        Items = items;
    }

    /// <summary>
    /// Attempts to cast the items in the cluster to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to cast the items to.</typeparam>
    /// <param name="items">When this method returns, contains the items cast to the specified type, if the cast succeeded; otherwise, the default value.</param>
    /// <returns><see langword="true"/> if the items were successfully cast to the specified type; otherwise, <see langword="false"/>.</returns>
    public bool TryCast<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>(out ReadOnlySpan<T> items) where T : class
    {
        if (Is<T>())
        {
            ref object data = ref MemoryMarshal.GetReference(Items);
            items = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<object, T>(ref data), Items.Length);
            return true;
        }

        items = default;
        return false;
    }

    /// <summary>
    /// Casts the items in the cluster to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to cast the items to.</typeparam>
    /// <returns>A read-only span of the items cast to the specified type.</returns>
    /// <exception cref="InvalidCastException">Thrown if the items in the cluster cannot be cast to the specified type.</exception>
    public ReadOnlySpan<T> Cast<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>() where T : class
    {
        if (!Is<T>())
        {
            InvalidCast(typeof(T));
        }

        ref object data = ref MemoryMarshal.GetReference(Items);
        return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<object, T>(ref data), Items.Length);
    }

    /// <inheritdoc cref="Is(Type)"/>
    /// <typeparam name="T">The type to check for.</typeparam>
    public bool Is<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>() where T : class
    {
        TypeInfo typeInfo = TypeInfo.Get<T>();
        return typeInfo.EqualsOrAssignableFrom(_typeInfo);
    }

    /// <summary>
    /// Determines whether the cluster contains items of the specified type or any of its derived types.
    /// </summary>
    /// <param name="type">The type to check for.</param>
    /// <returns>
    /// <see langword="true"/> if the cluster contains items of the specified type or any of its derived types; 
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool Is([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type)
    {
        TypeInfo typeInfo = TypeInfo.Get(type);
        return typeInfo.EqualsOrAssignableFrom(_typeInfo);
    }

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator()"/>
    public ReadOnlySpan<object>.Enumerator GetEnumerator() => Items.GetEnumerator();

    [DoesNotReturn, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
    private void InvalidCast(Type targetType)
    {
        throw new InvalidCastException($"Cannot cast a cluster of type '{Type.FullName}' to '{targetType.FullName}'.");
    }

    /// <summary>
    /// Defines an implicit conversion from a <see cref="TypeLookupCluster"/> to a <see cref="ReadOnlySpan{T}"/> of objects.
    /// </summary>
    /// <param name="value">The cluster to convert.</param>
    public static implicit operator ReadOnlySpan<object>(TypeLookupCluster value) => value.Items;
}
