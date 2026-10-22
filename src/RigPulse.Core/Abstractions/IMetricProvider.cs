namespace RigPulse.Core.Abstractions;

/// <summary>
/// Reads one kind of system metric on demand.
/// </summary>
/// <typeparam name="TStats">The metric model this provider produces.</typeparam>
/// <remarks>
/// Implementations must never throw from <see cref="Read"/>: when the metric cannot be read they
/// log the problem and return <see langword="null"/>. Rate-based providers (CPU, disk, network)
/// compute values relative to the previous call, so the very first read may be 0 and callers
/// should take one warm-up sample. Implementations are designed for a single polling caller and
/// are not required to be thread-safe.
/// </remarks>
public interface IMetricProvider<out TStats>
    where TStats : class
{
    /// <summary>
    /// Takes a sample of the metric.
    /// </summary>
    /// <returns>The current value, or <see langword="null"/> if the metric is unavailable.</returns>
    TStats? Read();
}
