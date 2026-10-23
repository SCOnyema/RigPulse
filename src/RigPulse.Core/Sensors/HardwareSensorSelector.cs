using System.Text.RegularExpressions;
using RigPulse.Core.Models;

namespace RigPulse.Core.Sensors;

/// <summary>
/// Turns the raw sensor list from an <see cref="Abstractions.ISensorSource"/> into the handful of
/// values the widget shows. Sensor names differ by vendor and generation, so each value is found
/// through an ordered fallback chain.
/// </summary>
public static partial class HardwareSensorSelector
{
    /// <summary>
    /// How many percentage points of 3D load one GPU must lead by to be picked as "the busiest".
    /// Below that, the choice is treated as ambiguous and the discrete GPU wins. The same margin
    /// keeps the current choice until another GPU is clearly busier, so the row doesn't flicker.
    /// </summary>
    public const double GpuSwitchMargin = 10;

    /// <summary>Megabytes (as reported by the sensor library) to bytes.</summary>
    private const double BytesPerMegabyte = 1024d * 1024d;

    /// <summary>Readings above this are treated as garbage (e.g. an unplugged probe reading 255 °C).</summary>
    private const double MaxPlausibleCelsius = 150;

    /// <summary>
    /// Processor temperature sensors, most preferred first. The first name with a valid reading
    /// wins. Intel reports "CPU Package". AMD Zen reports "Core (Tctl/Tdie)", or separate Tdie and
    /// Tctl values, where Tctl may include a fan-curve offset, so Tdie is preferred. Older AMD
    /// reports "CPU Cores". Core max and average are last-resort fallbacks.
    /// </summary>
    public static IReadOnlyList<string> CpuTemperatureNames { get; } =
    [
        "CPU Package",
        "Core (Tctl/Tdie)",
        "Tctl/Tdie",
        "Core (Tdie)",
        "Tdie",
        "Core (Tctl)",
        "Tctl",
        "CPU Cores",
        "Core Max",
        "Core Average",
    ];

    /// <summary>
    /// Pairs of (used, total) GPU memory sensors, most preferred first. Vendor APIs report
    /// "GPU Memory *". Windows' D3D statistics report "D3D Dedicated Memory *". Shared memory is
    /// deliberately ignored: it is borrowed system RAM, not VRAM.
    /// </summary>
    public static IReadOnlyList<(string Used, string Total)> GpuMemoryNames { get; } =
    [
        ("GPU Memory Used", "GPU Memory Total"),
        ("D3D Dedicated Memory Used", "D3D Dedicated Memory Total"),
    ];

    /// <summary>GPU core temperature sensor name (NVIDIA, AMD and Intel all use it).</summary>
    public const string GpuCoreSensorName = "GPU Core";

    /// <summary>GPU load sensors used to find the busiest adapter, most preferred first.</summary>
    public static IReadOnlyList<string> GpuLoadNames { get; } = ["D3D 3D", "GPU Core"];

    /// <summary>
    /// Picks the processor's package temperature and its average and maximum core clocks.
    /// </summary>
    /// <param name="readings">All sensor readings.</param>
    /// <returns>The processor stats, or <see langword="null"/> if there are no processor sensors at all.</returns>
    public static CpuSensorStats? SelectCpu(IReadOnlyList<SensorReading> readings)
    {
        ArgumentNullException.ThrowIfNull(readings);

        var cpu = readings.Where(r => r.HardwareKind == SensorHardwareKind.Cpu).ToList();
        if (cpu.Count == 0)
        {
            return null;
        }

        double? temperature = null;
        foreach (var name in CpuTemperatureNames)
        {
            // Max across matches, so a dual-socket machine reports its hotter package.
            temperature = MaxValid(cpu, SensorKind.Temperature, name, IsPlausibleTemperature);
            if (temperature is not null)
            {
                break;
            }
        }

        var clocks = cpu
            .Where(r => r.Kind == SensorKind.Clock && CoreClockName().IsMatch(r.Name) && IsPositive(r.Value))
            .Select(r => r.Value!.Value)
            .ToList();

        return new CpuSensorStats(
            temperature,
            clocks.Count > 0 ? clocks.Average() : null,
            clocks.Count > 0 ? clocks.Max() : null);
    }

