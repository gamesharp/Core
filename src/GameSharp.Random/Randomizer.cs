using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace GameSharp.Random;

/// <summary>
/// Provides thread-safe utilities for generating random values, selecting collection elements, and managing state with various distribution biases.
/// </summary>
public static class Randomizer
{
    private readonly struct EnumValueProvider<T> where T : struct, Enum
    {
        private static readonly T[] _values = Enum.GetValues<T>();
        public static T Normalize(uint random) => _values[random % (uint)_values.Length];
    }

    private const float TwoPi = 2.0f * MathF.PI;
    private const float DegreesToRadians = MathF.PI / 180.0f;

    [ThreadStatic]
    private static Seed _threadLocal;
    /// <summary>
    /// Gets the default <see cref="Seed"/> instance for the current thread.
    /// </summary>
    public static Seed CurrentSeed => _threadLocal;

    /// <summary>
    /// Generates a pseudo-random floating-point number and updates the local state for the current thread.
    /// </summary>
    /// <remarks>This method may advance the local <see cref="Seed"/> for the current thread to the next state in the sequence.</remarks>
    /// <returns>A new <see cref="float"/> with a value greater than or equal to 0.0 and less than 1.0.</returns>
    public static float NextFloat() => (float)++_threadLocal;

    /// <remarks>This method may advance the local <see cref="Seed"/> for the current thread to the next state in the sequence.</remarks>
    /// <inheritdoc cref="NextGaussian(ref Seed, float, float)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float NextGaussian(float mean = 0.0f, float standardDeviation = 1.0f) => NextGaussian(ref _threadLocal, mean, standardDeviation);

    /// <summary>
    /// Generates a random floating-point value that follows a Gaussian (normal) distribution.
    /// </summary>
    /// <param name="mean">The mean value around which the Gaussian distribution is centered. Defaults to 0.0.</param>
    /// <param name="standardDeviation">The standard deviation that determines the spread of the distribution. Defaults to 1.0.</param>
    /// <returns>
    /// A new <see cref="float"/> sampled from the Gaussian distribution defined by the specified <paramref name="mean"/> 
    /// and <paramref name="standardDeviation"/>.
    /// </returns>
    /// <inheritdoc cref="NextUnitVector3(ref Seed)"/>
    /// <param name="seed"/>
    public static float NextGaussian(ref Seed seed, float mean = 0.0f, float standardDeviation = 1.0f)
    {
        return mean + MathF.Sqrt(-2.0f * MathF.Log((float)++seed)) * MathF.Cos(TwoPi * (float)++seed) * standardDeviation;
    }

    /// <summary>Generates a pseudo-random integer.</summary>
    /// <remarks>This method may advance the local <see cref="Seed"/> for the current thread to the next state in the sequence.</remarks>
    /// <returns>A new 32-bit signed integer set to a random value.</returns>
    public static int NextInteger() => (int)++_threadLocal;

    /// <remarks>This method may advance the local <see cref="Seed"/> for the current thread to the next state in the sequence.</remarks>
    /// <inheritdoc cref="NextLong(ref Seed)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long NextLong() => NextLong(ref _threadLocal);

    /// <summary>Generates a pseudo-random integer.</summary>
    /// <returns>A new 64-bit signed integer set to a random value.</returns>
    /// <inheritdoc cref="NextUnitVector3(ref Seed)"/>
    public static long NextLong(ref Seed seed) => ((int)++seed << 32) | (long)++seed;

    /// <returns>A new 16-bit signed integer set to a random value.</returns>
    /// <inheritdoc cref="NextInteger()"/>
    public static short NextShort() => (short)++_threadLocal;

    /// <returns>A new 8-bit unsigned integer set to a random value.</returns>
    /// <inheritdoc cref="NextInteger()"/>
    public static byte NextByte() => (byte)++_threadLocal;

    /// <remarks>This method may advance the local <see cref="Seed"/> for the current thread to the next state in the sequence.</remarks>
    /// <inheritdoc cref="NextEnum{T}(Seed)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T NextEnum<T>() where T : unmanaged, Enum => NextEnum<T>(++_threadLocal);

