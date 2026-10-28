namespace ToolTracking;

/// <summary>Level 3 - builds the "where is every tool right now" report.</summary>
public sealed class InventoryReportBuilder
{
    public static readonly TimeSpan DefaultMissingAfter = TimeSpan.FromMinutes(30);

    private readonly TimeSpan _missingAfter;

    public InventoryReportBuilder(TimeSpan? missingAfter = null)
    {
        _missingAfter = missingAfter ?? DefaultMissingAfter;
    }

    /// <summary>
    /// Builds the report as of <paramref name="asOf"/>. Assets are ordered by Epc (ordinal).
    /// See README, Level 3.
    /// </summary>
    public InventoryReport Build(IEnumerable<Sighting> sightings, DateTimeOffset asOf)
    {
        // TODO: implement Level 3
        throw new NotImplementedException();
    }
}
