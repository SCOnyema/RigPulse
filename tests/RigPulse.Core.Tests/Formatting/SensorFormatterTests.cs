using System.Globalization;
using RigPulse.Core.Formatting;
using RigPulse.Core.Models;

namespace RigPulse.Core.Tests.Formatting;

public sealed class SensorFormatterTests
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    private const string Dash = ByteFormatter.Unavailable;

    [Theory]
    [InlineData(72.4, "72°C")]
    [InlineData(72.5, "73°C")]
    [InlineData(0, "0°C")]
    [InlineData(-1, Dash)]
    [InlineData(double.NaN, Dash)]
    public void FormatTemperature(double celsius, string expected)
    {
        SensorFormatter.FormatTemperature(celsius, Invariant).ShouldBe(expected);
    }

    [Fact]
    public void FormatTemperature_Null_IsDash()
    {
        SensorFormatter.FormatTemperature(null, Invariant).ShouldBe(Dash);
    }

    [Theory]
    [InlineData(3412, "3.41 GHz")]
    [InlineData(300, "0.30 GHz")]
    [InlineData(4999.6, "5.00 GHz")]
    [InlineData(double.PositiveInfinity, Dash)]
    public void FormatClock(double mhz, string expected)
    {
        SensorFormatter.FormatClock(mhz, Invariant).ShouldBe(expected);
    }

    [Fact]
    public void FormatClockRange_ShowsAverageAndMax()
    {
        SensorFormatter.FormatClockRange(3137.6, 3242.2, Invariant).ShouldBe("avg 3.14 · max 3.24 GHz");
    }

    [Fact]
    public void FormatClockRange_MissingValue_IsDash()
    {
        SensorFormatter.FormatClockRange(null, 3000, Invariant).ShouldBe(Dash);
        SensorFormatter.FormatClockRange(3000, null, Invariant).ShouldBe(Dash);
    }

    [Fact]
    public void FormatClock_UsesCultureDecimalSeparator()
    {
        SensorFormatter.FormatClock(3412, CultureInfo.GetCultureInfo("de-DE")).ShouldBe("3,41 GHz");
    }

    [Fact]
    public void FormatVram_SharesOneUnit()
    {
        const ulong GiB = 1024UL * 1024 * 1024;

        SensorFormatter.FormatVram((ulong)(3.1 * GiB), 8 * GiB, Invariant).ShouldBe("3.1 / 8.0 GB");
        SensorFormatter.FormatVram(256UL * 1024 * 1024, 4 * GiB, Invariant).ShouldBe("0.3 / 4.0 GB");
    }

    [Fact]
    public void FormatVram_MissingOrZeroTotal_IsDash()
    {
        SensorFormatter.FormatVram(null, 8UL, Invariant).ShouldBe(Dash);
        SensorFormatter.FormatVram(1UL, null, Invariant).ShouldBe(Dash);
        SensorFormatter.FormatVram(0UL, 0UL, Invariant).ShouldBe(Dash);
    }

    [Theory]
    [InlineData(1240, "1,240 RPM")]
    [InlineData(860.4, "860 RPM")]
    [InlineData(-1, Dash)]
    public void FormatRpm(double rpm, string expected)
    {
        SensorFormatter.FormatRpm(rpm, Invariant).ShouldBe(expected);
    }

    [Theory]
    [InlineData(87, "87%")]
    [InlineData(100, "100%")]
    public void FormatPercent(double percent, string expected)
    {
        SensorFormatter.FormatPercent(percent, Invariant).ShouldBe(expected);
    }

    [Theory]
    [InlineData(8040, "2 h 14 m")]
    [InlineData(7500, "2 h 05 m")]
    [InlineData(2700, "45 m")]
    [InlineData(59, "0 m")]
    [InlineData(90000, "25 h 00 m")]
    public void FormatDuration(int seconds, string expected)
    {
        SensorFormatter.FormatDuration(TimeSpan.FromSeconds(seconds), Invariant).ShouldBe(expected);
    }

    [Fact]
    public void FormatDuration_Negative_IsDash()
    {
        SensorFormatter.FormatDuration(TimeSpan.FromMinutes(-1), Invariant).ShouldBe(Dash);
    }

    [Theory]
    [InlineData(BatteryChargeState.Charging, null, "Charging")]
    [InlineData(BatteryChargeState.Full, null, "Full")]
    [InlineData(BatteryChargeState.PluggedIn, null, "Plugged in")]
    [InlineData(BatteryChargeState.Discharging, null, "Discharging")]
    [InlineData(BatteryChargeState.Discharging, 8040, "Discharging · 2 h 14 m")]
    public void FormatBatteryStatus(BatteryChargeState state, int? secondsLeft, string expected)
    {
        var battery = new BatteryStats(50, state, secondsLeft is { } s ? TimeSpan.FromSeconds(s) : null);

        SensorFormatter.FormatBatteryStatus(battery, Invariant).ShouldBe(expected);
    }
}
