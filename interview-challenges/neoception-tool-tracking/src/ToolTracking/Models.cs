namespace ToolTracking;

/// <summary>A single, validated raw read reported by an RFID reader antenna.</summary>
/// <param name="Timestamp">Moment of the read, normalized to UTC.</param>
/// <param name="ReaderId">Identifier of the reader that produced the read.</param>
/// <param name="Antenna">Antenna port on the reader (1-based).</param>
/// <param name="Epc">The tag's EPC as 24 upper-case hex characters.</param>
/// <param name="Rssi">Received signal strength in dBm (closer to 0 = stronger).</param>
/// <param name="Zone">Zone the reader antenna is installed in.</param>
public sealed record TagRead(
    DateTimeOffset Timestamp,
    string ReaderId,
    int Antenna,
    string Epc,
    double Rssi,
    string Zone);

/// <summary>A line of the input that could not be turned into a <see cref="TagRead"/>.</summary>
/// <param name="LineNumber">1-based line number in the input (the header is line 1).</param>
/// <param name="Reason">Short, human-readable reason.</param>
public sealed record ParseError(int LineNumber, string Reason);

public sealed record ParseResult(IReadOnlyList<TagRead> Reads, IReadOnlyList<ParseError> Errors);

/// <summary>
/// Several raw reads of the same tag in the same zone, collapsed into one logical observation.
/// </summary>
public sealed record Sighting(
    string Epc,
    string Zone,
    DateTimeOffset FirstSeen,
    DateTimeOffset LastSeen,
    int ReadCount,
    double MaxRssi);

/// <summary>A tool changed zone. <paramref name="FromZone"/> is null for the very first sighting.</summary>
public sealed record Movement(string Epc, string? FromZone, string ToZone, DateTimeOffset At);

public enum AssetStatus
{
    Present,
    Missing,
}

public sealed record AssetState(
    string Epc,
    string CurrentZone,
    DateTimeOffset LastSeen,
    AssetStatus Status,
    IReadOnlyDictionary<string, TimeSpan> DwellTimeByZone);

public sealed record InventoryReport(DateTimeOffset AsOf, IReadOnlyList<AssetState> Assets);

/// <summary>Decoded GS1 SGTIN-96 identifier (bonus level).</summary>
public sealed record Sgtin96(int Filter, string CompanyPrefix, string ItemReference, long SerialNumber)
{
    public string ToPureIdentityUri() => $"urn:epc:id:sgtin:{CompanyPrefix}.{ItemReference}.{SerialNumber}";
}
