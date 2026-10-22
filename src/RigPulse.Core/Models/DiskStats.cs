namespace RigPulse.Core.Models;

/// <summary>
/// Aggregate disk throughput across all physical disks.
/// </summary>
/// <param name="ReadBytesPerSecond">Bytes read per second.</param>
/// <param name="WriteBytesPerSecond">Bytes written per second.</param>
public sealed record DiskStats(double ReadBytesPerSecond, double WriteBytesPerSecond);
