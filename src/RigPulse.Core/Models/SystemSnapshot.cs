namespace RigPulse.Core.Models;

/// <summary>
/// One immutable sample of every metric RigPulse tracks.
/// </summary>
/// <remarks>
/// A <see langword="null"/> metric means it could not be read (for example, a missing performance
/// counter, or a sensor that needs administrator rights). Consumers should render that as
/// "unavailable" rather than zero. Hardware sensors and the battery are polled on their own slower
/// interval, so their values can be up to one sensor interval older than <see cref="Timestamp"/>.
/// </remarks>
/// <param name="Timestamp">When the sample was taken.</param>
/// <param name="Cpu">Processor load, or <see langword="null"/> if unavailable.</param>
/// <param name="Memory">Physical memory usage, or <see langword="null"/> if unavailable.</param>
/// <param name="Gpu">GPU load, or <see langword="null"/> if unavailable.</param>
/// <param name="Disk">Disk throughput, or <see langword="null"/> if unavailable.</param>
/// <param name="Network">Network throughput, or <see langword="null"/> if unavailable.</param>
/// <param name="Sensors">Temperatures, clocks, VRAM and fans, or <see langword="null"/> if they are not being read or are unavailable.</param>
/// <param name="Battery">Battery state, or <see langword="null"/> if it is not being read or the machine has no battery.</param>
public sealed record SystemSnapshot(
    DateTimeOffset Timestamp,
    CpuStats? Cpu,
    MemoryStats? Memory,
    GpuStats? Gpu,
    DiskStats? Disk,
    NetworkStats? Network,
    HardwareSensorStats? Sensors = null,
    BatteryStats? Battery = null);
