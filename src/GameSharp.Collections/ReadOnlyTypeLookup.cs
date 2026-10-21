using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace GameSharp.Collections;

/// <summary>
/// Represents a read-only view over a collection of instances keyed by their runtime type.
/// </summary>
public abstract class ReadOnlyTypeLookup
{
    /// <inheritdoc/>
    public abstract int Count { get; }

    /// <inheritdoc cref="IReadOnlyTypeLookup.Contains{T}(T)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>(T item) where T : class
    {
        return Contains(item, TypeInfo.Get<T>());
    }

    /// <inheritdoc cref="IReadOnlyTypeLookup.Contains(object, Type)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(object item, Type type)
    {
        return TypeInfo.TryGet(type, out TypeInfo? typeInfo) && Contains(item, typeInfo);
    }

    private protected abstract bool Contains(object item, TypeInfo typeInfo);

    /// <inheritdoc cref="IReadOnlyTypeLookup.GetOne{T}()"/>
    public T GetOne<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>() where T : class
    {
        if (!TryGetOne(out T? result))
        {
            ItemOfTypeWasNotFound(nameof(T));
        }

        return result;
    }

    /// <inheritdoc cref="IReadOnlyTypeLookup.GetOne(Type)"/>
    public object GetOne(Type type)
    {
        if (!TryGetOne(type, out object? result))
        {
            ItemOfTypeWasNotFound(nameof(type));
        }

        return result;
    }

    /// <inheritdoc cref="IReadOnlyTypeLookup.TryGetOne{T}(out T)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetOne<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>([NotNullWhen(true)] out T? result) where T : class
    {
        return TryGetOne(TypeInfo.Get<T>(), out result);
    }

    /// <inheritdoc cref="IReadOnlyTypeLookup.TryGetOne(Type, out object)"/>
    public bool TryGetOne([NotNullWhen(true)] Type? type, [NotNullWhen(true)] out object? item)
    {
        if (TypeInfo.TryGet(type, out TypeInfo? typeInfo))
        {
            return TryGetOne(typeInfo, out item);
        }

        item = null;
        return false;
    }

    internal static bool TryGetOne<T>(ReadOnlySpan<IntegerLookup> lookups, ReadOnlySpan<object> items, TypeInfo typeInfo, [MaybeNullWhen(false)] out T item) 
        where T : class
    {
        DerivedTypeCollection.Enumerator enumerator = typeInfo.Derived.GetEnumerator();
        int startIndex = 0, endIndex = lookups.Length, pivot = enumerator.ID;
        while (enumerator.MoveNext())
        {
            if (enumerator.Current < pivot)
            {
                endIndex = startIndex;
                startIndex = 0;
                pivot = enumerator.Current;
            }

            ref IntegerLookup lookup = ref lookups[startIndex..endIndex].GetValueRefOrInsertionPoint(enumerator.Current, out int byteOffset, out bool exists);
            if (exists)
            {
                item = Unsafe.As<T>(items[lookup.index]);
                return true;
            }
            startIndex += IntegerLookup.ElementOffset(byteOffset);
        }

        item = null;
        return false;
    }

    private protected abstract bool TryGetOne<T>(TypeInfo typeInfo, [NotNullWhen(true)] out T? item) where T : class;

    [DoesNotReturn, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
    private static void ItemOfTypeWasNotFound(string? paramName = null)
    {
        throw new ArgumentException("No items of the specified type were present the collection.", paramName);
    }
}