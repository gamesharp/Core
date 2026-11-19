using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace GameSharp.Mathematics;

[StructLayout(LayoutKind.Sequential, Size = 16)]
public struct Int3 : IEquatable<Int3>,
    IFormattable,
    IEqualityOperators<Int3, Int3, bool>,
    IAdditionOperators<Int3, Int3, Int3>,
    ISubtractionOperators<Int3, Int3, Int3>,
    IMultiplyOperators<Int3, Int3, Int3>,
    IUnaryNegationOperators<Int3, Int3>,
    IUnaryPlusOperators<Int3, Int3>,
    IAdditiveIdentity<Int3, Int3>,
    IMultiplicativeIdentity<Int3, Int3>,
    IBitwiseOperators<Int3, Int3, Int3>,
    IShiftOperators<Int3, int, Int3>
{
    private const int Count = 3;

    public static Int3 Zero
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(0);
    }

    public static Int3 One
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(1);
    }

    public int X;

    public int Y;

    public int Z;

    private readonly Vector128<int> Vector
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Unsafe.BitCast<Int3, Vector128<int>>(this);
    }

    public readonly long Sum
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (long)X + Y + Z;
    }

    public int this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get
        {
            ThrowHelpers.ThrowIfIndexOutOfRange(index, Count);
            return Unsafe.Add(ref Unsafe.AsRef(in X), index);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            ThrowHelpers.ThrowIfIndexOutOfRange(index, Count);
            Unsafe.Add(ref X, index) = value;
        }
    }

    static Int3 IAdditiveIdentity<Int3, Int3>.AdditiveIdentity => Zero;

    static Int3 IMultiplicativeIdentity<Int3, Int3>.MultiplicativeIdentity => One;

    public Int3(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public Int3(int value)
    {
        X = value;
        Y = value;
        Z = value;
    }

    public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? formatProvider)
    {
        string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
        DefaultInterpolatedStringHandler handler = new(literalLength: 4 + (separator.Length * 2), 
            formattedCount: 3,
            formatProvider,
            stackalloc char[64]);
        handler.AppendLiteral("<");
        handler.AppendFormatted(X, format);
        handler.AppendLiteral(separator);
        handler.AppendLiteral(" ");
        handler.AppendFormatted(Y, format);
        handler.AppendLiteral(separator);
        handler.AppendLiteral(" ");
        handler.AppendFormatted(Z, format);
        handler.AppendLiteral(">");
        return handler.ToStringAndClear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Int3 other)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return AllTrue(Vector128.Equals(Vector, other.Vector));
        }

        return X == other.X && Y == other.Y && Z == other.Z;
    }

    public override readonly bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is Int3 other && Equals(other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly string ToString()
    {
        return ToString("G", CultureInfo.CurrentCulture);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Distance(Int3 left, Int3 right)
    {
        if (Vector256.IsHardwareAccelerated)
        {
            Vector256<long> leftLo = Vector256.WidenLower(left.Vector.ToVector256Unsafe());
            Vector256<long> rightLo = Vector256.WidenLower(right.Vector.ToVector256Unsafe());
            Vector256<long> diff = Vector256.Abs(leftLo - rightLo);
            const long AllBitsSet64 = unchecked((long)0xFFFFFFFFFFFFFFFF);
            return Vector256.Sum(diff & Vector256.Create(AllBitsSet64, AllBitsSet64, AllBitsSet64, 0L));
        }

        if (Vector128.IsHardwareAccelerated)
        {
            const int AllBitsSet32 = unchecked((int)0xFFFFFFFF);
            Vector128<int> bitmask = Vector128.Create(AllBitsSet32, AllBitsSet32, AllBitsSet32, 0);
            (Vector128<long> leftLo, Vector128<long> leftHi) = Vector128.Widen(left.Vector & bitmask);
            (Vector128<long> rightLo, Vector128<long> rightHi) = Vector128.Widen(right.Vector & bitmask);
            return Vector128.Sum(Vector128.Abs(leftLo - rightLo) + Vector128.Abs(leftHi - rightHi));
        }

        return Math.Abs((long)left.X - right.X) + Math.Abs((long)left.Y - right.Y) + Math.Abs((long)left.Z - right.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int3 Abs(Int3 value)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector128<int>, Int3>(Vector128.Abs(value.Vector));
        }

        return new Int3(Math.Abs(value.X), Math.Abs(value.Y), Math.Abs(value.Z));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int3 Min(Int3 left, Int3 right)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector128<int>, Int3>(Vector128.Min(left.Vector, right.Vector));
        }

        return new Int3(Math.Min(left.X, right.X), Math.Min(left.Y, right.Y), Math.Min(left.Z, right.Z));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int3 Min(Int3 left, int right)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector128<int>, Int3>(Vector128.Min(left.Vector, Vector128.Create(right)));
        }

        return new Int3(Math.Min(left.X, right), Math.Min(left.Y, right), Math.Min(left.Z, right));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int3 Max(Int3 left, Int3 right)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector128<int>, Int3>(Vector128.Max(left.Vector, right.Vector));
        }

        return new Int3(Math.Max(left.X, right.X), Math.Max(left.Y, right.Y), Math.Max(left.Z, right.Z));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int3 Max(Int3 left, int right)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector128<int>, Int3>(Vector128.Max(left.Vector, Vector128.Create(right)));
        }

        return new Int3(Math.Max(left.X, right), Math.Max(left.Y, right), Math.Max(left.Z, right));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int3 Clamp(Int3 value, Int3 min, Int3 max)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector128<int>, Int3>(Vector128.Clamp(value.Vector, min.Vector, max.Vector));
        }

        return new Int3(Math.Clamp(value.X, min.X, max.X), Math.Clamp(value.Y, min.Y, max.Y), Math.Clamp(value.Z, min.Z, max.Z));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int3 Clamp(Int3 value, int min, int max)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector128<int>, Int3>(Vector128.Clamp(value.Vector, Vector128.Create(min), Vector128.Create(max)));
        }

        return new Int3(Math.Clamp(value.X, min, max), Math.Clamp(value.Y, min, max), Math.Clamp(value.Z, min, max));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GreaterThanAll(Int3 left, Int3 right)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return AllTrue(Vector128.GreaterThan(left.Vector, right.Vector));
        }

        return left.X > right.X && left.Y > right.Y && left.Z > right.Z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GreaterThanAll(Int3 left, int right)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return AllTrue(Vector128.GreaterThan(left.Vector, Vector128.Create(right)));
        }

        return left.X > right && left.Y > right && left.Z > right;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GreaterThanAny(Int3 left, Int3 right)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return AnyTrue(Vector128.GreaterThan(left.Vector, right.Vector));
        }

        return left.X > right.X || left.Y > right.Y || left.Z > right.Z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GreaterThanAny(Int3 left, int right)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return AnyTrue(Vector128.GreaterThan(left.Vector, Vector128.Create(right)));
        }

        return left.X > right || left.Y > right || left.Z > right;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GreaterThanOrEqualAll(Int3 left, Int3 right)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return AllTrue(Vector128.GreaterThanOrEqual(left.Vector, right.Vector));
        }

        return left.X >= right.X && left.Y >= right.Y && left.Z >= right.Z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GreaterThanOrEqualAll(Int3 left, int right)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return AllTrue(Vector128.GreaterThanOrEqual(left.Vector, Vector128.Create(right)));
        }

        return left.X >= right && left.Y >= right && left.Z >= right;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GreaterThanOrEqualAny(Int3 left, Int3 right)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return AnyTrue(Vector128.GreaterThanOrEqual(left.Vector, right.Vector));
        }

        return left.X >= right.X || left.Y >= right.Y || left.Z >= right.Z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GreaterThanOrEqualAny(Int3 left, int right)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return AnyTrue(Vector128.GreaterThanOrEqual(left.Vector, Vector128.Create(right)));
        }

        return left.X >= right || left.Y >= right || left.Z >= right;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool LessThanAll(Int3 left, Int3 right)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return AllTrue(Vector128.LessThan(left.Vector, right.Vector));
        }

        return left.X < right.X && left.Y < right.Y && left.Z < right.Z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool LessThanAll(Int3 left, int right)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return AllTrue(Vector128.LessThan(left.Vector, Vector128.Create(right)));
        }

        return left.X < right && left.Y < right && left.Z < right;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool LessThanAny(Int3 left, Int3 right)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return AnyTrue(Vector128.LessThan(left.Vector, right.Vector));
        }

        return left.X < right.X || left.Y < right.Y || left.Z < right.Z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool LessThanAny(Int3 left, int right)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return AnyTrue(Vector128.LessThan(left.Vector, Vector128.Create(right)));
        }

        return left.X < right || left.Y < right || left.Z < right;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool LessThanOrEqualAll(Int3 left, Int3 right)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return AllTrue(Vector128.LessThanOrEqual(left.Vector, right.Vector));
        }

        return left.X <= right.X && left.Y <= right.Y && left.Z <= right.Z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool LessThanOrEqualAll(Int3 left, int right)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return AllTrue(Vector128.LessThanOrEqual(left.Vector, Vector128.Create(right)));
        }

        return left.X <= right && left.Y <= right && left.Z <= right;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool LessThanOrEqualAny(Int3 left, Int3 right)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return AnyTrue(Vector128.LessThanOrEqual(left.Vector, right.Vector));
        }

        return left.X <= right.X || left.Y <= right.Y || left.Z <= right.Z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool LessThanOrEqualAny(Int3 left, int right)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return AnyTrue(Vector128.LessThanOrEqual(left.Vector, Vector128.Create(right)));
        }

        return left.X <= right || left.Y <= right || left.Z <= right;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool AllTrue(Vector128<int> comparison)
    {
        return (comparison.ExtractMostSignificantBits() & 0b111) == 0b111;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool AnyTrue(Vector128<int> comparison)
    {
        return (comparison.ExtractMostSignificantBits() & 0b111) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Int3 left, Int3 right)
    {
        return left.Equals(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Int3 left, Int3 right)
    {
        return !left.Equals(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int3 operator +(Int3 left, Int3 right)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector128<int>, Int3>(left.Vector + right.Vector);
        }

        return new Int3(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int3 operator +(Int3 left, int right)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector128<int>, Int3>(left.Vector + Vector128.Create(right));
        }

        return new Int3(left.X + right, left.Y + right, left.Z + right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int3 operator +(int left, Int3 right)
    {
        return right + left;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int3 operator -(Int3 left, Int3 right)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector128<int>, Int3>(left.Vector - right.Vector);
        }

        return new Int3(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int3 operator -(Int3 left, int right)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector128<int>, Int3>(left.Vector - Vector128.Create(right));
        }

        return new Int3(left.X - right, left.Y - right, left.Z - right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int3 operator -(int left, Int3 right)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector128<int>, Int3>(Vector128.Create(left) - right.Vector);
        }

        return new Int3(left - right.X, left - right.Y, left - right.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int3 operator *(Int3 left, Int3 right)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector128<int>, Int3>(left.Vector * right.Vector);
        }

        return new Int3(left.X * right.X, left.Y * right.Y, left.Z * right.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int3 operator *(Int3 left, int right)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector128<int>, Int3>(left.Vector * Vector128.Create(right));
        }

        return new Int3(left.X * right, left.Y * right, left.Z * right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int3 operator *(int left, Int3 right)
    {
        return right * left;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int3 operator -(Int3 value)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector128<int>, Int3>(-value.Vector);
        }

        return new Int3(-value.X, -value.Y, -value.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int3 operator +(Int3 value)
    {
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int3 operator &(Int3 left, Int3 right)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector128<int>, Int3>(left.Vector & right.Vector);
        }

        return new Int3(left.X & right.X, left.Y & right.Y, left.Z & right.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int3 operator |(Int3 left, Int3 right)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector128<int>, Int3>(left.Vector | right.Vector);
        }

        return new Int3(left.X | right.X, left.Y | right.Y, left.Z | right.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int3 operator ^(Int3 left, Int3 right)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector128<int>, Int3>(left.Vector ^ right.Vector);
        }

        return new Int3(left.X ^ right.X, left.Y ^ right.Y, left.Z ^ right.Z);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int3 operator ~(Int3 value)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector128<int>, Int3>(~value.Vector);
        }

        return new Int3(~value.X, ~value.Y, ~value.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int3 operator <<(Int3 value, int shiftAmount)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector128<int>, Int3>(value.Vector << shiftAmount);
        }

        return new Int3(value.X << shiftAmount, value.Y << shiftAmount, value.Z << shiftAmount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int3 operator >>(Int3 value, int shiftAmount)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector128<int>, Int3>(value.Vector >> shiftAmount);
        }

        return new Int3(value.X >> shiftAmount, value.Y >> shiftAmount, value.Z >> shiftAmount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int3 operator >>>(Int3 value, int shiftAmount)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector128<int>, Int3>(value.Vector >>> shiftAmount);
        }

        return new Int3(value.X >>> shiftAmount, value.Y >>> shiftAmount, value.Z >>> shiftAmount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Vector3(Int3 value)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<float> floats = Vector128.ConvertToSingle(value.Vector);
            return floats.AsVector3();
        }

        return new Vector3(value.X, value.Y, value.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Int3(Vector3 value)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<int> ints = Vector128.ConvertToInt32(value.AsVector128());
            return Unsafe.BitCast<Vector128<int>, Int3>(ints);
        }

        return new Int3((int)value.X, (int)value.Y, (int)value.Z);
    }
}
