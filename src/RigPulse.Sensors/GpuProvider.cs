using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RigPulse.Core.Abstractions;
using RigPulse.Core.Models;
using RigPulse.Sensors.Internal;

namespace RigPulse.Sensors;

/// <summary>
/// Reads GPU load the way Task Manager does: by summing the <c>GPU Engine\Utilization Percentage</c>
/// counters of every 3D engine instance.
/// </summary>
/// <remarks>
/// <para>
/// The category has one instance per process per engine (for example
/// <c>pid_1234_luid_0x00000000_0x0000D1B5_phys_0_eng_0_engtype_3D</c>), and instances come and go
/// as processes start and stop. Rather than managing hundreds of <see cref="PerformanceCounter"/>
/// objects, each poll reads the whole category in one call and computes each instance's value from
/// its raw sample and the previous one. New instances get a value from their second poll onwards;
/// vanished instances simply drop out.
/// </para>
/// <para>
/// Loads are summed per adapter (the <c>luid</c> part of the name) and the busiest adapter is
/// reported, so a laptop's integrated and discrete GPUs are not added together. The result is
/// clamped to 100.
/// </para>
/// </remarks>
internal sealed class GpuProvider(ILogger<GpuProvider> logger, TimeProvider timeProvider) : IGpuProvider
{
    private const string CategoryName = "GPU Engine";
    private const string CounterName = "Utilization Percentage";
    private const string EngineTypeSuffix = "engtype_3D";

    private readonly MetricHealth _health = new($"Counter '{CategoryName}(*{EngineTypeSuffix}) / {CounterName}'", logger, timeProvider);
    private readonly Dictionary<string, double> _loadByAdapter = new(StringComparer.Ordinal);
    private Dictionary<string, CounterSample> _previousSamples = new(StringComparer.Ordinal);
    private Dictionary<string, CounterSample> _currentSamples = new(StringComparer.Ordinal);
    private PerformanceCounterCategory? _category;

    public GpuStats? Read()
    {
        if (!_health.ShouldAttempt)
        {
            return null;
        }

        try
        {
            _category ??= OpenCategory();
            var utilization = _category.ReadCategory()[CounterName]
                ?? throw new InvalidOperationException($"Counter '{CounterName}' was not found.");

            var load = CalculateLoad(utilization);
            if (load is null)
            {
                _health.ReportFailure("no 3D engine instances found");
                return null;
            }

            _health.ReportSuccess();
            return new GpuStats(load.Value);
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            _category = null;
            _previousSamples.Clear();
            _health.ReportFailure(ex.Message, ex);
            return null;
        }
    }

    private static PerformanceCounterCategory OpenCategory() =>
        PerformanceCounterCategory.Exists(CategoryName)
            ? new PerformanceCounterCategory(CategoryName)
            : throw new InvalidOperationException($"Category '{CategoryName}' does not exist (the GPU driver may not publish WDDM 2.0+ counters).");

    /// <returns>The busiest adapter's 3D load in percent, or <see langword="null"/> if there are no 3D engines.</returns>
    private double? CalculateLoad(InstanceDataCollection utilization)
    {
        _currentSamples.Clear();
        _loadByAdapter.Clear();

        foreach (InstanceData instance in utilization.Values)
        {
            var name = instance.InstanceName;
            if (!name.EndsWith(EngineTypeSuffix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var sample = instance.Sample;
            _currentSamples[name] = sample;

            if (!_previousSamples.TryGetValue(name, out var previous))
            {
                continue; // first time we see this instance: no baseline yet
            }

            double value = CounterSample.Calculate(previous, sample);
            if (double.IsFinite(value) && value > 0)
            {
                var adapter = GetAdapterKey(name);
                _loadByAdapter[adapter] = _loadByAdapter.GetValueOrDefault(adapter) + value;
            }
        }

        (_previousSamples, _currentSamples) = (_currentSamples, _previousSamples);

        if (_previousSamples.Count == 0)
        {
            return null;
        }

        var busiest = 0d;
        foreach (var adapterLoad in _loadByAdapter.Values)
        {
            busiest = Math.Max(busiest, adapterLoad);
        }

        return Math.Clamp(busiest, 0, 100);
    }

    /// <summary>
    /// Extracts the adapter id, e.g. <c>luid_0x00000000_0x0000D1B5</c>, from an instance name.
    /// </summary>
    private static string GetAdapterKey(string instanceName)
    {
        var start = instanceName.IndexOf("luid_", StringComparison.Ordinal);
        if (start < 0)
        {
            return string.Empty;
        }

        var end = instanceName.IndexOf("_phys", start, StringComparison.Ordinal);
        return end < 0 ? instanceName[start..] : instanceName[start..end];
    }
}
