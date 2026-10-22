namespace RigPulse.Core.Models;

/// <summary>
/// Aggregate network throughput across all active, non-loopback interfaces.
/// </summary>
/// <param name="UploadBytesPerSecond">Bytes sent per second.</param>
/// <param name="DownloadBytesPerSecond">Bytes received per second.</param>
public sealed record NetworkStats(double UploadBytesPerSecond, double DownloadBytesPerSecond);
