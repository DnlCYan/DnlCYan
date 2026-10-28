namespace ToolTracking;

/// <summary>Level 2 - collapses bursts of raw reads into <see cref="Sighting"/>s.</summary>
public sealed class SightingAggregator
{
    public static readonly TimeSpan DefaultWindow = TimeSpan.FromSeconds(2);

    private readonly TimeSpan _window;

    public SightingAggregator(TimeSpan? window = null)
    {
        _window = window ?? DefaultWindow;
        if (_window < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(window), "Window must not be negative.");
        }
    }

    public IReadOnlyList<Sighting> Aggregate(IEnumerable<TagRead> reads)
    {
        ArgumentNullException.ThrowIfNull(reads);

        var sightings = new List<Sighting>();

        foreach (var tagReads in reads.GroupBy(r => r.Epc, StringComparer.Ordinal))
        {
            Sighting? current = null;

            foreach (var read in tagReads.OrderBy(r => r.Timestamp))
            {
                if (current is not null
                    && current.Zone == read.Zone
                    && read.Timestamp - current.LastSeen <= _window)
                {
                    current = current with
                    {
                        LastSeen = read.Timestamp,
                        ReadCount = current.ReadCount + 1,
                        MaxRssi = Math.Max(current.MaxRssi, read.Rssi),
                    };
                }
                else
                {
                    if (current is not null)
                    {
                        sightings.Add(current);
                    }

                    current = new Sighting(read.Epc, read.Zone, read.Timestamp, read.Timestamp, 1, read.Rssi);
                }
            }

            if (current is not null)
            {
                sightings.Add(current);
            }
        }

        return sightings
            .OrderBy(s => s.FirstSeen)
            .ThenBy(s => s.Epc, StringComparer.Ordinal)
            .ToList();
    }
}
