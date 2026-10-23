namespace RigPulse.Core.Models;

/// <summary>
/// Processor temperature and clock speeds.
/// </summary>
/// <param name="PackageTemperatureCelsius">Package temperature (or the nearest equivalent), or <see langword="null"/> if unavailable.</param>
/// <param name="AverageCoreClockMhz">Mean clock across all cores, or <see langword="null"/> if unavailable.</param>
/// <param name="MaxCoreClockMhz">Clock of the fastest core, or <see langword="null"/> if unavailable.</param>
public sealed record CpuSensorStats(double? PackageTemperatureCelsius, double? AverageCoreClockMhz, double? MaxCoreClockMhz);

/// <summary>
/// Temperature, clock and memory of one graphics adapter.
/// </summary>
/// <param name="Name">The adapter's display name.</param>
/// <param name="TemperatureCelsius">Core temperature, or <see langword="null"/> if unavailable.</param>
/// <param name="CoreClockMhz">Core clock, or <see langword="null"/> if unavailable.</param>
/// <param name="VramUsedBytes">Dedicated video memory in use, or <see langword="null"/> if unavailable.</param>
/// <param name="VramTotalBytes">Total dedicated video memory, or <see langword="null"/> if unavailable.</param>
public sealed record GpuSensorStats(string Name, double? TemperatureCelsius, double? CoreClockMhz, ulong? VramUsedBytes, ulong? VramTotalBytes);

/// <summary>
/// A spinning fan.
/// </summary>
/// <param name="Name">A short display name, e.g. "Fan #1" or "GPU Fan".</param>
/// <param name="Rpm">Speed in revolutions per minute. Always greater than 0.</param>
public sealed record FanReading(string Name, double Rpm);

/// <summary>
/// Everything RigPulse reads from hardware sensors in one poll.
/// </summary>
/// <param name="Cpu">Processor sensors, or <see langword="null"/> if no processor was found.</param>
/// <param name="Gpu">The selected GPU's sensors, or <see langword="null"/> if no GPU reports any.</param>
/// <param name="Fans">Fans that are currently spinning. Empty when none report a speed.</param>
public sealed record HardwareSensorStats(CpuSensorStats? Cpu, GpuSensorStats? Gpu, IReadOnlyList<FanReading> Fans);
