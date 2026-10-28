using System.Diagnostics.CodeAnalysis;

namespace ToolTracking;

/// <summary>Level 4 (bonus) - decodes GS1 SGTIN-96 EPCs.</summary>
public static class Sgtin96Decoder
{
    /// <summary>
    /// Returns true and the decoded value if <paramref name="epcHex"/> is a valid SGTIN-96 EPC.
    /// Returns false (never throws) for anything else. See README, Level 4.
    /// </summary>
    public static bool TryDecode(string epcHex, [NotNullWhen(true)] out Sgtin96? result)
    {
        // TODO: implement Level 4 (bonus)
        throw new NotImplementedException();
    }
}
