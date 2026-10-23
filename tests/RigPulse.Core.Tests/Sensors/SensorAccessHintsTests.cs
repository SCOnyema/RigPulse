using RigPulse.Core.Sensors;

namespace RigPulse.Core.Tests.Sensors;

public sealed class SensorAccessHintsTests
{
    [Theory]
    [InlineData(false, false, SensorAccessHints.RunAsAdmin)]
    [InlineData(false, true, SensorAccessHints.RunAsAdmin)]
    [InlineData(true, false, SensorAccessHints.InstallDriver)]
    [InlineData(true, true, null)]
    public void For_PicksHintByAccess(bool isElevated, bool isDriverInstalled, string? expected)
    {
        SensorAccessHints.For(isElevated, isDriverInstalled).ShouldBe(expected);
    }
}