    /// <summary>
    /// Generates a pseudo-random value of the specified enum type.
    /// </summary>
    /// <typeparam name="T">
    /// The enum type for which to generate a random value. Must be an enum.
    /// </typeparam>
    /// <returns>A new <typeparamref name="T"/> set to a random value.</returns>
    /// <inheritdoc cref="Condition(Seed, Chance)"/>
    public static T NextEnum<T>(Seed seed) where T : struct, Enum
    {
        return EnumValueProvider<T>.Normalize((uint)seed.Current);
    }

    /// <remarks>This method may advance the local <see cref="Seed"/> for the current thread to the next state in the sequence.</remarks>
    /// <inheritdoc cref="NextRotation(ref Seed)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion NextRotation() => NextRotation(ref _threadLocal);

    /// <summary>
    /// Generates a pseudo-random rotation as a quaternion.
    /// </summary>
    /// <returns>A <see cref="Quaternion"/> representing a rotation derived from random values.</returns>
    /// <inheritdoc cref="NextUnitVector3(ref Seed)"/>
    public static Quaternion NextRotation(ref Seed seed)
    {
        Vector3 euler = (NextUnitVector3(ref seed) * 360.0f) - new Vector3(180.0f);
        return Quaternion.CreateFromYawPitchRoll(euler.X * DegreesToRadians, euler.Y * DegreesToRadians, euler.Z * DegreesToRadians);
    }

    /// <remarks>This method may advance the local <see cref="Seed"/> for the current thread to the next state in the sequence.</remarks>
    /// <inheritdoc cref="NextUnitVector4(ref Seed)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 NextUnitVector4() => NextUnitVector4(ref _threadLocal);

    /// <summary>
    /// Generates a pseudo-random, four-dimensional vector.
    /// </summary>
    /// <returns>A new <see cref="Vector4"/>, where each component is a random floating-point value.</returns>
    /// <inheritdoc cref="NextUnitVector3(ref Seed)"/>
    public static Vector4 NextUnitVector4(ref Seed seed)
    {
        Vector4 abs = new((float)++seed, (float)++seed, (float)++seed, (float)++seed);
        Vector4 unit = (abs * 2.0f) - Vector4.One;
        return unit * MathF.ReciprocalSqrtEstimate(unit.LengthSquared());
    }

    /// <summary>
    /// Generates a pseudo-random, three-dimensional vector.
    /// </summary>
    /// <remarks>This method may advance the local <see cref="Seed"/> for the current thread to the next state in the sequence.</remarks>
    /// <inheritdoc cref="NextUnitVector3(ref Seed)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 NextUnitVector3() => NextUnitVector3(ref _threadLocal);

    /// <summary>
    /// Generates a pseudo-random, three-dimensional vector.
    /// </summary>
    /// <remarks>
    /// This method may advance the value of the specified <paramref name="seed"/> to the 
    /// next state in the sequence.
    /// </remarks>
    /// <returns>
    /// A new <see cref="Vector3"/>, where each component is a random floating-point value. 
    /// The vector is normalized to ensure it has a length of 1.
    /// </returns>
    /// <inheritdoc cref="Condition(Seed, Chance)"/>
    public static Vector3 NextUnitVector3(ref Seed seed)
    {
        Vector3 unit = (new Vector3((float)++seed, (float)++seed, (float)++seed) * 2.0f) - Vector3.One;
        float invLength = MathF.ReciprocalSqrtEstimate(unit.LengthSquared());
        return unit * invLength;
    }

    /// <remarks>This method may advance the local <see cref="Seed"/> for the current thread to the next state in the sequence.</remarks>
    /// <inheritdoc cref="NextUnitVector2(ref Seed)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 NextUnitVector2() => NextUnitVector2(ref _threadLocal);

    /// <summary>
    /// Generates a pseudo-random, two-dimensional vector.
    /// </summary>
    /// <returns>
    /// A new <see cref="Vector2"/>, where each component is a random floating-point value. 
    /// The vector is normalized to ensure it has a length of 1.
    /// </returns>
    /// <inheritdoc cref="NextUnitVector3(ref Seed)"/>
    public static Vector2 NextUnitVector2(ref Seed seed)
    {
        Vector2 unit = (new Vector2((float)++seed, (float)++seed) * 2.0f) - Vector2.One;
        float invLength = MathF.ReciprocalSqrtEstimate(unit.LengthSquared());
        return unit * invLength;
    }

    /// <remarks>This method may advance the local <see cref="Seed"/> for the current thread to the next state in the sequence.</remarks>
    /// <inheritdoc cref="NextUnitCircleVector2(Seed, float)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 NextUnitCircleVector2(float radius = 1.0f) => NextUnitCircleVector2(++_threadLocal, radius);

