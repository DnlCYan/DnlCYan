namespace ToolTracking.Tests;

// Level 4 (bonus) - only expected to pass if you attempt the bonus level.
public class Sgtin96DecoderTests
{
    [Fact]
    public void Decodes_the_gs1_reference_example()
    {
        // Example from the GS1 EPC Tag Data Standard.
        Assert.True(Sgtin96Decoder.TryDecode("3074257BF7194E4000001A85", out var sgtin));

        Assert.Equal(3, sgtin.Filter);
        Assert.Equal("0614141", sgtin.CompanyPrefix);
        Assert.Equal("812345", sgtin.ItemReference);
        Assert.Equal(6789, sgtin.SerialNumber);
        Assert.Equal("urn:epc:id:sgtin:0614141.812345.6789", sgtin.ToPureIdentityUri());
    }

    [Theory]
    [InlineData("3034F4E4E40C0E40000003E9", 1, "4012345", "012345", 1001)]   // partition 5
    [InlineData("306C75BCD15134800000002A", 3, "123456789", "1234", 42)]     // partition 3
    public void Decodes_tool_tags(string epc, int filter, string companyPrefix, string itemReference, long serial)
    {
        Assert.True(Sgtin96Decoder.TryDecode(epc, out var sgtin));

        Assert.Equal(new Sgtin96(filter, companyPrefix, itemReference, serial), sgtin);
    }

    [Theory]
    [InlineData("E2801160600002084D6A3B1C")] // header is not 0x30 (not an SGTIN-96)
    [InlineData("303C000000000000000003E9")] // partition 7 is not defined
    [InlineData("3074257BF7194E4000001A8")]  // too short
    [InlineData("3074257BF7194E4000001AXX")] // not hex
    [InlineData("")]
    public void Rejects_anything_that_is_not_a_valid_sgtin96(string epc)
    {
        Assert.False(Sgtin96Decoder.TryDecode(epc, out var sgtin));
        Assert.Null(sgtin);
    }
}
