using Microsoft.Extensions.Logging;

namespace RigPulse.Sensors.Internal;

/// <summary>
/// Tracks whether a metric source is working, logs state changes once instead of every poll,
/// and throttles retries of expensive setup work after a failure.
/// </summary>
internal sealed class MetricHealth(string metricName, ILogger logger, TimeProvider timeProvider)
{
    /// <summary>How long to wait after a failure before trying expensive setup again.</summary>
    public static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(30);

    private bool _isFaulted;
    private long _failedAt;

    /// <summary>
    /// Gets whether enough time has passed since the last failure to try again.
    /// </summary>
    public bool ShouldAttempt => !_isFaulted || timeProvider.GetElapsedTime(_failedAt) >= RetryDelay;

    public void ReportFailure(string reason, Exception? exception = null)
    {
        if (_isFaulted)
        {
            logger.MetricStillUnavailable(metricName, reason);
        }
        else
        {
            logger.MetricUnavailable(metricName, reason, exception);
        }

        _isFaulted = true;
        _failedAt = timeProvider.GetTimestamp();
    }

    public void ReportSuccess()
    {
        if (_isFaulted)
        {
            _isFaulted = false;
            logger.MetricRecovered(metricName);
        }
    }
}