    /// <summary>
    /// Generates a random <see cref="Vector2"/> point on a circle of a given radius.
    /// </summary>
    /// <param name="radius">Radius of circle. Default 1.0.</param>
    /// <returns>A random <see cref="Vector2"/>.</returns>
    /// <inheritdoc cref="Condition(Seed, Chance)"/>
    /// <param name="seed"/>
    public static Vector2 NextUnitCircleVector2(Seed seed, float radius = 1.0f)
    {
        float randomRadian = (float)seed * TwoPi;
        return new Vector2(MathF.Cos(randomRadian) * radius, MathF.Sin(randomRadian) * radius);
    }

    /// <remarks>This method may advance the local <see cref="Seed"/> for the current thread to the next state in the sequence.</remarks>
    /// <inheritdoc cref="Choose{T}(Seed, ReadOnlySpan{T})"/>
    public static T Choose<T>(ReadOnlySpan<T> items) => Choose(++_threadLocal, items);

    /// <summary>
    /// Selects a <typeparamref name="T"/> from <paramref name="items"/> using 
    /// a uniformly distributed, pseudo-random, floating-point number.
    /// </summary>
    /// <typeparam name="T">The type of elements contained in the collection.</typeparam>
    /// <param name="items">The collection of items from which to select a random element. Cannot be empty.</param>
    /// <returns>A randomly selected <typeparamref name="T"/> from the collection.</returns>
    /// <exception cref="ArgumentException">Thrown when the <paramref name="items"/> collection is empty.</exception>
    /// <inheritdoc cref="NextUnitVector3(ref Seed)"/>
    /// <param name="seed"/>
    public static T Choose<T>(Seed seed, ReadOnlySpan<T> items)
    {
        ThrowIfNullOrEmpty(items);
        return items[UniformRange(seed, items.Length)];
    }

    /// <remarks>This method may advance the local <see cref="Seed"/> for the current thread to the next state in the sequence.</remarks>
    /// <inheritdoc cref="Choose{T}(Seed, IList{T})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Choose<T>(IList<T> items) => Choose(++_threadLocal, items);

    /// <remarks>This method may advance the local <see cref="Seed"/> for the current thread to the next state in the sequence.</remarks>
    /// <inheritdoc cref="Choose{T}(Seed, IList{T}, int, int)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Choose<T>(IList<T> items, int offset, int length)
    {
        return Choose(++_threadLocal, items, offset, length);
    }

