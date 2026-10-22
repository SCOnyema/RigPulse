using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RigPulse.Core.Abstractions;
using RigPulse.Core.Collections;
using RigPulse.Core.Models;

namespace RigPulse.App.Services;

/// <summary>
/// Background loop that samples every provider on a fixed interval, records the snapshot in the
/// history buffer and publishes it to the UI.
/// </summary>
internal sealed partial class MonitorService(
    ICpuProvider cpuProvider,
    IMemoryProvider memoryProvider,
    IGpuProvider gpuProvider,
    IDiskProvider diskProvider,
    INetworkProvider networkProvider,
    ISnapshotPublisher publisher,
    RingBuffer<SystemSnapshot> history,
    IOptions<MonitorOptions> options,
    TimeProvider timeProvider,
    ILogger<MonitorService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = options.Value.PollInterval;
        LogStarting(interval);

        // Rate-based providers report change since the previous read, and performance counters
        // are created lazily on first use. Take one warm-up sample (off the UI thread) and discard it.
        _ = CollectSnapshot();

        using var timer = new PeriodicTimer(interval, timeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                var snapshot = CollectSnapshot();
                history.Add(snapshot);
                publisher.Publish(snapshot);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }

        LogStopped();
    }

    private SystemSnapshot CollectSnapshot() => new(
        timeProvider.GetUtcNow(),
        Read(cpuProvider),
        Read(memoryProvider),
        Read(gpuProvider),
        Read(diskProvider),
        Read(networkProvider));

    /// <summary>
    /// Providers are designed not to throw, but one faulty provider must never stop the loop.
    /// </summary>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Last-resort isolation of a single provider.")]
    private TStats? Read<TStats>(IMetricProvider<TStats> provider)
        where TStats : class
    {
        try
        {
            return provider.Read();
        }
        catch (Exception ex)
        {
            LogProviderFailed(provider.GetType().Name, ex);
            return null;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Monitoring started, polling every {Interval}.")]
    private partial void LogStarting(TimeSpan interval);

    [LoggerMessage(Level = LogLevel.Information, Message = "Monitoring stopped.")]
    private partial void LogStopped();

    [LoggerMessage(Level = LogLevel.Error, Message = "Provider {Provider} threw unexpectedly; reporting it as unavailable.")]
    private partial void LogProviderFailed(string provider, Exception exception);
}
