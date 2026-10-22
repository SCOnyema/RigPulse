using RigPulse.Core.Models;

namespace RigPulse.Core.Abstractions;

/// <summary>
/// Provides aggregate disk throughput as <see cref="DiskStats"/>.
/// </summary>
public interface IDiskProvider : IMetricProvider<DiskStats>;
