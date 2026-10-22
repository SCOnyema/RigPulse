using System.Globalization;
using RigPulse.Core.Formatting;

namespace RigPulse.Core.Tests.Formatting;

public sealed class ByteFormatterTests
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(1, "1 B")]
    [InlineData(512, "512 B")]
    [InlineData(1023, "1023 B")]
    [InlineData(1024, "1.0 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1_048_576, "1.0 MB")]
    [InlineData(1_288_490_189, "1.2 GB")]
    [InlineData(17_179_869_184, "16.0 GB")]
    [InlineData(1_099_511_627_776, "1.0 TB")]
    public void FormatBytes_UsesBinaryUnits(double bytes, string expected)
    {
        ByteFormatter.FormatBytes(bytes, Invariant).ShouldBe(expected);
    }

    [Theory]
    [InlineData(0, "0 B/s")]
    [InlineData(900, "900 B/s")]
    [InlineData(3_565_158, "3.4 MB/s")]
    [InlineData(125_829_120, "120.0 MB/s")]
    public void FormatRate_AppendsPerSecond(double bytesPerSecond, string expected)
    {
        ByteFormatter.FormatRate(bytesPerSecond, Invariant).ShouldBe(expected);
    }

    [Theory]
    [InlineData(1023.6, "1.0 KB")]    // would otherwise print "1024 B"
    [InlineData(1_048_525, "1.0 MB")] // 1023.95 KB would otherwise print "1024.0 KB"
    public void Format_PromotesUnit_WhenRoundingReachesNextUnit(double bytes, string expected)
    {
        ByteFormatter.FormatBytes(bytes, Invariant).ShouldBe(expected);
    }

    [Fact]
    public void Format_HugeValues_StayInLargestUnit()
    {
        // 1024^6 bytes is one exabyte, beyond the largest unit, so it stays in PB.
        ByteFormatter.FormatBytes(Math.Pow(1024, 6), Invariant).ShouldBe("1024.0 PB");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Format_InvalidInput_ReturnsUnavailable(double value)
    {
        ByteFormatter.FormatBytes(value, Invariant).ShouldBe(ByteFormatter.Unavailable);
        ByteFormatter.FormatRate(value, Invariant).ShouldBe(ByteFormatter.Unavailable);
    }

    [Fact]
    public void Format_RespectsCultureDecimalSeparator()
    {
        ByteFormatter.FormatBytes(1536, CultureInfo.GetCultureInfo("de-DE")).ShouldBe("1,5 KB");
    }
}
