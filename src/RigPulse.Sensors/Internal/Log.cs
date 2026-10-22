using Microsoft.Extensions.Logging;

namespace RigPulse.Sensors.Internal;

/// <summary>
/// Source-generated log messages shared by all providers.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Warning,
        Message = "{Metric} is unavailable ({Reason}). It will show as '–' and be retried periodically.")]
    public static partial void MetricUnavailable(this ILogger logger, string metric, string reason, Exception? exception);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Debug, Message = "{Metric} is still unavailable ({Reason}).")]
    public static partial void MetricStillUnavailable(this ILogger logger, string metric, string reason);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Information, Message = "{Metric} is available again.")]
    public static partial void MetricRecovered(this ILogger logger, string metric);
}
