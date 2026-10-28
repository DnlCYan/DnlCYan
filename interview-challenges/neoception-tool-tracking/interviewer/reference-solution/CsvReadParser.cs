using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ToolTracking;

/// <summary>Level 1 - turns the raw reader export (CSV) into validated <see cref="TagRead"/>s.</summary>
public sealed class CsvReadParser
{
    private const int ColumnCount = 5;
    private const int EpcLength = 24;

    // DateTimeOffset.TryParse silently assumes the machine's local time zone when no offset is
    // given. Readers in different plants run in different time zones, so we insist on one.
    private static readonly Regex ExplicitOffset = new(@"(Z|[+-]\d{2}:\d{2})$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private readonly ReaderConfiguration _configuration;

    public CsvReadParser(ReaderConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public ParseResult Parse(TextReader csv)
    {
        ArgumentNullException.ThrowIfNull(csv);

        var reads = new List<TagRead>();
        var errors = new List<ParseError>();
        var lineNumber = 0;

        while (csv.ReadLine() is { } line)
        {
            lineNumber++;
            if (lineNumber == 1 || string.IsNullOrWhiteSpace(line))
            {
                continue; // header / blank line
            }

            if (TryParseLine(line, out var read, out var reason))
            {
                reads.Add(read);
            }
            else
            {
                errors.Add(new ParseError(lineNumber, reason));
            }
        }

        return new ParseResult(reads, errors);
    }

    private bool TryParseLine(string line, [NotNullWhen(true)] out TagRead? read, out string reason)
    {
        read = null;
        var fields = line.Split(',').Select(f => f.Trim()).ToArray();

        if (fields.Length != ColumnCount)
        {
            reason = $"expected {ColumnCount} columns but found {fields.Length}";
            return false;
        }

        var (rawTimestamp, readerId, rawAntenna, rawEpc, rawRssi) = (fields[0], fields[1], fields[2], fields[3], fields[4]);

        if (!ExplicitOffset.IsMatch(rawTimestamp)
            || !DateTimeOffset.TryParse(rawTimestamp, CultureInfo.InvariantCulture, DateTimeStyles.None, out var timestamp))
        {
            reason = $"invalid timestamp '{rawTimestamp}' (ISO-8601 with UTC offset expected)";
            return false;
        }

        if (!int.TryParse(rawAntenna, NumberStyles.None, CultureInfo.InvariantCulture, out var antenna))
        {
            reason = $"invalid antenna '{rawAntenna}'";
            return false;
        }

        var epc = rawEpc.ToUpperInvariant();
        if (epc.Length != EpcLength || !epc.All(char.IsAsciiHexDigit))
        {
            reason = $"invalid EPC '{rawEpc}' ({EpcLength} hex characters expected)";
            return false;
        }

        if (!double.TryParse(rawRssi, NumberStyles.Float, CultureInfo.InvariantCulture, out var rssi) || !double.IsFinite(rssi))
        {
            reason = $"invalid RSSI '{rawRssi}'";
            return false;
        }

        if (!_configuration.TryGetZone(readerId, antenna, out var zone))
        {
            reason = $"unknown reader antenna {readerId}/{antenna}";
            return false;
        }

        read = new TagRead(timestamp.ToUniversalTime(), readerId, antenna, epc, rssi, zone);
        reason = string.Empty;
        return true;
    }
}
