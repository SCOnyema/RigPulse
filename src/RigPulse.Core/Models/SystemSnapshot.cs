namespace RigPulse.Core.Models;

/// <summary>
/// One immutable sample of every metric RigPulse tracks.
/// </summary>
/// <remarks>
/// A <see langword="null"/> metric means its provider could not read it (for example, a missing
/// performance counter). Consumers should render that as "unavailable" rather than zero.
/// </remarks>
/// <param name="Timestamp">When the sample was taken.</param>
/// <param name="Cpu">Processor load, or <see langword="null"/> if unavailable.</param>
/// <param name="Memory">Physical memory usage, or <see langword="null"/> if unavailable.</param>
/// <param name="Gpu">GPU load, or <see langword="null"/> if unavailable.</param>
/// <param name="Disk">Disk throughput, or <see langword="null"/> if unavailable.</param>
/// <param name="Network">Network throughput, or <see langword="null"/> if unavailable.</param>
public sealed record SystemSnapshot(
    DateTimeOffset Timestamp,
    CpuStats? Cpu,
    MemoryStats? Memory,
    GpuStats? Gpu,
    DiskStats? Disk,
    NetworkStats? Network);
