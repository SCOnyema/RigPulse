using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using RigPulse.Core.Abstractions;
using RigPulse.Core.Models;
using RigPulse.Sensors.Internal;
using RigPulse.Sensors.Interop;

namespace RigPulse.Sensors;

/// <summary>
/// Reads physical memory usage via the Win32 <c>GlobalMemoryStatusEx</c> API.
/// </summary>
internal sealed class MemoryProvider(ILogger<MemoryProvider> logger, TimeProvider timeProvider) : IMemoryProvider
{
    private readonly MetricHealth _health = new("Memory status", logger, timeProvider);

    public MemoryStats? Read()
    {
        var status = NativeMethods.MemoryStatusEx.Create();
        if (!NativeMethods.GlobalMemoryStatusEx(ref status))
        {
            _health.ReportFailure($"GlobalMemoryStatusEx failed: {Marshal.GetLastPInvokeErrorMessage()}");
            return null;
        }

        _health.ReportSuccess();
        return new MemoryStats(UsedBytes: status.TotalPhys - status.AvailPhys, TotalBytes: status.TotalPhys);
    }
}
