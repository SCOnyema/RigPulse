using RigPulse.Core.Models;

namespace RigPulse.Core.Abstractions;

/// <summary>
/// Provides total processor load as <see cref="CpuStats"/>.
/// </summary>
public interface ICpuProvider : IMetricProvider<CpuStats>;