    /// <summary>
    /// Chooses which GPU to show.
    /// </summary>
    /// <remarks>
    /// <para>Only GPUs that report a core temperature, core clock or VRAM are considered. An
    /// integrated GPU without sensors would only ever show "–".</para>
    /// <para>The busiest GPU by 3D load is chosen, which matches the adapter behind the GPU % row.
    /// It must lead the others by <see cref="GpuSwitchMargin"/>. Otherwise the choice is ambiguous
    /// (typically everything is idle) and the discrete GPU is chosen. The current choice is kept
    /// until another GPU is clearly busier, so the row doesn't flip back and forth.</para>
    /// </remarks>
    /// <param name="readings">All sensor readings.</param>
    /// <param name="previousHardwareId">The GPU chosen last time, or <see langword="null"/>.</param>
    /// <returns>The chosen GPU's <see cref="SensorReading.HardwareId"/>, or <see langword="null"/> if no GPU reports sensors.</returns>
    public static string? SelectGpuHardwareId(IReadOnlyList<SensorReading> readings, string? previousHardwareId)
    {
        ArgumentNullException.ThrowIfNull(readings);

        var candidates = readings
            .Where(r => r.HardwareKind is SensorHardwareKind.DiscreteGpu or SensorHardwareKind.IntegratedGpu)
            .GroupBy(r => r.HardwareId, StringComparer.Ordinal)
            .Select(g => new GpuCandidate(g.Key, g.First().HardwareKind == SensorHardwareKind.DiscreteGpu, GetLoad(g), ReadGpu(g.ToList(), g.Key)))
            .Where(c => c.Stats is { } s && (s.TemperatureCelsius is not null || s.CoreClockMhz is not null || s.VramTotalBytes is not null))
            .OrderByDescending(c => c.Load) // stable: ties keep library order
            .ToList();

        if (candidates.Count == 0)
        {
            return null;
        }

        var busiest = candidates[0];
        var previous = candidates.Find(c => c.HardwareId == previousHardwareId);
        if (previous is not null && busiest.Load - previous.Load < GpuSwitchMargin)
        {
            return previous.HardwareId;
        }

        if (candidates.Count == 1 || busiest.Load - candidates[1].Load >= GpuSwitchMargin)
        {
            return busiest.HardwareId;
        }

        return (candidates.Find(c => c.IsDiscrete) ?? busiest).HardwareId;
    }

    /// <summary>
    /// Reads one GPU's core temperature, core clock and dedicated memory.
    /// </summary>
    /// <param name="readings">All sensor readings (readings of other devices are ignored).</param>
    /// <param name="hardwareId">The GPU to read.</param>
    /// <returns>The GPU's stats, or <see langword="null"/> if it has no readings.</returns>
    public static GpuSensorStats? ReadGpu(IReadOnlyList<SensorReading> readings, string hardwareId)
    {
        ArgumentNullException.ThrowIfNull(readings);

        var gpu = readings.Where(r => r.HardwareId == hardwareId).ToList();
        if (gpu.Count == 0)
        {
            return null;
        }

        ulong? vramUsed = null;
        ulong? vramTotal = null;
        foreach (var (usedName, totalName) in GpuMemoryNames)
        {
            var used = FirstValid(gpu, SensorKind.SmallData, usedName, v => v >= 0);
            var total = FirstValid(gpu, SensorKind.SmallData, totalName, v => v > 0);
            if (used is { } u && total is { } t)
            {
                vramUsed = (ulong)(Math.Min(u, t) * BytesPerMegabyte);
                vramTotal = (ulong)(t * BytesPerMegabyte);
                break;
            }
        }

        return new GpuSensorStats(
            gpu[0].HardwareName,
            FirstValid(gpu, SensorKind.Temperature, GpuCoreSensorName, IsPlausibleTemperature),
            FirstValid(gpu, SensorKind.Clock, GpuCoreSensorName, v => v > 0),
            vramUsed,
            vramTotal);
    }

