using static ToolTracking.Tests.TestData;

namespace ToolTracking.Tests;

public class SightingAggregatorTests
{
    private readonly SightingAggregator _aggregator = new(TimeSpan.FromSeconds(2));

    [Fact]
    public void Collapses_a_burst_of_reads_into_one_sighting()
    {
        var sightings = _aggregator.Aggregate(new[]
        {
            Read(Wrench1, "Warehouse", 0.0, rssi: -61),
            Read(Wrench1, "Warehouse", 0.8, rssi: -54),
            Read(Wrench1, "Warehouse", 1.6, rssi: -58),
        });

        var s = Assert.Single(sightings);
        Assert.Equal(Wrench1, s.Epc);
        Assert.Equal("Warehouse", s.Zone);
        Assert.Equal(T0, s.FirstSeen);
        Assert.Equal(T0.AddSeconds(1.6), s.LastSeen);
        Assert.Equal(3, s.ReadCount);
        Assert.Equal(-54, s.MaxRssi);
    }

    [Fact]
    public void Window_is_measured_from_the_previous_read_not_the_first_one()
    {
        // 0 -> 1.5 -> 3.0 -> 4.5: every gap is 1.5s, so this is one continuous sighting.
        var sightings = _aggregator.Aggregate(new[]
        {
            Read(Wrench1, "Warehouse", 0.0),
            Read(Wrench1, "Warehouse", 1.5),
            Read(Wrench1, "Warehouse", 3.0),
            Read(Wrench1, "Warehouse", 4.5),
        });

        var s = Assert.Single(sightings);
        Assert.Equal(4, s.ReadCount);
    }

    [Fact]
    public void A_gap_of_exactly_the_window_still_belongs_to_the_same_sighting()
    {
        var sightings = _aggregator.Aggregate(new[]
        {
            Read(Wrench1, "Warehouse", 0.0),
            Read(Wrench1, "Warehouse", 2.0),
        });

        Assert.Single(sightings);
    }

    [Fact]
    public void A_gap_larger_than_the_window_starts_a_new_sighting()
    {
        var sightings = _aggregator.Aggregate(new[]
        {
            Read(Wrench1, "Warehouse", 0.0),
            Read(Wrench1, "Warehouse", 2.001),
        });

        Assert.Equal(2, sightings.Count);
    }

    [Fact]
    public void A_zone_change_starts_a_new_sighting_even_inside_the_window()
    {
        var sightings = _aggregator.Aggregate(new[]
        {
            Read(Wrench1, "Warehouse", 0.0),
            Read(Wrench1, "Production", 0.5),
        });

        Assert.Equal(new[] { "Warehouse", "Production" }, sightings.Select(s => s.Zone));
    }

    [Fact]
    public void Handles_out_of_order_input()
    {
        var sightings = _aggregator.Aggregate(new[]
        {
            Read(Wrench1, "Warehouse", 1.0),
            Read(Wrench1, "Warehouse", 0.0),
            Read(Wrench1, "Warehouse", 0.5),
        });

        var s = Assert.Single(sightings);
        Assert.Equal(T0, s.FirstSeen);
        Assert.Equal(T0.AddSeconds(1), s.LastSeen);
    }

    [Fact]
    public void Tags_are_aggregated_independently_and_result_is_ordered_by_first_seen_then_epc()
    {
        var sightings = _aggregator.Aggregate(new[]
        {
            Read(Wrench2, "Warehouse", 5.0),
            Read(Wrench1, "Warehouse", 0.0),
            Read(Wrench2, "Warehouse", 0.0),
            Read(Wrench1, "Warehouse", 1.0),
        });

        Assert.Equal(
            new[] { (Wrench1, 0.0), (Wrench2, 0.0), (Wrench2, 5.0) },
            sightings.Select(s => (s.Epc, (s.FirstSeen - T0).TotalSeconds)));
    }

    [Fact]
    public void Empty_input_gives_no_sightings()
    {
        Assert.Empty(_aggregator.Aggregate(Array.Empty<TagRead>()));
    }
}
