using Microsoft.Extensions.Logging;
using RigPulse.Core.Abstractions;
using RigPulse.Core.Models;
using RigPulse.Sensors.Internal;

namespace RigPulse.Sensors;

/// <summary>
/// Reads aggregate disk throughput from the <c>PhysicalDisk(_Total)</c> performance counters.
/// </summary>
internal sealed class DiskProvider : IDiskProvider, IDisposable
{
    private const string Category = "PhysicalDisk";
    private const string Instance = "_Total";

    private readonly SafePerformanceCounter _readBytes;
    private readonly SafePerformanceCounter _writeBytes;

    public DiskProvider(ILogger<DiskProvider> logger, TimeProvider timeProvider)
    {
        _readBytes = new SafePerformanceCounter(Category, "Disk Read Bytes/sec", Instance, logger, timeProvider);
        _writeBytes = new SafePerformanceCounter(Category, "Disk Write Bytes/sec", Instance, logger, timeProvider);
    }

    public DiskStats? Read()
    {
        // Read both every tick so each counter keeps its own baseline up to date.
        var read = _readBytes.NextValue();
        var write = _writeBytes.NextValue();

        return read is { } r && write is { } w
            ? new DiskStats(Math.Max(0, r), Math.Max(0, w))
            : null;
    }

    public void Dispose()
    {
        _readBytes.Dispose();
        _writeBytes.Dispose();
    }
}
