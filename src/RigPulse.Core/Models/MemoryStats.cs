namespace RigPulse.Core.Models;

/// <summary>
/// Physical memory usage at a point in time.
/// </summary>
/// <param name="UsedBytes">Physical memory currently in use, in bytes.</param>
/// <param name="TotalBytes">Total installed physical memory visible to the OS, in bytes.</param>
public sealed record MemoryStats(ulong UsedBytes, ulong TotalBytes)
{
    /// <summary>
    /// Gets the used share of physical memory, from 0 to 100. Returns 0 when <see cref="TotalBytes"/> is 0.
    /// </summary>
    public double UsagePercent => TotalBytes == 0 ? 0 : Math.Clamp(100.0 * UsedBytes / TotalBytes, 0, 100);
}
