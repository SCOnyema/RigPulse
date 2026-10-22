using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace RigPulse.Sensors.Internal;

/// <summary>
/// A <see cref="PerformanceCounter"/> wrapper that is created lazily on the polling thread,
/// primed on creation, and never throws: failures are logged and reported as <see langword="null"/>.
/// </summary>
internal sealed class SafePerformanceCounter : IDisposable
{
    private readonly string _categoryName;
    private readonly string _counterName;
    private readonly string _instanceName;
    private readonly MetricHealth _health;
    private PerformanceCounter? _counter;
    private bool _disposed;

    public SafePerformanceCounter(string categoryName, string counterName, string instanceName, ILogger logger, TimeProvider timeProvider)
    {
        _categoryName = categoryName;
        _counterName = counterName;
        _instanceName = instanceName;
        _health = new MetricHealth($@"Counter '\{categoryName}({instanceName})\{counterName}'", logger, timeProvider);
    }

    /// <summary>
    /// Reads the next calculated value. The first successful read after creation returns the
    /// priming sample (0 for rate counters).
    /// </summary>
    /// <returns>The counter value, or <see langword="null"/> if the counter is unavailable.</returns>
    public float? NextValue()
    {
        if (_disposed || (_counter is null && !TryCreate()))
        {
            return null;
        }

        try
        {
            var value = _counter!.NextValue();
            _health.ReportSuccess();
            return value;
        }
        catch (Exception ex) when (IsCounterException(ex))
        {
            ResetCounter();
            _health.ReportFailure(ex.Message, ex);
            return null;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        ResetCounter();
    }

    private bool TryCreate()
    {
        if (!_health.ShouldAttempt)
        {
            return false;
        }

        try
        {
            // Creating a counter loads the category's metadata, which can take a few hundred ms.
            // Doing it here keeps that cost on the polling thread instead of app startup.
            _counter = new PerformanceCounter(_categoryName, _counterName, _instanceName, readOnly: true);
            return true;
        }
        catch (Exception ex) when (IsCounterException(ex))
        {
            ResetCounter();
            _health.ReportFailure(ex.Message, ex);
            return false;
        }
    }

    private void ResetCounter()
    {
        _counter?.Dispose();
        _counter = null;
    }

    private static bool IsCounterException(Exception ex) =>
        ex is InvalidOperationException or Win32Exception or UnauthorizedAccessException or PlatformNotSupportedException;
}
