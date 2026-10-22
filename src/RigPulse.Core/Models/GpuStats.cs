namespace RigPulse.Core.Models;

/// <summary>
/// Graphics processor load at a point in time.
/// </summary>
/// <param name="UsagePercent">3D engine utilisation of the busiest GPU, from 0 to 100.</param>
public sealed record GpuStats(double UsagePercent);
