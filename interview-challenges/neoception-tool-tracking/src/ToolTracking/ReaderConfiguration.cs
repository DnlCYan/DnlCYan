using System.Text.Json;

namespace ToolTracking;

/// <summary>Maps a (reader, antenna) pair to the zone it is installed in.</summary>
public sealed record ReaderAntenna(string ReaderId, int Antenna, string Zone);

/// <summary>
/// Provided infrastructure - you do not need to change this class.
/// </summary>
public sealed class ReaderConfiguration
{
    private readonly Dictionary<(string ReaderId, int Antenna), string> _zones;

    public ReaderConfiguration(IEnumerable<ReaderAntenna> antennas)
    {
        _zones = new Dictionary<(string, int), string>();
        foreach (var a in antennas)
        {
            _zones[(a.ReaderId, a.Antenna)] = a.Zone;
        }
    }

    public bool TryGetZone(string readerId, int antenna, out string zone)
    {
        if (_zones.TryGetValue((readerId, antenna), out var z))
        {
            zone = z;
            return true;
        }

        zone = string.Empty;
        return false;
    }

    public static ReaderConfiguration FromJson(string json)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var antennas = JsonSerializer.Deserialize<List<ReaderAntenna>>(json, options)
                       ?? throw new InvalidDataException("Reader configuration is empty.");
        return new ReaderConfiguration(antennas);
    }
}
