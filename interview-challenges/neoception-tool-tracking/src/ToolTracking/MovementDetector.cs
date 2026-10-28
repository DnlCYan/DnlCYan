namespace ToolTracking;

/// <summary>Level 3 - derives zone changes from sightings.</summary>
public sealed class MovementDetector
{
    /// <summary>
    /// Emits a <see cref="Movement"/> whenever a tool is seen in a zone different from its
    /// previous one (and once for its very first sighting). Result is ordered by At, then Epc.
    /// See README, Level 3.
    /// </summary>
    public IReadOnlyList<Movement> Detect(IEnumerable<Sighting> sightings)
    {
        // TODO: implement Level 3
        throw new NotImplementedException();
    }
}
