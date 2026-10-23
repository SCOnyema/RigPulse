using RigPulse.Core.Models;
using RigPulse.Core.Sensors;

namespace RigPulse.Core.Tests.Sensors;

public sealed class HardwareSensorSelectorTests
{
    private const string CpuId = "/intelcpu/0";
    private const string NvidiaId = "/gpu-nvidia/0";
    private const string IntelIgpuId = "/gpu-intel-integrated/0";
    private const string AmdId = "/gpu-amd/0";

    // ---- CPU temperature ----

    [Fact]
    public void SelectCpu_Intel_UsesCpuPackage()
    {
        var readings = Cpu(
            Temp("Core Max", 81),
            Temp("Core Average", 74),
            Temp("CPU Package", 77));

        HardwareSensorSelector.SelectCpu(readings)!.PackageTemperatureCelsius.ShouldBe(77);
    }

    [Theory]
    [InlineData("Core (Tctl/Tdie)")]
    [InlineData("Tctl/Tdie")]
    [InlineData("Tdie")]
    [InlineData("Core (Tdie)")]
    [InlineData("Tctl")]
    [InlineData("CPU Cores")]
    public void SelectCpu_AmdNames_AreRecognised(string name)
    {
        var readings = Cpu(Temp(name, 63));

        HardwareSensorSelector.SelectCpu(readings)!.PackageTemperatureCelsius.ShouldBe(63);
    }

    [Fact]
    public void SelectCpu_AmdZen_PrefersTctlTdieOverCcdAndTctl()
    {
        var readings = Cpu(
            Temp("CCD1 (Tdie)", 70),
            Temp("Core (Tctl)", 80),
            Temp("Core (Tctl/Tdie)", 65));

        HardwareSensorSelector.SelectCpu(readings)!.PackageTemperatureCelsius.ShouldBe(65);
    }

    [Fact]
    public void SelectCpu_PrefersTdieOverOffsetTctl()
    {
        // First-generation Ryzen reports Tctl with a +10/+20 °C fan offset; Tdie is the real value.
        var readings = Cpu(Temp("Core (Tctl)", 80), Temp("Core (Tdie)", 60));

        HardwareSensorSelector.SelectCpu(readings)!.PackageTemperatureCelsius.ShouldBe(60);
    }

    [Fact]
    public void SelectCpu_FallsBackWhenPreferredSensorHasNoValue()
    {
        var readings = Cpu(Temp("CPU Package", null), Temp("Core Max", 72));

        HardwareSensorSelector.SelectCpu(readings)!.PackageTemperatureCelsius.ShouldBe(72);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(200)]
    [InlineData(double.NaN)]
    public void SelectCpu_IgnoresImplausibleTemperatures(double bogus)
    {
        var readings = Cpu(Temp("CPU Package", bogus), Temp("Core Average", 55));

        HardwareSensorSelector.SelectCpu(readings)!.PackageTemperatureCelsius.ShouldBe(55);
    }

    [Fact]
    public void SelectCpu_NonElevated_AllNull_ReturnsStatsWithNulls()
    {
        var readings = Cpu(Temp("CPU Package", null), Clock("CPU Core #1", null));

        var cpu = HardwareSensorSelector.SelectCpu(readings);

        cpu.ShouldNotBeNull();
        cpu.PackageTemperatureCelsius.ShouldBeNull();
        cpu.AverageCoreClockMhz.ShouldBeNull();
        cpu.MaxCoreClockMhz.ShouldBeNull();
    }

    [Fact]
    public void SelectCpu_NoCpuReadings_ReturnsNull()
    {
        HardwareSensorSelector.SelectCpu([Gpu(NvidiaId, SensorHardwareKind.DiscreteGpu, SensorKind.Temperature, "GPU Core", 50)]).ShouldBeNull();
    }

    [Fact]
    public void SelectCpu_IgnoresTemperatureOfOtherDevices()
    {
        var readings = new List<SensorReading>(Cpu(Temp("CPU Package", null)))
        {
            new("/motherboard", "Board", SensorHardwareKind.Motherboard, SensorKind.Temperature, "CPU Package", 40),
        };

        HardwareSensorSelector.SelectCpu(readings)!.PackageTemperatureCelsius.ShouldBeNull();
    }

