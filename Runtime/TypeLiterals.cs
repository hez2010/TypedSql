using System.Runtime.CompilerServices;

namespace TypedSql.Runtime;

internal interface IHex
{
    abstract static int Value { get; }
}

internal readonly struct Hex0 : IHex { public static int Value => 0; }
internal readonly struct Hex1 : IHex { public static int Value => 1; }
internal readonly struct Hex2 : IHex { public static int Value => 2; }
internal readonly struct Hex3 : IHex { public static int Value => 3; }
internal readonly struct Hex4 : IHex { public static int Value => 4; }
internal readonly struct Hex5 : IHex { public static int Value => 5; }
internal readonly struct Hex6 : IHex { public static int Value => 6; }
internal readonly struct Hex7 : IHex { public static int Value => 7; }
internal readonly struct Hex8 : IHex { public static int Value => 8; }
internal readonly struct Hex9 : IHex { public static int Value => 9; }
internal readonly struct HexA : IHex { public static int Value => 10; }
internal readonly struct HexB : IHex { public static int Value => 11; }
internal readonly struct HexC : IHex { public static int Value => 12; }
internal readonly struct HexD : IHex { public static int Value => 13; }
internal readonly struct HexE : IHex { public static int Value => 14; }
internal readonly struct HexF : IHex { public static int Value => 15; }

internal interface ILiteral<T>
{
    abstract static T Value { get; }
}

internal readonly struct Int<H7, H6, H5, H4, H3, H2, H1, H0> : ILiteral<int>
    where H7 : IHex
    where H6 : IHex
    where H5 : IHex
    where H4 : IHex
    where H3 : IHex
    where H2 : IHex
    where H1 : IHex
    where H0 : IHex
{
    public static int Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => H7.Value << 28
               | H6.Value << 24
               | H5.Value << 20
               | H4.Value << 16
               | H3.Value << 12
               | H2.Value << 8
               | H1.Value << 4
               | H0.Value;
    }
}

internal readonly struct Float<H7, H6, H5, H4, H3, H2, H1, H0> : ILiteral<float>
    where H7 : IHex
    where H6 : IHex
    where H5 : IHex
    where H4 : IHex
    where H3 : IHex
    where H2 : IHex
    where H1 : IHex
    where H0 : IHex
{
    public static float Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Unsafe.BitCast<int, float>(H7.Value << 28
               | H6.Value << 24
               | H5.Value << 20
               | H4.Value << 16
               | H3.Value << 12
               | H2.Value << 8
               | H1.Value << 4
               | H0.Value);
    }
}

internal readonly struct Char<H3, H2, H1, H0> : ILiteral<char>
    where H3 : IHex
    where H2 : IHex
    where H1 : IHex
    where H0 : IHex
{
    public static char Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (char)(H3.Value << 12
                      | H2.Value << 8
                      | H1.Value << 4
                      | H0.Value);
    }
}

internal interface IStringNode
{
    abstract static int Length { get; }

    abstract static void Write(Span<char> destination, int index);
}

internal readonly struct StringEnd : IStringNode
{
    public static int Length => 0;

    public static void Write(Span<char> destination, int index)
    {
    }
}

internal readonly struct StringNull : IStringNode
{
    public static int Length => -1;
    public static void Write(Span<char> destination, int index)
    {
    }
}

internal readonly struct StringNode<TChar, TNext> : IStringNode
    where TChar : ILiteral<char>
    where TNext : IStringNode
{
    public static int Length => 1 + TNext.Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(Span<char> destination, int index)
    {
        destination[index] = TChar.Value;
        TNext.Write(destination, index + 1);
    }
}

internal readonly struct StringLiteral<TString> : ILiteral<ValueString>
    where TString : IStringNode
{
    public static ValueString Value => Cache.Value;

    private static class Cache
    {
        public static readonly ValueString Value = Build();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ValueString Build()
        {
            var length = TString.Length;
            if (length < 0)
            {
                return new ValueString(null);
            }

            if (length == 0)
            {
                return new ValueString(string.Empty);
            }

            var chars = new char[length];
            TString.Write(chars.AsSpan(), 0);
            return new string(chars, 0, length);
        }
    }
}

internal readonly struct TrueLiteral : ILiteral<bool>
{
    public static bool Value => true;
}

internal readonly struct FalseLiteral : ILiteral<bool>
{
    public static bool Value => false;
}

internal static class LiteralTypeFactory
{
    private static readonly Type[] HexTypes =
    [
        typeof(Hex0), typeof(Hex1), typeof(Hex2), typeof(Hex3),
        typeof(Hex4), typeof(Hex5), typeof(Hex6), typeof(Hex7),
        typeof(Hex8), typeof(Hex9), typeof(HexA), typeof(HexB),
        typeof(HexC), typeof(HexD), typeof(HexE), typeof(HexF)
    ];

    public static Type CreateIntLiteral(int value)
    {
        var typeArgs = new Type[8];
        var unsigned = unchecked((uint)value);
        for (var i = 0; i < 8; i++)
        {
            var shift = (7 - i) * 4;
            var nibble = (int)((unsigned >>> shift) & 0xF);
            typeArgs[i] = HexTypes[nibble];
        }

        return typeof(Int<,,,,,,,>).MakeGenericType(typeArgs);
    }

    public static Type CreateStringLiteral(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var type = typeof(StringEnd);
        for (var i = value.Length - 1; i >= 0; i--)
        {
            var charType = CreateCharType(value[i]);
            type = typeof(StringNode<,>).MakeGenericType(charType, type);
        }

        return typeof(StringLiteral<>).MakeGenericType(type);
    }

    public static Type CreateFloatLiteral(float value)
    {
        var typeArgs = new Type[8];
        var unsigned = Unsafe.BitCast<float, uint>(value);
        for (var i = 0; i < 8; i++)
        {
            var shift = (7 - i) * 4;
            var nibble = (unsigned >>> shift) & 0xF;
            typeArgs[i] = HexTypes[nibble];
        }

        return typeof(Float<,,,,,,,>).MakeGenericType(typeArgs);
    }

    public static Type CreateBoolLiteral(bool value)
    {
        return value
            ? typeof(TrueLiteral)
            : typeof(FalseLiteral);
    }

    private static Type CreateCharType(char value)
    {
        var typeArgs = new Type[4];
        var unsigned = (ushort)value;
        for (var i = 0; i < 4; i++)
        {
            var shift = (3 - i) * 4;
            var nibble = (unsigned >>> shift) & 0xF;
            typeArgs[i] = HexTypes[nibble];
        }

        return typeof(Char<,,,>).MakeGenericType(typeArgs);
    }
}
