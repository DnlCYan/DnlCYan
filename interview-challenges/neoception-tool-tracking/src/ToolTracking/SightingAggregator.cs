namespace ToolTracking;

/// <summary>Level 2 - collapses bursts of raw reads into <see cref="Sighting"/>s.</summary>
public sealed class SightingAggregator
{
    public static readonly TimeSpan DefaultWindow = TimeSpan.FromSeconds(2);

    private readonly TimeSpan _window;

    public SightingAggregator(TimeSpan? window = null)
    {
        _window = window ?? DefaultWindow;
    }

    /// <summary>
    /// Groups reads of the same EPC in the same zone that are at most <c>window</c> apart.
    /// Input may be unordered. Result is ordered by FirstSeen, then Epc. See README, Level 2.
    /// </summary>
    public IReadOnlyList<Sighting> Aggregate(IEnumerable<TagRead> reads)
    {
        // TODO: implement Level 2
        throw new NotImplementedException();
    }
}
