using RigPulse.Core.Models;

namespace RigPulse.Sensors.Tests;

public sealed class BatteryProviderTests
{
    private const byte AcOffline = 0;
    private const byte AcOnline = 1;
    private const byte FlagHigh = 1;
    private const byte FlagCharging = 8;
    private const byte FlagNoBattery = 128;
    private const byte FlagUnknown = 255;
    private const uint LifeUnknown = uint.MaxValue;

    [Theory]
    [InlineData(FlagNoBattery)]
    [InlineData(FlagUnknown)]
    public void Map_Desktop_ReturnsNull(byte flag)
    {
        BatteryProvider.Map(AcOnline, flag, 255, LifeUnknown).ShouldBeNull();
    }

    [Fact]
    public void Map_UnknownPercent_ReturnsNull()
    {
        BatteryProvider.Map(AcOffline, FlagHigh, 255, LifeUnknown).ShouldBeNull();
    }

    [Fact]
    public void Map_Discharging_WithEstimate()
    {
        BatteryProvider.Map(AcOffline, FlagHigh, 87, 8040)
            .ShouldBe(new BatteryStats(87, BatteryChargeState.Discharging, TimeSpan.FromSeconds(8040)));
    }

    [Fact]
    public void Map_Discharging_EstimateNotReadyYet()
    {
        BatteryProvider.Map(AcOffline, FlagHigh, 87, LifeUnknown)
            .ShouldBe(new BatteryStats(87, BatteryChargeState.Discharging, null));
    }

    [Fact]
    public void Map_Charging_HasNoTimeRemaining()
    {
        BatteryProvider.Map(AcOnline, FlagHigh | FlagCharging, 40, 5000)
            .ShouldBe(new BatteryStats(40, BatteryChargeState.Charging, null));
    }

    [Fact]
    public void Map_PluggedInAndFull()
    {
        BatteryProvider.Map(AcOnline, FlagHigh, 100, LifeUnknown)
            .ShouldBe(new BatteryStats(100, BatteryChargeState.Full, null));
    }

    [Fact]
    public void Map_PluggedInNotCharging_ChargeLimit()
    {
        BatteryProvider.Map(AcOnline, FlagHigh, 60, LifeUnknown)
            .ShouldBe(new BatteryStats(60, BatteryChargeState.PluggedIn, null));
    }
}
