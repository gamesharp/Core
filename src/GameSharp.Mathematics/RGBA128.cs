using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace GameSharp.Mathematics;

/// <summary>
/// Represents a colour with red, green, blue, and alpha components stored as single-precision floating-point values.
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 16)]
public struct RGBA128 : IEquatable<RGBA128>,
    IFormattable,
    IEqualityOperators<RGBA128, RGBA128, bool>,
    IAdditionOperators<RGBA128, RGBA128, RGBA128>,
    ISubtractionOperators<RGBA128, RGBA128, RGBA128>,
    IMultiplyOperators<RGBA128, RGBA128, RGBA128>,
    IDivisionOperators<RGBA128, RGBA128, RGBA128>,
    IAdditiveIdentity<RGBA128, RGBA128>,
    IMultiplicativeIdentity<RGBA128, RGBA128>
{
    private const int Count = 4;

    /// <include file='RGBAX.xml' path='doc/members[@name="RGBAX"]/Transparent/*'/>
    public static RGBA128 Transparent => new(r: 0.0f, g: 0.0f, b: 0.0f, a: 0.0f);

    /// <include file='RGBAX.xml' path='doc/members[@name="RGBAX"]/White/*'/>
    public static RGBA128 White => new(r: 1.0f, g: 1.0f, b: 1.0f, a: 1.0f);

    private readonly Vector128<float> Vector
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Unsafe.BitCast<RGBA128, Vector128<float>>(this);
    }

    /// <include file='RGBAX.xml' path='doc/members[@name="RGBAX"]/R/*'/>
    public float R;

    /// <include file='RGBAX.xml' path='doc/members[@name="RGBAX"]/G/*'/>
    public float G;

    /// <include file='RGBAX.xml' path='doc/members[@name="RGBAX"]/B/*'/>
    public float B;

    /// <include file='RGBAX.xml' path='doc/members[@name="RGBAX"]/A/*'/>
    public float A;

    /// <include file='RGBAX.xml' path='doc/members[@name="RGBAX"]/Indexer/*'/>
    public float this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get
        {
            ThrowHelpers.ThrowIfIndexOutOfRange(index, Count);
            return Unsafe.Add(ref Unsafe.AsRef(in R), index);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            ThrowHelpers.ThrowIfIndexOutOfRange(index, Count);
            Unsafe.Add(ref R, index) = value;
        }
    }

    static RGBA128 IAdditiveIdentity<RGBA128, RGBA128>.AdditiveIdentity => Transparent;

    static RGBA128 IMultiplicativeIdentity<RGBA128, RGBA128>.MultiplicativeIdentity => White;

    /// <include file='RGBAX.xml' path='doc/members[@name="RGBAX"]/CtorRGBA/*'/>
    public RGBA128(float r, float g, float b, float a = 1.0f)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    /// <include file='RGBAX.xml' path='doc/members[@name="RGBAX"]/CtorGrayscale/*'/>
    public RGBA128(float rgb, float a = 1.0f)
    {
        R = G = B = rgb;
        A = a;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(RGBA128 other)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Vector128.EqualsAll(Vector, other.Vector);
        }

        return R == other.R && G == other.G && B == other.B && A == other.A;
    }

    /// <inheritdoc/>
    public override readonly bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is RGBA128 other && Equals(other);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly int GetHashCode()
    {
        return HashCode.Combine(R, G, B, A);
    }

    /// <inheritdoc/>
    public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? formatProvider)
    {
        string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
        DefaultInterpolatedStringHandler handler = new(literalLength: 5 + (separator.Length * 3),
            formattedCount: 4,
            formatProvider,
            stackalloc char[512]);
        handler.AppendLiteral("<");
        handler.AppendFormatted(R, format);
        handler.AppendLiteral(separator);
        handler.AppendLiteral(" ");
        handler.AppendFormatted(G, format);
        handler.AppendLiteral(separator);
        handler.AppendLiteral(" ");
        handler.AppendFormatted(B, format);
        handler.AppendLiteral(separator);
        handler.AppendLiteral(" ");
        handler.AppendFormatted(A, format);
        handler.AppendLiteral(">");
        return handler.ToStringAndClear();
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly string ToString()
    {
        return ToString("G", CultureInfo.CurrentCulture);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte FloatToByte(float r)
    {
        float scaled = MathF.FusedMultiplyAdd(r, byte.MaxValue, 0.5f);
        return byte.CreateSaturating(scaled);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(RGBA128 left, RGBA128 right)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return left.Vector == right.Vector;
        }

        return left.R == right.R && left.G == right.G && left.B == right.B && left.A == right.A;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(RGBA128 left, RGBA128 right)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return left.Vector != right.Vector;
        }

        return left.R != right.R || left.G != right.G || left.B != right.B || left.A != right.A;
    }

    /// <include file='RGBAX.xml' path='doc/members[@name="RGBAX"]/OpAdd/*'/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RGBA128 operator +(RGBA128 left, RGBA128 right)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector128<float>, RGBA128>(left.Vector + right.Vector);
        }

        return new RGBA128(left.R + right.R, left.G + right.G, left.B + right.B, left.A + right.A);
    }

    /// <include file='RGBAX.xml' path='doc/members[@name="RGBAX"]/OpSubtract/*'/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RGBA128 operator -(RGBA128 left, RGBA128 right)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector128<float>, RGBA128>(left.Vector - right.Vector);
        }

        return new RGBA128(left.R - right.R, left.G - right.G, left.B - right.B, left.A - right.A);
    }

    /// <include file='RGBAX.xml' path='doc/members[@name="RGBAX"]/OpMultiplyVectorVector/*'/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RGBA128 operator *(RGBA128 left, RGBA128 right)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector128<float>, RGBA128>(left.Vector * right.Vector);
        }

        return new RGBA128(left.R * right.R, left.G * right.G, left.B * right.B, left.A * right.A);
    }

    /// <include file='RGBAX.xml' path='doc/members[@name="RGBAX"]/OpMultiplyVectorScalar/*'/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RGBA128 operator *(RGBA128 left, float right)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector128<float>, RGBA128>(left.Vector * right);
        }

        return new RGBA128(left.R * right, left.G * right, left.B * right, left.A * right);
    }

    /// <include file='RGBAX.xml' path='doc/members[@name="RGBAX"]/OpMultiplyScalarVector/*'/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RGBA128 operator *(float left, RGBA128 right)
    {
        return right * left;
    }

    /// <include file='RGBAX.xml' path='doc/members[@name="RGBAX"]/OpDivideVectorVector/*'/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RGBA128 operator /(RGBA128 left, RGBA128 right)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector128<float>, RGBA128>(left.Vector / right.Vector);
        }

        return new RGBA128(left.R / right.R, left.G / right.G, left.B / right.B, left.A / right.A);
    }

    /// <include file='RGBAX.xml' path='doc/members[@name="RGBAX"]/OpDivideVectorScalar/*'/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RGBA128 operator /(RGBA128 left, float right)
    {
        return left * MathF.ReciprocalEstimate(right);
    }

    /// <include file='RGBAX.xml' path='doc/members[@name="RGBAX"]/OpDivideScalarVector/*'/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RGBA128 operator /(float left, RGBA128 right)
    {
        return right / left;
    }

    /// <summary>
    /// Defines an implicit conversion of an <see cref="RGBA128"/> value to a <see cref="Vector4"/> value.
    /// </summary>
    /// <param name="value">The <see cref="RGBA128"/> value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Vector4(RGBA128 value)
    {
        return Unsafe.BitCast<RGBA128, Vector4>(value);
    }

    /// <summary>
    /// Defines an implicit conversion of a <see cref="Vector4"/> value to an <see cref="RGBA128"/> value.
    /// </summary>
    /// <param name="value">The <see cref="Vector4"/> value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator RGBA128(Vector4 value)
    {
        return Unsafe.BitCast<Vector4, RGBA128>(value);
    }

    /// <summary>
    /// Defines an explicit conversion of an <see cref="RGBA128"/> value to an <see cref="RGBA32"/> value.
    /// </summary>
    /// <param name="value">The <see cref="RGBA128"/> value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator RGBA32(RGBA128 value)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<float> scaled = Vector128.ClampNative(value.Vector, Vector128<float>.Zero, Vector128<float>.One) * byte.MaxValue;
            Vector128<int> ints = Vector128.ConvertToInt32Native(scaled);
            Vector128<short> shorts = Vector128.NarrowWithSaturation(ints, ints);
            Vector128<byte> bytes = Vector128.NarrowWithSaturation(shorts.AsUInt16(), shorts.AsUInt16());
            return Unsafe.BitCast<uint, RGBA32>(bytes.AsUInt32().ToScalar());
        }

        return new RGBA32(FloatToByte(value.R), FloatToByte(value.G), FloatToByte(value.B), FloatToByte(value.A));
    }
}
