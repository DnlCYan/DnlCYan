using static ToolTracking.Tests.TestData;

namespace ToolTracking.Tests;

public class InventoryReportBuilderTests
{
    private readonly InventoryReportBuilder _builder = new(TimeSpan.FromMinutes(30));

    [Fact]
    public void Current_zone_and_last_seen_come_from_the_latest_sighting()
    {
        var report = _builder.Build(
            new[]
            {
                SightingOf(Wrench1, "Warehouse", 0, 1),
                SightingOf(Wrench1, "Production", 900, 905),
            },
            asOf: T0.AddMinutes(20));

        var asset = Assert.Single(report.Assets);
        Assert.Equal(Wrench1, asset.Epc);
        Assert.Equal("Production", asset.CurrentZone);
        Assert.Equal(T0.AddSeconds(905), asset.LastSeen);
        Assert.Equal(AssetStatus.Present, asset.Status);
        Assert.Equal(T0.AddMinutes(20), report.AsOf);
    }

    [Theory]
    [InlineData(30 * 60, AssetStatus.Present)]      // exactly at the threshold
    [InlineData(30 * 60 + 1, AssetStatus.Missing)]  // one second over
    public void A_tool_is_missing_when_not_seen_for_longer_than_the_threshold(int secondsSinceLastSeen, AssetStatus expected)
    {
        var report = _builder.Build(
            new[] { SightingOf(Wrench1, "Warehouse", 0, 10) },
            asOf: T0.AddSeconds(10 + secondsSinceLastSeen));

        Assert.Equal(expected, Assert.Single(report.Assets).Status);
    }

    [Fact]
    public void Dwell_time_runs_from_entering_a_zone_until_entering_the_next_one_or_as_of()
    {
        var report = _builder.Build(
            new[]
            {
                SightingOf(Wrench1, "Warehouse", 0, 1),            // Warehouse 08:00 -> 08:15
                SightingOf(Wrench1, "Production", 900, 901),       // Production 08:15 -> 09:00
                SightingOf(Wrench1, "Production", 1800, 1801),     // (same zone, no movement)
                SightingOf(Wrench1, "Warehouse", 3600, 3601),      // Warehouse 09:00 -> 09:10 (asOf)
            },
            asOf: T0.AddMinutes(70));

        var dwell = Assert.Single(report.Assets).DwellTimeByZone;
        Assert.Equal(2, dwell.Count);
        Assert.Equal(TimeSpan.FromMinutes(15 + 10), dwell["Warehouse"]);
        Assert.Equal(TimeSpan.FromMinutes(45), dwell["Production"]);
    }

    [Fact]
    public void Assets_are_ordered_by_epc()
    {
        var report = _builder.Build(
            new[]
            {
                SightingOf(Wrench2, "Warehouse", 0, 1),
                SightingOf(Caliper, "Warehouse", 0, 1),
                SightingOf(Wrench1, "Warehouse", 0, 1),
            },
            asOf: T0.AddMinutes(1));

        Assert.Equal(new[] { Wrench1, Wrench2, Caliper }, report.Assets.Select(a => a.Epc));
    }

    [Fact]
    public void Rejects_as_of_that_is_earlier_than_the_data()
    {
        Assert.Throws<ArgumentException>(() => _builder.Build(
            new[] { SightingOf(Wrench1, "Warehouse", 0, 60) },
            asOf: T0.AddSeconds(30)));
    }

    [Fact]
    public void No_sightings_gives_an_empty_report()
    {
        Assert.Empty(_builder.Build(Array.Empty<Sighting>(), T0).Assets);
    }
}
