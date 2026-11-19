using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace GameSharp.Mathematics;

[StructLayout(LayoutKind.Sequential, Size = 4)]
public struct RGBA32 : IEquatable<RGBA32>,
    IEqualityOperators<RGBA32, RGBA32, bool>,
    IAdditionOperators<RGBA32, RGBA32, RGBA128>,
    ISubtractionOperators<RGBA32, RGBA32, RGBA128>,
    IMultiplyOperators<RGBA32, RGBA32, RGBA128>,
    IDivisionOperators<RGBA32, RGBA32, RGBA128>,
    IAdditiveIdentity<RGBA32, RGBA32>,
    IMultiplicativeIdentity<RGBA32, RGBA32>
{
    private const int Count = 4;
    private const float ByteToFloat = 1.0f / 255.0f;

    public static RGBA32 Transparent => new(r: 0, g: 0, b: 0, a: 0);

    public static RGBA32 White => new(r: 255, g: 255, b: 255, a: 255);

    private readonly uint Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Unsafe.BitCast<RGBA32, uint>(this);
    }

    public byte R;

    public byte G;

    public byte B;

    public byte A;

    static RGBA32 IAdditiveIdentity<RGBA32, RGBA32>.AdditiveIdentity => Transparent;

    static RGBA32 IMultiplicativeIdentity<RGBA32, RGBA32>.MultiplicativeIdentity => White;

    public byte this[int index]
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

    public RGBA32(byte r, byte g, byte b, byte a = byte.MaxValue)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public RGBA32(byte rgb, byte a = byte.MaxValue)
    {
        R = G = B = rgb;
        A = a;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(RGBA32 other)
    {
        return Value == other.Value;
    }

    public override readonly bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is RGBA32 other && Equals(other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly int GetHashCode()
    {
        return Value.GetHashCode();
    }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly string ToString()
    {
        return ToString("G", CultureInfo.CurrentCulture);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(RGBA32 left, RGBA32 right)
    {
        return left.Equals(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(RGBA32 left, RGBA32 right)
    {
        return !left.Equals(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RGBA128 operator +(RGBA32 left, RGBA32 right)
    {
        return (RGBA128)left + (RGBA128)right;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RGBA128 operator -(RGBA32 left, RGBA32 right)
    {
        return (RGBA128)left - (RGBA128)right;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RGBA128 operator *(RGBA32 left, RGBA32 right)
    {
        return (RGBA128)left * (RGBA128)right;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RGBA128 operator /(RGBA32 left, RGBA32 right)
    {
        return (RGBA128)left / (RGBA128)right;
    }

    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator RGBA128(RGBA32 value)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<float> floats = Vector128.ConvertToSingle(
                Vector128.WidenLower(
                    Vector128.WidenLower(
                        Vector128.CreateScalarUnsafe(value.Value).AsByte())));
            return Unsafe.BitCast<Vector128<float>, RGBA128>(floats * ByteToFloat);
        }

        return new RGBA128(value.R * ByteToFloat, value.G * ByteToFloat, value.B * ByteToFloat, value.A * ByteToFloat);
    }
}
