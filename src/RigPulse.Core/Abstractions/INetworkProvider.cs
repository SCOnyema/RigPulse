using RigPulse.Core.Models;

namespace RigPulse.Core.Abstractions;

/// <summary>
/// Provides aggregate network throughput as <see cref="NetworkStats"/>.
/// </summary>
public interface INetworkProvider : IMetricProvider<NetworkStats>;