    /// <param name="offset">
    /// The zero-based index at which to start the selection range within 
    /// the <paramref name="items"/> collection.
    /// </param>
    /// <param name="length">
    /// The number of elements in the selection range starting from the 
    /// <paramref name="offset"/>.
    /// </param>
    /// <inheritdoc cref="Choose{T}(Seed, ReadOnlySpan{T})"/>
    /// <param name="seed"/>
    /// <param name="items"/>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="items"/> collection is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the offset and length are out of bounds.</exception>
    public static T Choose<T>(Seed seed, IList<T> items, int offset, int length)
    {
        ThrowIfNullOrEmpty(items, out int count);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)offset, (uint)count);
        int end = count - offset;
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)length, (uint)end);
        return items[UniformRange(seed, end, offset)];
    }

    /// <inheritdoc cref="Choose{T}(Seed, ReadOnlySpan{T})"/>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="items"/> collection is <see langword="null"/>.</exception>
    public static T Choose<T>(Seed seed, IList<T> items)
    {
        ThrowIfNullOrEmpty(items, out int count);
        return items[UniformRange(seed, count)];
    }

    /// <inheritdoc cref="Choose{T}(IList{T})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Choose<T>(IReadOnlyList<T> items) => Choose(++_threadLocal, items);

    /// <inheritdoc cref="Choose{T}(IList{T}, int, int)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Choose<T>(IReadOnlyList<T> items, int offset, int length) => Choose(++_threadLocal, items, offset, length);

    /// <inheritdoc cref="Choose{T}(Seed, IList{T}, int, int)"/>
    public static T Choose<T>(Seed seed, IReadOnlyList<T> items, int offset, int length)
    {
        ThrowIfNullOrEmpty(items, out int count);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)offset, (uint)count);
        int end = count - offset;
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)length, (uint)end);
        return items[UniformRange(seed, end, offset)];
    }

    /// <inheritdoc cref="Choose{T}(Seed, ReadOnlySpan{T})"/>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="items"/> collection is <see langword="null"/>.</exception>
    public static T Choose<T>(Seed seed, IReadOnlyList<T> items)
    {
        ThrowIfNullOrEmpty(items, out int count);
        return items[UniformRange(seed, count)];
    }

    /// <inheritdoc cref="Condition(Seed, Chance)"/>
    /// <inheritdoc cref="NextInteger()"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Condition(Chance chance = Chance.Even) => Condition(++_threadLocal, chance);

    /// <summary>
    /// Evaluates a probabilistic condition based on the specified <paramref name="chance"/>.
    /// </summary>
    /// <param name="chance">
    /// The probability of the condition being <see langword="true"/>. 
    /// Defaults to <see cref="Chance.Even"/> if not specified.
    /// </param>
    /// <param name="seed">The <see cref="Seed"/> instance to use for generating the random value.</param>
    /// <returns>
    /// <see langword="true"/> if the condition is met according to the evaluated chance; 
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool Condition(Seed seed, Chance chance = Chance.Even) => unchecked((uint)seed.Current) < (uint)chance;

    /// <inheritdoc cref="Fluctuate{T}(Seed, T, T)"/>
    /// <inheritdoc cref="NextInteger()"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Fluctuate<T>(T value, T maxDeviation) where T : INumberBase<T> => Fluctuate(++_threadLocal, value, maxDeviation);

    /// <summary>
    /// Applies a random fluctuation to the specified value within the range defined by the maximum deviation.
    /// </summary>
    /// <param name="value">The base value to which the random fluctuation will be applied.</param>
    /// <param name="maxDeviation">The maximum allowable deviation from the base value. Determines the range within which the value may fluctuate.</param>
    /// <returns>A value that is randomly adjusted from the original value by an amount within the specified maximum deviation.</returns>
    /// <typeparam name="T">Specifies the numeric type of the value and maximum deviation. Must implement <see cref="INumberBase{TSelf}"/>.</typeparam>
    /// <inheritdoc cref="Condition(Seed, Chance)"/>
    /// <param name="seed"/>
    public static T Fluctuate<T>(Seed seed, T value, T maxDeviation) where T : INumberBase<T>
    {
        float factor = ToSignedRange((float)seed);
        return value + (T.CreateTruncating(factor) * maxDeviation);
    }


    /// <remarks>This method may advance the local <see cref="Seed"/> for the current thread to the next state in the sequence.</remarks>
    /// <inheritdoc cref="Shuffle{T}(Span{T}, ref Seed)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Shuffle<T>(this Span<T> items) => Shuffle(items, ref _threadLocal);

    /// <summary>
    /// Randomly rearranges the elements within the specified collection in place.
    /// </summary>
    /// <typeparam name="T">The type of elements contained in the collection.</typeparam>
    /// <param name="items">A span of items to shuffle. The span must contain at least one element.</param>
    /// <returns/>
    /// <inheritdoc cref="NextUnitVector3(ref Seed)"/>
    /// <param name="seed"/>
    public static void Shuffle<T>(this Span<T> items, ref Seed seed)
    {
        for (int i = items.Length - 1; i > 0; i--)
        {
            int j = UniformRange(++seed, i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }

    /// <remarks>This method may advance the local <see cref="Seed"/> for the current thread to the next state in the sequence.</remarks>
    /// <inheritdoc cref="Shuffle{T}(IList{T}, ref Seed)"/>
    public static void Shuffle<T>(this IList<T> items) => Shuffle(items, ref _threadLocal);

    /// <inheritdoc cref="Shuffle{T}(Span{T})"/>
    /// <returns/>
    /// <inheritdoc cref="NextUnitVector3(ref Seed)"/>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="items"/> collection is <see langword="null"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Shuffle<T>(this IList<T> items, ref Seed seed)
    {
        ArgumentNullException.ThrowIfNull(items);
        for (int i = items.Count - 1; i > 0; i--)
        {
            int j = UniformRange(++seed, i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }

    /// <remarks>This method may advance the local <see cref="Seed"/> for the current thread to the next state in the sequence.</remarks>
    /// <inheritdoc cref="UniformRange{T}(Seed, T, T)"/>
    public static T UniformRange<T>(T min, T max) where T : IFloatingPoint<T>
    {
        return UniformRange(++_threadLocal, max, min);
    }

    /// <summary>
    /// Generates a random <typeparamref name="T"/> that is uniformly distributed within the specified range.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the values defining the range. 
    /// Must implement <see cref="INumberBase{TSelf}"/>.
    /// </typeparam>
    /// <param name="min">
    /// The inclusive lower bound of the range. 
    /// The generated value will be greater than or equal to this value.
    /// </param>
    /// <param name="max">
    /// The exclusive upper bound of the range. 
    /// The generated value will be less than this value.
    /// </param>
    /// <returns>
    /// A random <typeparamref name="T"/> that is greater than or equal to <paramref name="min"/> 
    /// and less than <paramref name="max"/>.
    /// </returns>
    /// <inheritdoc cref="Condition(Seed, Chance)"/>
    /// <param name="seed"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T UniformRange<T>(Seed seed, T max, T min = default) where T : IFloatingPoint<T>
    {
        return Range(min, max, T.CreateTruncating((float)seed));
    }

    /// <remarks>This method may advance the local <see cref="Seed"/> for the current thread to the next state in the sequence.</remarks>
    /// <inheritdoc cref="UniformRange(Seed, int, int)"/>
    public static int UniformRange(int max, int min = default) => UniformRange(++_threadLocal, max, min);

    /// <summary>
    /// Generates a pseudo-random integer that is uniformly distributed within the specified range.
    /// </summary>
    /// <returns>An integer greater than or equal to <paramref name="min"/> and less than <paramref name="max"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="min"/> is greater than or equal to <paramref name="max"/>.</exception>
    /// <inheritdoc cref="UniformRange{T}(Seed, T, T)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UniformRange(Seed seed, int max, int min = default)
    {
        uint randomValue = unchecked((uint)seed.Current);
        return UniformRange(randomValue, min, max, 32);
    }

    /// <remarks>This method may advance the local <see cref="Seed"/> for the current thread to the next state in the sequence.</remarks>
    /// <inheritdoc cref="UniformRange(ref Seed, long, long)"/>
    public static long UniformRange(long max, long min = default) => UniformRange(ref _threadLocal, max, min);

    /// <inheritdoc cref="UniformRange(Seed, int, int)"/>
    /// <inheritdoc cref="NextUnitVector2(ref Seed)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long UniformRange(ref Seed seed, long max, long min = default)
    {
        ulong randomValue = unchecked((ulong)NextLong(ref seed));
        return UniformRange(randomValue, min, max, 64);
    }

    /// <remarks>This method may advance the local <see cref="Seed"/> for the current thread to the next state in the sequence.</remarks>
    /// <inheritdoc cref="UniformRange(Seed, short, short)"/>
    public static short UniformRange(short max, short min = default) => UniformRange(++_threadLocal, max, min);

    /// <inheritdoc cref="UniformRange(Seed, int, int)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short UniformRange(Seed seed, short max, short min = default)
    {
        ushort randomValue = unchecked((ushort)(short)seed);
        return UniformRange(randomValue, min, max, 16);
    }

    /// <remarks>This method may advance the local <see cref="Seed"/> for the current thread to the next state in the sequence.</remarks>
    /// <inheritdoc cref="UniformRange(Seed, byte, byte)"/>
    public static byte UniformRange(byte max, byte min = default) => UniformRange(++_threadLocal, max, min);

    /// <inheritdoc cref="UniformRange(Seed, int, int)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte UniformRange(Seed seed, byte max, byte min = default)
    {
        byte randomValue = (byte)seed;
        return UniformRange<byte, ushort>(randomValue, min, max, 8);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TValue UniformRange<TValue, TWidened>(TWidened random, TValue min, TValue max, int shift)
        where TValue : struct, IBinaryInteger<TValue>
        where TWidened : struct, IBinaryInteger<TWidened>
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(min, max);
        TWidened range = TWidened.CreateTruncating(++max - min);
        return min + TValue.CreateTruncating((random * range) >> shift);
    }

    /// <remarks>This method may advance the local <see cref="Seed"/> for the current thread to the next state in the sequence.</remarks>
    /// <inheritdoc cref="MedianBiasedRange{T}(T, T, Seed)"/>
    public static T MedianBiasedRange<T>(T min, T max) where T : IFloatingPoint<T>
    {
        return MedianBiasedRange(min, max, ++_threadLocal);
    }

    /// <summary>
    /// Generates a random <typeparamref name="T"/> that is biased towards the median of the specified range.
    /// </summary>
    /// <inheritdoc cref="UniformRange{T}(Seed, T, T)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T MedianBiasedRange<T>(T min, T max, Seed seed) where T : IFloatingPoint<T>
    {
        float s = ToSignedRange((float)seed);
        float biasedSeed = s * MathF.Abs(s);
        T normalizedSeed = T.CreateTruncating((biasedSeed + 1.0f) * 0.5f);
        return Range(min, max, normalizedSeed);
    }

    /// <remarks>This method may advance the local <see cref="Seed"/> for the current thread to the next state in the sequence.</remarks>
    /// <inheritdoc cref="ExtremesBiasedRange{T}(T, T, Seed)"/>
    public static T ExtremesBiasedRange<T>(T min, T max) where T : IFloatingPoint<T> => ExtremesBiasedRange(min, max, ++_threadLocal);

    /// <summary>
    /// Generates a random <typeparamref name="T"/> that is biased towards the extremes (minimum and maximum) of the specified range.
    /// </summary>
    /// <inheritdoc cref="UniformRange{T}(Seed, T, T)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ExtremesBiasedRange<T>(T min, T max, Seed seed) where T : IFloatingPoint<T>
    {
        float s = ToSignedRange((float)seed);
        float abs = MathF.Abs(s);
        float outward = 1.0f - ((1.0f - abs) * (1.0f - abs));
        float biasedSeed = MathF.CopySign(outward, s);
        T normalizedSeed = T.CreateTruncating((biasedSeed + 1.0f) * 0.5f);
        return Range(min, max, normalizedSeed);
    }

    /// <remarks>This method may advance the local <see cref="Seed"/> for the current thread to the next state in the sequence.</remarks>
    /// <inheritdoc cref="MaxBiasedRange{T}(T, T, Seed)"/>
    public static T MaxBiasedRange<T>(T min, T max) where T : IFloatingPoint<T> => MaxBiasedRange(min, max, ++_threadLocal);

    /// <summary>
    /// Generates a random <typeparamref name="T"/> that is biased towards the maximum value of the specified range.
    /// </summary>
    /// <inheritdoc cref="UniformRange{T}(Seed, T, T)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T MaxBiasedRange<T>(T min, T max, Seed seed) where T : IFloatingPoint<T>
    {
        float factor = (float)seed;
        return Range(max, min, T.CreateTruncating(factor * factor));
    }

    /// <remarks>This method may advance the local <see cref="Seed"/> for the current thread to the next state in the sequence.</remarks>
    /// <inheritdoc cref="MinBiasedRange{T}(T, T, Seed)"/>
    public static T MinBiasedRange<T>(T min, T max) where T : IFloatingPoint<T> => MinBiasedRange(min, max, ++_threadLocal);

    /// <summary>
    /// Generates a random <typeparamref name="T"/> that is biased towards the minimum value of the specified range.
    /// </summary>
    /// <inheritdoc cref="UniformRange{T}(Seed, T, T)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T MinBiasedRange<T>(T min, T max, Seed seed) where T : IFloatingPoint<T>
    {
        float factor = (float)seed;
        return Range(min, max, T.CreateTruncating(factor * factor));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T Range<T>(T min, T max, T factor) where T : IFloatingPoint<T> => min + (factor * (++max - min));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float ToSignedRange(this float state) => (state * 2.0f) - 1.0f;

    [StackTraceHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ThrowIfNullOrEmpty<T>(ReadOnlySpan<T> items, [CallerArgumentExpression(nameof(items))] string? paramName = null)
    {
        if (items.IsEmpty)
        {
            EmptyCollection(paramName);
        }
    }

    [StackTraceHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ThrowIfNullOrEmpty<T>([NotNull] IEnumerable<T>? items,
        out int count,
        [CallerArgumentExpression(nameof(items))] string? paramName = null)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (!items.TryGetNonEnumeratedCount(out count) || count == 0)
        {
            EmptyCollection(paramName);
        }
    }

    [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
    private static void EmptyCollection(string? paramName)
    {
        throw new ArgumentException("The collection must contain at least one item.", paramName);
    }
}