    [Fact]
    public void SelectCpu_DualSocket_ReportsHotterPackage()
    {
        SensorReading[] readings =
        [
            new("/intelcpu/0", "Xeon", SensorHardwareKind.Cpu, SensorKind.Temperature, "CPU Package", 60),
            new("/intelcpu/1", "Xeon", SensorHardwareKind.Cpu, SensorKind.Temperature, "CPU Package", 68),
        ];

        HardwareSensorSelector.SelectCpu(readings)!.PackageTemperatureCelsius.ShouldBe(68);
    }

    // ---- CPU clocks ----

    [Fact]
    public void SelectCpu_Clocks_AverageAndMaxOfCores()
    {
        var readings = Cpu(
            Clock("CPU Core #1", 3000),
            Clock("CPU Core #2", 4000),
            Clock("CPU Core #3", 3500),
            Clock("Bus Speed", 100));

        var cpu = HardwareSensorSelector.SelectCpu(readings)!;

        cpu.AverageCoreClockMhz.ShouldBe(3500);
        cpu.MaxCoreClockMhz.ShouldBe(4000);
    }

    [Fact]
    public void SelectCpu_Clocks_AmdAndHybridNames_ExcludingEffectiveAndAggregates()
    {
        var readings = Cpu(
            Clock("Core #1", 4200),
            Clock("Core #1 (Effective)", 900),
            Clock("Cores (Average)", 1),
            Clock("Cores (Average Effective)", 1),
            Clock("P-Core #1", 5000),
            Clock("E-Core #2", 3800));

        var cpu = HardwareSensorSelector.SelectCpu(readings)!;

        cpu.AverageCoreClockMhz.ShouldNotBeNull().ShouldBe(4333.333, tolerance: 0.001);
        cpu.MaxCoreClockMhz.ShouldBe(5000);
    }

    [Fact]
    public void SelectCpu_Clocks_IgnoresZeroAndNull()
    {
        var readings = Cpu(Clock("CPU Core #1", 0), Clock("CPU Core #2", null), Clock("CPU Core #3", 2000));

        var cpu = HardwareSensorSelector.SelectCpu(readings)!;

        cpu.AverageCoreClockMhz.ShouldBe(2000);
        cpu.MaxCoreClockMhz.ShouldBe(2000);
    }

    // ---- GPU selection ----

    [Fact]
    public void SelectGpu_NoGpus_ReturnsNull()
    {
        HardwareSensorSelector.SelectGpuHardwareId(Cpu(Temp("CPU Package", 50)), null).ShouldBeNull();
    }

    [Fact]
    public void SelectGpu_SingleGpu_IsChosen()
    {
        var readings = NvidiaWithLoad(load: 0);

        HardwareSensorSelector.SelectGpuHardwareId(readings, null).ShouldBe(NvidiaId);
    }

    [Fact]
    public void SelectGpu_ClearlyBusiestWins_EvenIfIntegrated()
    {
        var readings = Concat(NvidiaWithLoad(load: 5), IntelWithSensors(load: 60));

        HardwareSensorSelector.SelectGpuHardwareId(readings, null).ShouldBe(IntelIgpuId);
    }

    [Fact]
    public void SelectGpu_Ambiguous_PrefersDiscrete()
    {
        var readings = Concat(IntelWithSensors(load: 8), NvidiaWithLoad(load: 2));

        HardwareSensorSelector.SelectGpuHardwareId(readings, null).ShouldBe(NvidiaId);
    }

    [Fact]
    public void SelectGpu_IgnoresGpusWithoutSensors()
    {
        // A busy Intel iGPU that reports only load and shared memory can't show anything useful.
        var readings = Concat(
            NvidiaWithLoad(load: 0),
            [
                Gpu(IntelIgpuId, SensorHardwareKind.IntegratedGpu, SensorKind.Load, "D3D 3D", 90),
                Gpu(IntelIgpuId, SensorHardwareKind.IntegratedGpu, SensorKind.SmallData, "D3D Shared Memory Used", 600),
            ]);

        HardwareSensorSelector.SelectGpuHardwareId(readings, null).ShouldBe(NvidiaId);
    }

