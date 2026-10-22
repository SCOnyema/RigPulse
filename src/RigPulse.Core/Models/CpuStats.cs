namespace RigPulse.Core.Models;

/// <summary>
/// Overall processor load at a point in time.
/// </summary>
/// <param name="UsagePercent">Total CPU utilisation across all logical processors, from 0 to 100.</param>
public sealed record CpuStats(double UsagePercent);
