namespace ToolTracking.Tests;

public class CsvReadParserTests
{
    private const string Header = "timestamp,readerId,antenna,epc,rssi";

    private static ParseResult Parse(params string[] lines) =>
        new CsvReadParser(TestData.Readers()).Parse(new StringReader(string.Join("\n", lines)));

    [Fact]
    public void Parses_a_valid_line_and_resolves_the_zone()
    {
        var result = Parse(Header, $"2026-03-02T08:00:00.250Z,PROD-01,1,{TestData.Wrench1},-55.5");

        Assert.Empty(result.Errors);
        var read = Assert.Single(result.Reads);
        Assert.Equal(TestData.At("2026-03-02T08:00:00.250Z"), read.Timestamp);
        Assert.Equal("PROD-01", read.ReaderId);
        Assert.Equal(1, read.Antenna);
        Assert.Equal(TestData.Wrench1, read.Epc);
        Assert.Equal(-55.5, read.Rssi);
        Assert.Equal("Production", read.Zone);
    }

    [Fact]
    public void Normalizes_timestamps_to_utc()
    {
        var result = Parse(Header, $"2026-03-02T09:00:00+01:00,WH-01,1,{TestData.Wrench1},-50");

        var read = Assert.Single(result.Reads);
        Assert.Equal(TimeSpan.Zero, read.Timestamp.Offset);
        Assert.Equal(TestData.At("2026-03-02T08:00:00Z"), read.Timestamp);
    }

    [Fact]
    public void Normalizes_epc_to_trimmed_upper_case()
    {
        var result = Parse(Header, $"2026-03-02T08:00:00Z,WH-01,1, {TestData.Wrench1.ToLowerInvariant()} ,-50");

        Assert.Equal(TestData.Wrench1, Assert.Single(result.Reads).Epc);
    }

    [Fact]
    public void Ignores_blank_lines_without_reporting_them()
    {
        var result = Parse(Header, "", $"2026-03-02T08:00:00Z,WH-01,1,{TestData.Wrench1},-50", "   ");

        Assert.Single(result.Reads);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("2026-03-02T08:00:00Z,WH-01,1,3034F4E4E40C0E40000003E,-50")]      // EPC too short
    [InlineData("2026-03-02T08:00:00Z,WH-01,1,3034F4E4E40C0E40000003EZ,-50")]     // EPC not hex
    [InlineData("2026-03-02T08:00:00Z,DOCK-09,1,3034F4E4E40C0E40000003E9,-50")]   // unknown reader
    [InlineData("2026-03-02T08:00:00Z,WH-01,7,3034F4E4E40C0E40000003E9,-50")]     // unknown antenna
    [InlineData("2026-03-02T08:00:00Z,WH-01,x,3034F4E4E40C0E40000003E9,-50")]     // antenna not a number
    [InlineData("yesterday,WH-01,1,3034F4E4E40C0E40000003E9,-50")]                // not a timestamp
    [InlineData("2026-03-02T08:00:00,WH-01,1,3034F4E4E40C0E40000003E9,-50")]      // no UTC offset
    [InlineData("2026-03-02T08:00:00Z,WH-01,1,3034F4E4E40C0E40000003E9,n/a")]     // bad RSSI
    [InlineData("2026-03-02T08:00:00Z,WH-01,1,3034F4E4E40C0E40000003E9")]         // missing column
    [InlineData("2026-03-02T08:00:00Z,WH-01,1,3034F4E4E40C0E40000003E9,-50,x")]   // extra column
    public void Rejects_invalid_lines_with_their_line_number(string badLine)
    {
        var result = Parse(
            Header,
            $"2026-03-02T08:00:00Z,WH-01,1,{TestData.Wrench1},-50",
            badLine,
            $"2026-03-02T08:00:01Z,WH-01,1,{TestData.Wrench1},-50");

        Assert.Equal(2, result.Reads.Count);
        var error = Assert.Single(result.Errors);
        Assert.Equal(3, error.LineNumber);
        Assert.False(string.IsNullOrWhiteSpace(error.Reason));
    }
}
