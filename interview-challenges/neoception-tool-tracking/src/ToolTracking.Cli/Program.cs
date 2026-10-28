using System.Globalization;
using ToolTracking;

// Provided wiring - you do not need to change this file.
//
// Usage:
//   dotnet run --project src/ToolTracking.Cli -- <reads.csv> <readers.json> [asOf]
// Example:
//   dotnet run --project src/ToolTracking.Cli -- data/reads.csv data/readers.json 2026-03-02T12:00:00Z

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: ToolTracking.Cli <reads.csv> <readers.json> [asOf ISO-8601]");
    return 1;
}

var configuration = ReaderConfiguration.FromJson(File.ReadAllText(args[1]));
var asOf = args.Length > 2
    ? DateTimeOffset.Parse(args[2], CultureInfo.InvariantCulture).ToUniversalTime()
    : DateTimeOffset.UtcNow;

ParseResult parsed;
using (var reader = File.OpenText(args[0]))
{
    parsed = new CsvReadParser(configuration).Parse(reader);
}

Console.WriteLine($"Parsed {parsed.Reads.Count} reads, rejected {parsed.Errors.Count} lines");
foreach (var error in parsed.Errors)
{
    Console.WriteLine($"  line {error.LineNumber,4}: {error.Reason}");
}

var sightings = new SightingAggregator().Aggregate(parsed.Reads);
Console.WriteLine();
Console.WriteLine($"{sightings.Count} sightings");

Console.WriteLine();
Console.WriteLine("Movements:");
foreach (var m in new MovementDetector().Detect(sightings))
{
    Console.WriteLine($"  {Format(m.At)}  {m.Epc}  {m.FromZone ?? "(new)",-15} -> {m.ToZone}");
}

var report = new InventoryReportBuilder().Build(sightings, asOf);
Console.WriteLine();
Console.WriteLine($"Inventory as of {Format(report.AsOf)}:");
foreach (var asset in report.Assets)
{
    var label = TryDescribe(asset.Epc);
    Console.WriteLine($"  {asset.Epc}  {asset.CurrentZone,-15} last seen {Format(asset.LastSeen)}  {asset.Status,-8} {label}");
    foreach (var (zone, dwell) in asset.DwellTimeByZone.OrderBy(kv => kv.Key, StringComparer.Ordinal))
    {
        Console.WriteLine($"      {zone,-15} {dwell:hh\\:mm\\:ss\\.fff}");
    }
}

return 0;

static string Format(DateTimeOffset value) =>
    value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);

static string TryDescribe(string epc)
{
    try
    {
        return Sgtin96Decoder.TryDecode(epc, out var sgtin) ? sgtin.ToPureIdentityUri() : "(not an SGTIN-96)";
    }
    catch (NotImplementedException)
    {
        return string.Empty; // bonus level not implemented yet
    }
}
