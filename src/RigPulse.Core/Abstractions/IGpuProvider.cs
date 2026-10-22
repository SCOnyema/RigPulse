using RigPulse.Core.Models;

namespace RigPulse.Core.Abstractions;

/// <summary>
/// Provides GPU 3D engine load as <see cref="GpuStats"/>.
/// </summary>
public interface IGpuProvider : IMetricProvider<GpuStats>;
