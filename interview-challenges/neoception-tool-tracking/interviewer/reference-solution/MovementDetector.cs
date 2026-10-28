namespace ToolTracking;

/// <summary>Level 3 - derives zone changes from sightings.</summary>
public sealed class MovementDetector
{
    public IReadOnlyList<Movement> Detect(IEnumerable<Sighting> sightings)
    {
        ArgumentNullException.ThrowIfNull(sightings);

        var movements = new List<Movement>();

        foreach (var tagSightings in sightings.GroupBy(s => s.Epc, StringComparer.Ordinal))
        {
            string? previousZone = null;

            foreach (var sighting in tagSightings.OrderBy(s => s.FirstSeen))
            {
                if (sighting.Zone != previousZone)
                {
                    movements.Add(new Movement(sighting.Epc, previousZone, sighting.Zone, sighting.FirstSeen));
                    previousZone = sighting.Zone;
                }
            }
        }

        return movements
            .OrderBy(m => m.At)
            .ThenBy(m => m.Epc, StringComparer.Ordinal)
            .ToList();
    }
}