    [Fact]
    public void SelectGpu_KeepsPreviousUntilAnotherIsClearlyBusier()
    {
        var slightlyBusier = Concat(NvidiaWithLoad(load: 20), IntelWithSensors(load: 25));
        HardwareSensorSelector.SelectGpuHardwareId(slightlyBusier, previousHardwareId: NvidiaId).ShouldBe(NvidiaId);

        var clearlyBusier = Concat(NvidiaWithLoad(load: 20), IntelWithSensors(load: 30));
        HardwareSensorSelector.SelectGpuHardwareId(clearlyBusier, previousHardwareId: NvidiaId).ShouldBe(IntelIgpuId);
    }

    [Fact]
    public void SelectGpu_PreviousGpuGone_Reselects()
    {
        var readings = NvidiaWithLoad(load: 0);

        HardwareSensorSelector.SelectGpuHardwareId(readings, previousHardwareId: AmdId).ShouldBe(NvidiaId);
    }

    [Fact]
    public void SelectGpu_FallsBackToCoreLoadWithoutD3D()
    {
        SensorReading[] readings =
        [
            Gpu(NvidiaId, SensorHardwareKind.DiscreteGpu, SensorKind.Temperature, "GPU Core", 50),
            Gpu(NvidiaId, SensorHardwareKind.DiscreteGpu, SensorKind.Load, "GPU Core", 5),
            Gpu(AmdId, SensorHardwareKind.DiscreteGpu, SensorKind.Temperature, "GPU Core", 50),
            Gpu(AmdId, SensorHardwareKind.DiscreteGpu, SensorKind.Load, "GPU Core", 80),
        ];

        HardwareSensorSelector.SelectGpuHardwareId(readings, null).ShouldBe(AmdId);
    }

    // ---- GPU values ----

    [Fact]
    public void ReadGpu_ReadsCoreTemperatureClockAndVram()
    {
        SensorReading[] readings =
        [
            Gpu(NvidiaId, SensorHardwareKind.DiscreteGpu, SensorKind.Temperature, "GPU Hot Spot", 63),
            Gpu(NvidiaId, SensorHardwareKind.DiscreteGpu, SensorKind.Temperature, "GPU Core", 52),
            Gpu(NvidiaId, SensorHardwareKind.DiscreteGpu, SensorKind.Clock, "GPU Memory", 405),
            Gpu(NvidiaId, SensorHardwareKind.DiscreteGpu, SensorKind.Clock, "GPU Core", 1350),
            Gpu(NvidiaId, SensorHardwareKind.DiscreteGpu, SensorKind.SmallData, "D3D Dedicated Memory Used", 256),
            Gpu(NvidiaId, SensorHardwareKind.DiscreteGpu, SensorKind.SmallData, "GPU Memory Used", 3174.4),
            Gpu(NvidiaId, SensorHardwareKind.DiscreteGpu, SensorKind.SmallData, "GPU Memory Total", 8192),
        ];

        var gpu = HardwareSensorSelector.ReadGpu(readings, NvidiaId)!;

        gpu.Name.ShouldBe("GPU " + NvidiaId);
        gpu.TemperatureCelsius.ShouldBe(52);
        gpu.CoreClockMhz.ShouldBe(1350);
        gpu.VramUsedBytes.ShouldBe((ulong)(3174.4 * 1024 * 1024));
        gpu.VramTotalBytes.ShouldBe(8UL * 1024 * 1024 * 1024);
    }

    [Fact]
    public void ReadGpu_VramFallsBackToD3DDedicated()
    {
        SensorReading[] readings =
        [
            Gpu(AmdId, SensorHardwareKind.DiscreteGpu, SensorKind.SmallData, "GPU Memory Used", null),
            Gpu(AmdId, SensorHardwareKind.DiscreteGpu, SensorKind.SmallData, "D3D Dedicated Memory Used", 1024),
            Gpu(AmdId, SensorHardwareKind.DiscreteGpu, SensorKind.SmallData, "D3D Dedicated Memory Total", 4096),
        ];

        var gpu = HardwareSensorSelector.ReadGpu(readings, AmdId)!;

        gpu.VramUsedBytes.ShouldBe(1024UL * 1024 * 1024);
        gpu.VramTotalBytes.ShouldBe(4096UL * 1024 * 1024);
    }

