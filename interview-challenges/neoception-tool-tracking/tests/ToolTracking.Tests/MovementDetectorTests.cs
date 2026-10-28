using static ToolTracking.Tests.TestData;

namespace ToolTracking.Tests;

public class MovementDetectorTests
{
    private readonly MovementDetector _detector = new();

    [Fact]
    public void First_sighting_is_a_movement_from_nowhere()
    {
        var movement = Assert.Single(_detector.Detect(new[] { SightingOf(Wrench1, "Warehouse", 0, 1) }));

        Assert.Equal(new Movement(Wrench1, null, "Warehouse", T0), movement);
    }

    [Fact]
    public void Repeated_sightings_in_the_same_zone_are_not_movements()
    {
        var movements = _detector.Detect(new[]
        {
            SightingOf(Wrench1, "Warehouse", 0, 1),
            SightingOf(Wrench1, "Warehouse", 600, 601),
        });

        Assert.Single(movements);
    }

    [Fact]
    public void Detects_each_zone_change_at_the_first_read_in_the_new_zone()
    {
        var movements = _detector.Detect(new[]
        {
            SightingOf(Wrench1, "Production", 900, 901),   // input is not ordered
            SightingOf(Wrench1, "Warehouse", 0, 1),
            SightingOf(Wrench1, "Warehouse", 3600, 3605),
        });

        Assert.Equal(
            new[]
            {
                new Movement(Wrench1, null, "Warehouse", T0),
                new Movement(Wrench1, "Warehouse", "Production", T0.AddSeconds(900)),
                new Movement(Wrench1, "Production", "Warehouse", T0.AddSeconds(3600)),
            },
            movements);
    }

    [Fact]
    public void Tracks_each_tool_independently()
    {
        var movements = _detector.Detect(new[]
        {
            SightingOf(Wrench1, "Warehouse", 0, 1),
            SightingOf(Wrench2, "Production", 10, 11),
            SightingOf(Wrench1, "Production", 20, 21),
        });

        Assert.Equal(
            new[]
            {
                new Movement(Wrench1, null, "Warehouse", T0),
                new Movement(Wrench2, null, "Production", T0.AddSeconds(10)),
                new Movement(Wrench1, "Warehouse", "Production", T0.AddSeconds(20)),
            },
            movements);
    }
}
