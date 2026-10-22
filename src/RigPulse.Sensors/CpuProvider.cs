using Microsoft.Extensions.Logging;
using RigPulse.Core.Abstractions;
using RigPulse.Core.Models;
using RigPulse.Sensors.Internal;

namespace RigPulse.Sensors;

/// <summary>
/// Reads total CPU load from Windows performance counters.
/// </summary>
/// <remarks>
/// Prefers <c>Processor Information\% Processor Utility</c>, which accounts for frequency scaling
/// and matches Task Manager. Falls back to the classic <c>Processor\% Processor Time</c> if the
/// former is unavailable. Utility can exceed 100% under turbo boost, so values are clamped.
/// </remarks>
internal sealed class CpuProvider : ICpuProvider, IDisposable
{
    private readonly SafePerformanceCounter _processorUtility;
    private readonly SafePerformanceCounter _processorTime;

    public CpuProvider(ILogger<CpuProvider> logger, TimeProvider timeProvider)
    {
        _processorUtility = new SafePerformanceCounter("Processor Information", "% Processor Utility", "_Total", logger, timeProvider);
        _processorTime = new SafePerformanceCounter("Processor", "% Processor Time", "_Total", logger, timeProvider);
    }

    public CpuStats? Read()
    {
        // The fallback counter is only created if the preferred one fails.
        var value = _processorUtility.NextValue() ?? _processorTime.NextValue();
        return value is { } percent ? new CpuStats(Math.Clamp(percent, 0, 100)) : null;
    }

    public void Dispose()
    {
        _processorUtility.Dispose();
        _processorTime.Dispose();
    }
}
