using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace ToolTracking;

/// <summary>Level 4 (bonus) - decodes GS1 SGTIN-96 EPCs.</summary>
public static class Sgtin96Decoder
{
    private const ulong SgtinHeader = 0x30;

    // GS1 EPC Tag Data Standard, SGTIN partition table (index = partition value).
    private static readonly (int CompanyBits, int CompanyDigits, int ItemBits, int ItemDigits)[] Partitions =
    {
        (40, 12, 4, 1),
        (37, 11, 7, 2),
        (34, 10, 10, 3),
        (30, 9, 14, 4),
        (27, 8, 17, 5),
        (24, 7, 20, 6),
        (20, 6, 24, 7),
    };

    public static bool TryDecode(string epcHex, [NotNullWhen(true)] out Sgtin96? result)
    {
        result = null;

        if (string.IsNullOrEmpty(epcHex) || epcHex.Length != 24 || !epcHex.All(char.IsAsciiHexDigit))
        {
            return false;
        }

        var value = UInt128.Parse(epcHex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);

        // Reads `count` bits starting at bit `offset`, counted from the most significant bit.
        ulong Bits(int offset, int count) =>
            (ulong)((value >> (96 - offset - count)) & ((UInt128.One << count) - 1));

        if (Bits(0, 8) != SgtinHeader)
        {
            return false;
        }

        var filter = (int)Bits(8, 3);
        var partition = (int)Bits(11, 3);
        if (partition >= Partitions.Length)
        {
            return false;
        }

        var p = Partitions[partition];
        var companyPrefix = Bits(14, p.CompanyBits);
        var itemReference = Bits(14 + p.CompanyBits, p.ItemBits);
        var serial = Bits(58, 38);

        // The bit fields are wider than the decimal digits they carry, so not every value is legal.
        if (companyPrefix >= Pow10(p.CompanyDigits) || itemReference >= Pow10(p.ItemDigits))
        {
            return false;
        }

        result = new Sgtin96(
            filter,
            companyPrefix.ToString("D" + p.CompanyDigits, CultureInfo.InvariantCulture),
            itemReference.ToString("D" + p.ItemDigits, CultureInfo.InvariantCulture),
            (long)serial);
        return true;
    }

    private static ulong Pow10(int exponent)
    {
        ulong result = 1;
        for (var i = 0; i < exponent; i++)
        {
            result *= 10;
        }

        return result;
    }
}