    /// <summary>
    /// Lists every fan that is spinning. Fans reading 0 or nothing are left out: they are usually
    /// unconnected headers or fans stopped at idle.
    /// </summary>
    /// <param name="readings">All sensor readings.</param>
    /// <returns>The spinning fans, in the order the library reports them.</returns>
    public static IReadOnlyList<FanReading> SelectFans(IReadOnlyList<SensorReading> readings)
    {
        ArgumentNullException.ThrowIfNull(readings);

        return readings
            .Where(r => r.Kind == SensorKind.Fan && IsPositive(r.Value))
            .Select(r => new FanReading(FanDisplayName(r), r.Value!.Value))
            .ToList();
    }

    /// <summary>
    /// Runs every selection over one set of readings.
    /// </summary>
    /// <param name="readings">All sensor readings.</param>
    /// <param name="previousGpuHardwareId">The GPU chosen last time, or <see langword="null"/>.</param>
    /// <returns>The stats, and the GPU chosen this time (pass it back in on the next call).</returns>
    public static (HardwareSensorStats Stats, string? GpuHardwareId) Select(IReadOnlyList<SensorReading> readings, string? previousGpuHardwareId)
    {
        var gpuId = SelectGpuHardwareId(readings, previousGpuHardwareId);
        var stats = new HardwareSensorStats(
            SelectCpu(readings),
            gpuId is null ? null : ReadGpu(readings, gpuId),
            SelectFans(readings));
        return (stats, gpuId);
    }

    private static string FanDisplayName(SensorReading reading) =>
        reading.HardwareKind is SensorHardwareKind.DiscreteGpu or SensorHardwareKind.IntegratedGpu
        && !reading.Name.Contains("GPU", StringComparison.OrdinalIgnoreCase)
            ? $"GPU {reading.Name}"
            : reading.Name;

    private static double GetLoad(IEnumerable<SensorReading> gpu)
    {
        foreach (var name in GpuLoadNames)
        {
            if (FirstValid(gpu, SensorKind.Load, name, v => v >= 0) is { } load)
            {
                return load;
            }
        }

        return 0;
    }

    private static double? FirstValid(IEnumerable<SensorReading> readings, SensorKind kind, string name, Func<double, bool> isValid)
    {
        foreach (var r in readings)
        {
            if (r.Kind == kind && r.Value is { } v && double.IsFinite(v) && isValid(v)
                && string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return v;
            }
        }

        return null;
    }

    private static double? MaxValid(IEnumerable<SensorReading> readings, SensorKind kind, string name, Func<double, bool> isValid)
    {
        double? max = null;
        foreach (var r in readings)
        {
            if (r.Kind == kind && r.Value is { } v && double.IsFinite(v) && isValid(v)
                && string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                max = max is { } m ? Math.Max(m, v) : v;
            }
        }

        return max;
    }

    private static bool IsPlausibleTemperature(double celsius) => celsius is > 0 and < MaxPlausibleCelsius;

    private static bool IsPositive(double? value) => value is { } v && double.IsFinite(v) && v > 0;

    /// <summary>
    /// Per-core clock sensors: Intel "CPU Core #1", hybrid "P-Core #1" / "E-Core #3", AMD "Core #1".
    /// Excludes "Bus Speed", AMD "Core #1 (Effective)" and aggregate sensors such as "Cores (Average)".
    /// </summary>
    [GeneratedRegex(@"Core #\d+$", RegexOptions.CultureInvariant)]
    private static partial Regex CoreClockName();

    private sealed record GpuCandidate(string HardwareId, bool IsDiscrete, double Load, GpuSensorStats? Stats);
}
