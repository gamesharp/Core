using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace GameSharp.Mathematics;

/// <summary>
/// Represents a two-dimensional vector with signed 32-bit integer components.
/// </summary>
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

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/Zero/*' />
    public static Int2 Zero
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(0);
    }

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/One/*' />
    public static Int2 One
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(1);
    }

    /// <summary>
    /// The X component of the vector.
    /// </summary>
    public int X;

    /// <summary>
    /// The Y component of the vector.
    /// </summary>
    public int Y;

    private readonly Vector64<int> Vector
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Unsafe.BitCast<Int2, Vector64<int>>(this);
    }

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/Sum/*' />
    public readonly int Sum
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => X + Y;
    }

    static Int2 IAdditiveIdentity<Int2, Int2>.AdditiveIdentity => Zero;

    static Int2 IMultiplicativeIdentity<Int2, Int2>.MultiplicativeIdentity => One;

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/Indexer/*'/>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="Int2"/> struct with the specified components.
    /// </summary>
    /// <param name="x">The X component of the vector.</param>
    /// <param name="y">The Y component of the vector.</param>
    public Int2(int x, int y)
    {
        X = x;
        Y = y;
    }

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/CtorUniform/*' />
    public Int2(int value)
    {
        X = value;
        Y = value;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Int2 other)
    {
        if (Vector64.IsHardwareAccelerated)
        {
            return Vector64.EqualsAll(Vector, other.Vector);
        }

        return Unsafe.BitCast<Int2, ulong>(this) == Unsafe.BitCast<Int2, ulong>(other);
    }

    /// <inheritdoc/>
    public override readonly bool Equals(object? obj)
    {
        return obj is Int2 other && Equals(other);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly int GetHashCode()
    {
        ulong value = Unsafe.BitCast<Int2, ulong>(this);
        return value.GetHashCode();
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly string ToString()
    {
        return ToString("G", CultureInfo.CurrentCulture);
    }

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/Abs/*'/>
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

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/MinVector/*'/>
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

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/MinScalar/*'/>
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

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/MaxVector/*'/>
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

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/MaxScalar/*'/>
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

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/ClampVector/*'/>
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

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/ClampScalar/*'/>
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

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/GreaterThanAllVector/*'/>
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

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/GreaterThanAllScalar/*'/>
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

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/GreaterThanAnyVector/*'/>
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

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/GreaterThanAnyScalar/*'/>
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

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/GreaterThanOrEqualAllVector/*'/>
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

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/GreaterThanOrEqualAllScalar/*'/>
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

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/GreaterThanOrEqualAnyVector/*'/>
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

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/GreaterThanOrEqualAnyScalar/*'/>
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

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/LessThanAllVector/*'/>
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

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/LessThanAllScalar/*'/>
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

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/LessThanAnyVector/*'/>
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

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/LessThanAnyScalar/*'/>
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

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/LessThanOrEqualAllVector/*'/>
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

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/LessThanOrEqualAllScalar/*'/>
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

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/LessThanOrEqualAnyVectorVector/*'/>
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

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/LessThanOrEqualAnyVectorScalar/*'/>
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

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Int2 left, Int2 right)
    {
        return left.Equals(right);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Int2 left, Int2 right)
    {
        return !left.Equals(right);
    }

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/OpAddVectorVector/*'/>
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

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/OpAddVectorScalar/*'/>
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

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/OpAddScalarVector/*'/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 operator +(int left, Int2 right)
    {
        return right + left;
    }
    
    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/OpSubtractVectorVector/*'/>
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

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/OpSubtractVectorScalar/*'/>
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

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/OpSubtractScalarVector/*'/>
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

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/OpMultiplyVectorVector/*'/>
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

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/OpMultiplyVectorScalar/*'/>
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

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/OpMultiplyScalarVector/*'/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 operator *(int left, Int2 right)
    {
        return right * left;
    }

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/OpUnaryNegation/*'/>
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
    
    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/OpUnaryPlus/*'/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 operator +(Int2 value)
    {
        return value;
    }
    
    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/OpBitwiseAndVectorVector/*'/>
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
    
    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/OpBitwiseOrVectorVector/*'/>
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

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/OpExclusiveOrVectorVector/*'/>
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

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/OpOnesComplement/*'/>
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

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/OpShiftLeft/*'/>
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

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/OpShiftRight/*'/>
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

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/OpShiftRightUnsigned/*'/>
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

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/OpImplicitToFloatVector/*'/>
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

    /// <include file='IntX.xml' path='doc/members[@name="IntX"]/OpExplicitFromFloatVector/*'/>
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
