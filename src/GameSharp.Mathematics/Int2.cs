using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace GameSharp.Mathematics;

[StructLayout(LayoutKind.Sequential, Size = 8)]
public struct Int2 : IEquatable<Int2>, 
    IFormattable, 
    IEqualityOperators<Int2, Int2, bool>,
    IAdditionOperators<Int2, Int2, Int2>,
    ISubtractionOperators<Int2, Int2, Int2>,
    IMultiplyOperators<Int2, Int2, Int2>,
    IUnaryNegationOperators<Int2, Int2>,
    IUnaryPlusOperators<Int2, Int2>,
    IAdditiveIdentity<Int2, Int2>,
    IMultiplicativeIdentity<Int2, Int2>,
    IBitwiseOperators<Int2, Int2, Int2>,
    IShiftOperators<Int2, int, Int2>
{
    private const int Count = 2;

    public static Int2 Zero
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(0);
    }

    public static Int2 One
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(1);
    }

    public int X;

    public int Y;

    private readonly Vector64<int> Vector
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Unsafe.BitCast<Int2, Vector64<int>>(this);
    }

    public readonly int Sum
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => X + Y;
    }

    static Int2 IAdditiveIdentity<Int2, Int2>.AdditiveIdentity => Zero;

    static Int2 IMultiplicativeIdentity<Int2, Int2>.MultiplicativeIdentity => One;

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

    public Int2(int x, int y)
    {
        X = x;
        Y = y;
    }

    public Int2(int value)
    {
        X = value;
        Y = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Int2 other)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Vector64.EqualsAll(Vector, other.Vector);
        }

        return Unsafe.BitCast<Int2, ulong>(this) == Unsafe.BitCast<Int2, ulong>(other);
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is Int2 other && Equals(other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly int GetHashCode()
    {
        ulong value = Unsafe.BitCast<Int2, ulong>(this);
        return value.GetHashCode();
    }

    public readonly string ToString(string? format, IFormatProvider? formatProvider)
    {
        string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
        DefaultInterpolatedStringHandler handler = new(literalLength: 3 + separator.Length, 
            formattedCount: 2, 
            formatProvider, 
            stackalloc char[64]);
        handler.AppendLiteral("<");
        handler.AppendFormatted(X, format);
        handler.AppendLiteral(separator);
        handler.AppendLiteral(" ");
        handler.AppendFormatted(Y, format);
        handler.AppendLiteral(">");
        return handler.ToStringAndClear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly string ToString()
    {
        return ToString("G", CultureInfo.CurrentCulture);
    }

    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 Abs(Int2 value)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector64<int>, Int2>(Vector64.Abs(value.Vector));
        }

        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<int> result = Vector128.Abs(value.Vector.ToVector128Unsafe());
            return Unsafe.BitCast<Vector64<int>, Int2>(result.GetLower());
        }

        return new Int2(Math.Abs(value.X), Math.Abs(value.Y));
    }

    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 Min(Int2 left, Int2 right)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector64<int>, Int2>(Vector64.Min(left.Vector, right.Vector));
        }

        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<int> result = Vector128.Min(left.Vector.ToVector128Unsafe(), right.Vector.ToVector128Unsafe());
            return Unsafe.BitCast<Vector64<int>, Int2>(result.GetLower());
        }

        return new Int2(Math.Min(left.X, right.X), Math.Min(left.Y, right.Y));
    }

    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 Min(Int2 left, int right)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector64<int>, Int2>(Vector64.Min(left.Vector, Vector64.Create(right)));
        }

        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<int> result = Vector128.Min(left.Vector.ToVector128Unsafe(), Vector128.Create(right));
            return Unsafe.BitCast<Vector64<int>, Int2>(result.GetLower());
        }

        return new Int2(Math.Min(left.X, right), Math.Min(left.Y, right));
    }

    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 Max(Int2 left, Int2 right)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector64<int>, Int2>(Vector64.Max(left.Vector, right.Vector));
        }

        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<int> result = Vector128.Max(left.Vector.ToVector128Unsafe(), right.Vector.ToVector128Unsafe());
            return Unsafe.BitCast<Vector64<int>, Int2>(result.GetLower());
        }

        return new Int2(Math.Max(left.X, right.X), Math.Max(left.Y, right.Y));
    }

    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 Max(Int2 left, int right)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector64<int>, Int2>(Vector64.Max(left.Vector, Vector64.Create(right)));
        }

        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<int> result = Vector128.Max(left.Vector.ToVector128Unsafe(), Vector128.Create(right));
            return Unsafe.BitCast<Vector64<int>, Int2>(result.GetLower());
        }

        return new Int2(Math.Max(left.X, right), Math.Max(left.Y, right));
    }

    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 Clamp(Int2 value, Int2 min, Int2 max)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector64<int>, Int2>(Vector64.Clamp(value.Vector, min.Vector, max.Vector));
        }

        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<int> result = Vector128.Clamp(value.Vector.ToVector128Unsafe(),
                min.Vector.ToVector128Unsafe(),
                max.Vector.ToVector128Unsafe());
            return Unsafe.BitCast<Vector64<int>, Int2>(result.GetLower());
        }

        return new Int2(Math.Clamp(value.X, min.X, max.X), Math.Clamp(value.Y, min.Y, max.Y));
    }

    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 Clamp(Int2 value, int min, int max)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector64<int>, Int2>(Vector64.Clamp(value.Vector, Vector64.Create(min), Vector64.Create(max)));
        }

        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<int> result = Vector128.Clamp(value.Vector.ToVector128Unsafe(),
                Vector128.Create(min),
                Vector128.Create(max));
            return Unsafe.BitCast<Vector64<int>, Int2>(result.GetLower());
        }

        return new Int2(Math.Clamp(value.X, min, max), Math.Clamp(value.Y, min, max));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GreaterThanAll(Int2 left, Int2 right)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Vector64.GreaterThanAll(left.Vector, right.Vector);
        }

        if (Vector128.IsHardwareAccelerated)
        {
            return Vector128.GreaterThanAll(Vector128.Create(left.Vector), Vector128.Create(right.Vector));
        }

        return left.X > right.X && left.Y > right.Y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GreaterThanAll(Int2 left, int right)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Vector64.GreaterThanAll(left.Vector, Vector64.Create(right));
        }

        if (Vector128.IsHardwareAccelerated)
        {
            return Vector128.GreaterThanAll(Vector128.Create(left.Vector), Vector128.Create(right));
        }

        return left.X > right && left.Y > right;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GreaterThanAny(Int2 left, Int2 right)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Vector64.GreaterThanAny(left.Vector, right.Vector);
        }

        if (Vector128.IsHardwareAccelerated)
        {
            return Vector128.GreaterThanAny(Vector128.Create(left.Vector), Vector128.Create(right.Vector));
        }

        return left.X > right.X || left.Y > right.Y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GreaterThanAny(Int2 left, int right)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Vector64.GreaterThanAny(left.Vector, Vector64.Create(right));
        }

        if (Vector128.IsHardwareAccelerated)
        {
            return Vector128.GreaterThanAny(Vector128.Create(left.Vector), Vector128.Create(right));
        }

        return left.X > right || left.Y > right;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GreaterThanOrEqualAll(Int2 left, Int2 right)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Vector64.GreaterThanOrEqualAll(left.Vector, right.Vector);
        }

        if (Vector128.IsHardwareAccelerated)
        {
            return Vector128.GreaterThanOrEqualAll(Vector128.Create(left.Vector), Vector128.Create(right.Vector));
        }

        return left.X >= right.X && left.Y >= right.Y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GreaterThanOrEqualAll(Int2 left, int right)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Vector64.GreaterThanOrEqualAll(left.Vector, Vector64.Create(right));
        }

        if (Vector128.IsHardwareAccelerated)
        {
            return Vector128.GreaterThanOrEqualAll(Vector128.Create(left.Vector), Vector128.Create(right));
        }

        return left.X >= right && left.Y >= right;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GreaterThanOrEqualAny(Int2 left, Int2 right)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Vector64.GreaterThanOrEqualAny(left.Vector, right.Vector);
        }

        if (Vector128.IsHardwareAccelerated)
        {
            return Vector128.GreaterThanOrEqualAny(Vector128.Create(left.Vector), Vector128.Create(right.Vector));
        }

        return left.X >= right.X || left.Y >= right.Y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GreaterThanOrEqualAny(Int2 left, int right)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Vector64.GreaterThanOrEqualAny(left.Vector, Vector64.Create(right));
        }

        if (Vector128.IsHardwareAccelerated)
        {
            return Vector128.GreaterThanOrEqualAny(Vector128.Create(left.Vector), Vector128.Create(right));
        }

        return left.X >= right || left.Y >= right;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool LessThanAll(Int2 left, Int2 right)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Vector64.LessThanAll(left.Vector, right.Vector);
        }

        if (Vector128.IsHardwareAccelerated)
        {
            return Vector128.LessThanAll(Vector128.Create(left.Vector), Vector128.Create(right.Vector));
        }

        return left.X < right.X && left.Y < right.Y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool LessThanAll(Int2 left, int right)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Vector64.LessThanAll(left.Vector, Vector64.Create(right));
        }

        if (Vector128.IsHardwareAccelerated)
        {
            return Vector128.LessThanAll(Vector128.Create(left.Vector), Vector128.Create(right));
        }

        return left.X < right && left.Y < right;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool LessThanAny(Int2 left, Int2 right)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Vector64.LessThanAny(left.Vector, right.Vector);
        }

        if (Vector128.IsHardwareAccelerated)
        {
            return Vector128.LessThanAny(Vector128.Create(left.Vector), Vector128.Create(right.Vector));
        }

        return left.X < right.X || left.Y < right.Y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool LessThanAny(Int2 left, int right)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Vector64.LessThanAny(left.Vector, Vector64.Create(right));
        }

        if (Vector128.IsHardwareAccelerated)
        {
            return Vector128.LessThanAny(Vector128.Create(left.Vector), Vector128.Create(right));
        }

        return left.X < right || left.Y < right;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool LessThanOrEqualAll(Int2 left, Int2 right)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Vector64.LessThanOrEqualAll(left.Vector, right.Vector);
        }

        if (Vector128.IsHardwareAccelerated)
        {
            return Vector128.LessThanOrEqualAll(Vector128.Create(left.Vector), Vector128.Create(right.Vector));
        }

        return left.X <= right.X && left.Y <= right.Y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool LessThanOrEqualAll(Int2 left, int right)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Vector64.LessThanOrEqualAll(left.Vector, Vector64.Create(right));
        }

        if (Vector128.IsHardwareAccelerated)
        {
            return Vector128.LessThanOrEqualAll(Vector128.Create(left.Vector), Vector128.Create(right));
        }

        return left.X <= right && left.Y <= right;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool LessThanOrEqualAny(Int2 left, Int2 right)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Vector64.LessThanOrEqualAny(left.Vector, right.Vector);
        }

        if (Vector128.IsHardwareAccelerated)
        {
            return Vector128.LessThanOrEqualAll(Vector128.Create(left.Vector), Vector128.Create(right.Vector));
        }

        return left.X <= right.X || left.Y <= right.Y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool LessThanOrEqualAny(Int2 left, int right)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Vector64.LessThanOrEqualAny(left.Vector, Vector64.Create(right));
        }

        if (Vector128.IsHardwareAccelerated)
        {
            return Vector128.LessThanOrEqualAny(Vector128.Create(left.Vector), Vector128.Create(right));
        }

        return left.X <= right || left.Y <= right;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Int2 left, Int2 right)
    {
        return left.Equals(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Int2 left, Int2 right)
    {
        return !left.Equals(right);
    }

    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 operator +(Int2 left, Int2 right)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector64<int>, Int2>(left.Vector + right.Vector);
        }

        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<int> result = left.Vector.ToVector128Unsafe() + right.Vector.ToVector128Unsafe();
            return Unsafe.BitCast<Vector64<int>, Int2>(result.GetLower());
        }

        return new Int2(left.X + right.X, left.Y + right.Y);
    }

    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 operator +(Int2 left, int right)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector64<int>, Int2>(left.Vector + Vector64.Create(right));
        }

        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<int> result = left.Vector.ToVector128Unsafe() + Vector128.Create(right);
            return Unsafe.BitCast<Vector64<int>, Int2>(result.GetLower());
        }

        return new Int2(left.X + right, left.Y + right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 operator +(int left, Int2 right)
    {
        return right + left;
    }
    
    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 operator -(Int2 left, Int2 right)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector64<int>, Int2>(left.Vector - right.Vector);
        }

        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<int> result = left.Vector.ToVector128Unsafe() - right.Vector.ToVector128Unsafe();
            return Unsafe.BitCast<Vector64<int>, Int2>(result.GetLower());
        }

        return new Int2(left.X - right.X, left.Y - right.Y);
    }

    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 operator -(Int2 left, int right)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector64<int>, Int2>(left.Vector - Vector64.Create(right));
        }

        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<int> result = left.Vector.ToVector128Unsafe() - Vector128.Create(right);
            return Unsafe.BitCast<Vector64<int>, Int2>(result.GetLower());
        }

        return new Int2(left.X - right, left.Y - right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 operator -(int left, Int2 right)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector64<int>, Int2>(Vector64.Create(left) - right.Vector);
        }

        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<int> result = Vector128.Create(left) - right.Vector.ToVector128Unsafe();
            return Unsafe.BitCast<Vector64<int>, Int2>(result.GetLower());
        }

        return new Int2(left - right.X, left - right.Y);
    }

    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 operator *(Int2 left, Int2 right)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector64<int>, Int2>(left.Vector * right.Vector);
        }

        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<int> result = left.Vector.ToVector128Unsafe() * right.Vector.ToVector128Unsafe();
            return Unsafe.BitCast<Vector64<int>, Int2>(result.GetLower());
        }

        return new Int2(left.X * right.X, left.Y * right.Y);
    }

    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 operator *(Int2 left, int right)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector64<int>, Int2>(left.Vector * Vector64.Create(right));
        }

        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<int> result = left.Vector.ToVector128Unsafe() * Vector128.Create(right);
            return Unsafe.BitCast<Vector64<int>, Int2>(result.GetLower());
        }

        return new Int2(left.X * right, left.Y * right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 operator *(int left, Int2 right)
    {
        return right * left;
    }

    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 operator -(Int2 value)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector64<int>, Int2>(-value.Vector);
        }

        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<int> result = -value.Vector.ToVector128Unsafe();
            return Unsafe.BitCast<Vector64<int>, Int2>(result.GetLower());
        }

        return new Int2(-value.X, -value.Y);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 operator +(Int2 value)
    {
        return value;
    }
    
    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 operator &(Int2 left, Int2 right)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector64<int>, Int2>(left.Vector & right.Vector);
        }

        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<int> result = left.Vector.ToVector128Unsafe() & right.Vector.ToVector128Unsafe();
            return Unsafe.BitCast<Vector64<int>, Int2>(result.GetLower());
        }

        return new Int2(left.X & right.X, left.Y & right.Y);
    }
    
    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 operator |(Int2 left, Int2 right)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector64<int>, Int2>(left.Vector | right.Vector);
        }

        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<int> result = left.Vector.ToVector128Unsafe() | right.Vector.ToVector128Unsafe();
            return Unsafe.BitCast<Vector64<int>, Int2>(result.GetLower());
        }

        return new Int2(left.X | right.X, left.Y | right.Y);
    }

    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 operator ^(Int2 left, Int2 right)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector64<int>, Int2>(left.Vector ^ right.Vector);
        }

        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<int> result = left.Vector.ToVector128Unsafe() ^ right.Vector.ToVector128Unsafe();
            return Unsafe.BitCast<Vector64<int>, Int2>(result.GetLower());
        }

        return new Int2(left.X ^ right.X, left.Y ^ right.Y);
    }

    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 operator ~(Int2 value)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector64<int>, Int2>(~value.Vector);
        }

        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<int> result = ~value.Vector.ToVector128Unsafe();
            return Unsafe.BitCast<Vector64<int>, Int2>(result.GetLower());
        }

        return new Int2(~value.X, ~value.Y);
    }

    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 operator <<(Int2 value, int shiftAmount)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector64<int>, Int2>(value.Vector << shiftAmount);
        }

        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<int> result = value.Vector.ToVector128Unsafe() << shiftAmount;
            return Unsafe.BitCast<Vector64<int>, Int2>(result.GetLower());
        }

        return new Int2(value.X << shiftAmount, value.Y << shiftAmount);
    }

    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 operator >>(Int2 value, int shiftAmount)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector64<int>, Int2>(value.Vector >> shiftAmount);
        }

        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<int> result = value.Vector.ToVector128Unsafe() >> shiftAmount;
            return Unsafe.BitCast<Vector64<int>, Int2>(result.GetLower());
        }

        return new Int2(value.X >> shiftAmount, value.Y >> shiftAmount);
    }

    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 operator >>>(Int2 value, int shiftAmount)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Unsafe.BitCast<Vector64<int>, Int2>(value.Vector >>> shiftAmount);
        }

        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<int> result = value.Vector.ToVector128Unsafe() >>> shiftAmount;
            return Unsafe.BitCast<Vector64<int>, Int2>(result.AsInt32().GetLower());
        }

        return new Int2((int)((uint)value.X >> shiftAmount), (int)((uint)value.Y >> shiftAmount));
    }

    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Vector2(Int2 value)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<float> result = Vector128.ConvertToSingle(value.Vector.ToVector128Unsafe());
            return Unsafe.BitCast<Vector64<float>, Vector2>(result.GetLower());
        }

        return new Vector2(value.X, value.Y);
    }

    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Int2(Vector2 value)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<int> result = Vector128.ConvertToInt32(value.AsVector128Unsafe());
            return Unsafe.BitCast<Vector64<int>, Int2>(result.GetLower());
        }

        return new Int2((int)value.X, (int)value.Y);
    }
}
