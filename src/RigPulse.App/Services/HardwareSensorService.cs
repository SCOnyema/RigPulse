using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RigPulse.Core.Abstractions;

namespace RigPulse.App.Services;

/// <summary>
/// Background loop that reads hardware sensors and the battery on the slower
/// <see cref="MonitorOptions.SensorPollInterval"/>, while <see cref="HardwareSensorFeed.IsRequested"/>
/// is set, and stores the result in the <see cref="HardwareSensorFeed"/>.
/// </summary>
/// <remarks>
/// It runs separately from <see cref="MonitorService"/> because one hardware read takes roughly
/// 20–250 ms (mostly waiting on GPU driver calls), and the first one opens the library, which can
/// take seconds. Neither delay may hold up the 1 s metrics.
/// </remarks>
internal sealed partial class HardwareSensorService(
    IHardwareSensorProvider sensorProvider,
    IBatteryProvider batteryProvider,
    HardwareSensorFeed feed,
    IOptions<MonitorOptions> options,
    TimeProvider timeProvider,
    ILogger<HardwareSensorService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = options.Value.SensorPollInterval;
        LogStarting(interval);

        using var timer = new PeriodicTimer(interval, timeProvider);
        try
        {
            do
            {
                Poll();
            }
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }

        LogStopped();
    }

    private void Poll()
    {
        if (!feed.IsRequested)
        {
            feed.Publish(null);
            return;
        }

        var start = timeProvider.GetTimestamp();
        var sample = new HardwareSample(Read(sensorProvider), Read(batteryProvider));
        var elapsed = timeProvider.GetElapsedTime(start);
        LogRead(elapsed.TotalMilliseconds);

        // Drop the read if the details were closed while it ran.
        feed.Publish(feed.IsRequested ? sample : null);
    }

    /// <summary>
    /// Providers are designed not to throw, but a faulty one must never stop the loop.
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Hardware sensor polling started, every {Interval} while details are shown.")]
    private partial void LogStarting(TimeSpan interval);

    [LoggerMessage(Level = LogLevel.Information, Message = "Hardware sensor polling stopped.")]
    private partial void LogStopped();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Hardware sensors read in {ElapsedMs:0.0} ms.")]
    private partial void LogRead(double elapsedMs);

    [LoggerMessage(Level = LogLevel.Error, Message = "Provider {Provider} threw unexpectedly; reporting it as unavailable.")]
    private partial void LogProviderFailed(string provider, Exception exception);
}
