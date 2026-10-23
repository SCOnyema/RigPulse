using RigPulse.Core.Abstractions;
using RigPulse.Core.Models;

namespace RigPulse.Sensors.Tests;

public sealed class HardwareSensorProviderTests
{
    private const string NvidiaId = "/gpu-nvidia/0";
    private const string IntelId = "/gpu-intel-integrated/0";

    [Fact]
    public void Read_NoReadings_ReturnsNull()
    {
        var provider = new HardwareSensorProvider(new FakeSensorSource());

        provider.Read().ShouldBeNull();
    }

    [Fact]
    public void Read_RemembersChosenGpuBetweenReads()
    {
        var source = new FakeSensorSource();
        var provider = new HardwareSensorProvider(source);

        // Idle: ambiguous, so the discrete GPU is chosen.
        source.Readings = Gpus(nvidiaLoad: 0, intelLoad: 5);
        provider.Read()!.Gpu!.Name.ShouldBe("NVIDIA");

        // The iGPU is a little busier, but not by enough to switch.
        source.Readings = Gpus(nvidiaLoad: 0, intelLoad: 9);
        provider.Read()!.Gpu!.Name.ShouldBe("NVIDIA");

        // Now it is clearly busier.
        source.Readings = Gpus(nvidiaLoad: 0, intelLoad: 50);
        provider.Read()!.Gpu!.Name.ShouldBe("Intel");

        // And it stays chosen while loads are close again.
        source.Readings = Gpus(nvidiaLoad: 3, intelLoad: 0);
        provider.Read()!.Gpu!.Name.ShouldBe("Intel");
    }

    private static SensorReading[] Gpus(double nvidiaLoad, double intelLoad) =>
    [
        new(NvidiaId, "NVIDIA", SensorHardwareKind.DiscreteGpu, SensorKind.Temperature, "GPU Core", 50),
        new(NvidiaId, "NVIDIA", SensorHardwareKind.DiscreteGpu, SensorKind.Load, "D3D 3D", nvidiaLoad),
        new(IntelId, "Intel", SensorHardwareKind.IntegratedGpu, SensorKind.Temperature, "GPU Core", 55),
        new(IntelId, "Intel", SensorHardwareKind.IntegratedGpu, SensorKind.Load, "D3D 3D", intelLoad),
    ];

    private sealed class FakeSensorSource : ISensorSource
    {
        public IReadOnlyList<SensorReading> Readings { get; set; } = [];

        public IReadOnlyList<SensorReading> ReadAll() => Readings;
    }
}
