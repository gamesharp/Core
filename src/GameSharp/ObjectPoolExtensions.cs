using System.Runtime.CompilerServices;

namespace GameSharp;

/// <summary>
/// Provides extension methods for working with <see cref="ObjectPool{T}"/> instances.
/// </summary>
public static class ObjectPoolExtensions
{
    /// <summary>
    /// Initializes <paramref name="location1"/> with a new <typeparamref name="T"/> instance from the pool if it is <see langword="null"/>.
    /// </summary>
    /// <param name="pool">The <see cref="ObjectPool{T}"/> to get a new instance from.</param>
    /// <param name="location1">A reference to a <typeparamref name="T"/> variable that may be initialized.</param>
    /// <returns>A new <typeparamref name="T"/> instance from the pool or the existing one if it is already initialized.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="pool"/> is <see langword="null"/>.</exception>
    public static T InterlockedInit<T>(this ObjectPool<T> pool, scoped ref T? location1) where T : class
    {
        ArgumentNullException.ThrowIfNull(pool);

        T newValue = pool.Get();

        if (Interlocked.CompareExchange(ref location1, newValue, null) is { } oldValue)
        {
            pool.Return(newValue);
            return oldValue;
        }

        return newValue;
    }

    /// <summary>
    /// Atomically replaces the specified location with null and returns the previous value to the pool when a value was
    /// present.
    /// </summary>
    /// <typeparam name="T">The reference type stored in the pool.</typeparam>
    /// <param name="pool">The pool that receives the removed instance.</param>
    /// <param name="location1">A reference to the location containing the instance to return; set to <see langword="null"/> as part of the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="pool"/> is <see langword="null"/>.</exception>
    public static void InterlockedReturn<T>(this ObjectPool<T> pool, scoped ref T? location1) where T : class
    {
        ArgumentNullException.ThrowIfNull(pool);
        if (Interlocked.Exchange(ref location1, null) is { } oldValue)
        {
            pool.Return(oldValue);
        }
    }

    /// <summary>
    /// Atomically replaces a reference with <see langword="null"/> when it matches the expected instance, returns that
    /// instance to the pool, and clears the caller's reference.
    /// </summary>
    /// <typeparam name="T">The reference type stored in the pool.</typeparam>
    /// <param name="pool">The pool that receives the instance.</param>
    /// <param name="location1">The target reference to compare and conditionally set to <see langword="null"/>.</param>
    /// <param name="comparand">
    /// A reference to the expected instance; the instance is returned to the pool and the reference is cleared before
    /// returning.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="location1"/> was equal to <paramref name="comparand"/> and was set to
    /// <see langword="null"/>; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool InterlockedReturn<T>(this ObjectPool<T> pool, scoped ref T? location1, scoped ref T comparand) where T : class
    {
        ArgumentNullException.ThrowIfNull(pool);
        ArgumentNullException.ThrowIfNull(comparand);
        T? oldValue = Interlocked.CompareExchange(ref location1, null, comparand);
        bool result = ReferenceEquals(oldValue, comparand);
        pool.Return(comparand);
        comparand = ref Unsafe.NullRef<T>();
        return result;
    }
}
