namespace ToolTracking;

/// <summary>Level 3 - builds the "where is every tool right now" report.</summary>
public sealed class InventoryReportBuilder
{
    public static readonly TimeSpan DefaultMissingAfter = TimeSpan.FromMinutes(30);

    private readonly TimeSpan _missingAfter;
    private readonly MovementDetector _movementDetector = new();

    public InventoryReportBuilder(TimeSpan? missingAfter = null)
    {
        _missingAfter = missingAfter ?? DefaultMissingAfter;
    }

    public InventoryReport Build(IEnumerable<Sighting> sightings, DateTimeOffset asOf)
    {
        ArgumentNullException.ThrowIfNull(sightings);

        var all = sightings.ToList();
        if (all.Any(s => s.LastSeen > asOf))
        {
            throw new ArgumentException("asOf must not be earlier than the latest sighting.", nameof(asOf));
        }

        // Detect() returns movements ordered by time; ToLookup preserves that order per tag.
        var movementsByEpc = _movementDetector.Detect(all).ToLookup(m => m.Epc, StringComparer.Ordinal);

        var assets = all
            .GroupBy(s => s.Epc, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => BuildAssetState(g.Key, g.Max(s => s.LastSeen), movementsByEpc[g.Key].ToList(), asOf))
            .ToList();

        return new InventoryReport(asOf, assets);
    }

    private AssetState BuildAssetState(string epc, DateTimeOffset lastSeen, IReadOnlyList<Movement> movements, DateTimeOffset asOf)
    {
        var dwell = new Dictionary<string, TimeSpan>(StringComparer.Ordinal);

        for (var i = 0; i < movements.Count; i++)
        {
            var enteredAt = movements[i].At;
            var leftAt = i + 1 < movements.Count ? movements[i + 1].At : asOf;
            var zone = movements[i].ToZone;
            dwell[zone] = dwell.GetValueOrDefault(zone) + (leftAt - enteredAt);
        }

        var status = asOf - lastSeen > _missingAfter ? AssetStatus.Missing : AssetStatus.Present;

        return new AssetState(epc, movements[^1].ToZone, lastSeen, status, dwell);
    }
}