    [Fact]
    public void ReadGpu_SharedMemoryOnly_HasNoVram()
    {
        SensorReading[] readings =
        [
            Gpu(IntelIgpuId, SensorHardwareKind.IntegratedGpu, SensorKind.SmallData, "D3D Shared Memory Used", 600),
            Gpu(IntelIgpuId, SensorHardwareKind.IntegratedGpu, SensorKind.SmallData, "D3D Shared Memory Total", 16000),
        ];

        var gpu = HardwareSensorSelector.ReadGpu(readings, IntelIgpuId)!;

        gpu.VramUsedBytes.ShouldBeNull();
        gpu.VramTotalBytes.ShouldBeNull();
    }

    [Fact]
    public void ReadGpu_UnknownId_ReturnsNull()
    {
        HardwareSensorSelector.ReadGpu(NvidiaWithLoad(0), "/gpu-amd/9").ShouldBeNull();
    }

    // ---- Fans ----

    [Fact]
    public void SelectFans_HidesStoppedAndUnreadableFans()
    {
        SensorReading[] readings =
        [
            new("/lpc/nct6798d", "Nuvoton", SensorHardwareKind.Motherboard, SensorKind.Fan, "Fan #1", 1240),
            new("/lpc/nct6798d", "Nuvoton", SensorHardwareKind.Motherboard, SensorKind.Fan, "Fan #2", 0),
            new("/lpc/nct6798d", "Nuvoton", SensorHardwareKind.Motherboard, SensorKind.Fan, "Fan #3", null),
            new("/lpc/nct6798d", "Nuvoton", SensorHardwareKind.Motherboard, SensorKind.Fan, "Fan #4", 860),
        ];

        HardwareSensorSelector.SelectFans(readings).ShouldBe([new FanReading("Fan #1", 1240), new FanReading("Fan #4", 860)]);
    }

    [Fact]
    public void SelectFans_PrefixesGpuFansThatDontSayGpu()
    {
        SensorReading[] readings =
        [
            Gpu(NvidiaId, SensorHardwareKind.DiscreteGpu, SensorKind.Fan, "Fan 1", 1500),
            Gpu(AmdId, SensorHardwareKind.DiscreteGpu, SensorKind.Fan, "GPU Fan", 1100),
        ];

        HardwareSensorSelector.SelectFans(readings).Select(f => f.Name).ShouldBe(["GPU Fan 1", "GPU Fan"]);
    }

    [Fact]
    public void Select_CombinesAllAndReturnsChosenGpu()
    {
        var readings = Concat(Cpu(Temp("CPU Package", 70)), NvidiaWithLoad(load: 3));

        var (stats, gpuId) = HardwareSensorSelector.Select(readings, previousGpuHardwareId: null);

        gpuId.ShouldBe(NvidiaId);
        stats.Cpu!.PackageTemperatureCelsius.ShouldBe(70);
        stats.Gpu!.TemperatureCelsius.ShouldBe(45);
        stats.Fans.ShouldBeEmpty();
    }

    // ---- helpers ----

    private static SensorReading[] Cpu(params (SensorKind Kind, string Name, double? Value)[] sensors) =>
        [.. sensors.Select(s => new SensorReading(CpuId, "Intel Core i7", SensorHardwareKind.Cpu, s.Kind, s.Name, s.Value))];

    private static (SensorKind, string, double?) Temp(string name, double? value) => (SensorKind.Temperature, name, value);

    private static (SensorKind, string, double?) Clock(string name, double? value) => (SensorKind.Clock, name, value);

    private static SensorReading Gpu(string id, SensorHardwareKind kind, SensorKind sensorKind, string name, double? value) =>
        new(id, "GPU " + id, kind, sensorKind, name, value);

    private static SensorReading[] NvidiaWithLoad(double load) =>
    [
        Gpu(NvidiaId, SensorHardwareKind.DiscreteGpu, SensorKind.Temperature, "GPU Core", 45),
        Gpu(NvidiaId, SensorHardwareKind.DiscreteGpu, SensorKind.Load, "D3D 3D", load),
    ];

    private static SensorReading[] IntelWithSensors(double load) =>
    [
        Gpu(IntelIgpuId, SensorHardwareKind.IntegratedGpu, SensorKind.Clock, "GPU Core", 1100),
        Gpu(IntelIgpuId, SensorHardwareKind.IntegratedGpu, SensorKind.Load, "D3D 3D", load),
    ];

    private static SensorReading[] Concat(params SensorReading[][] parts) => [.. parts.SelectMany(p => p)];
}
