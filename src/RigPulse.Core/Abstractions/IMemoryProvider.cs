using RigPulse.Core.Models;

namespace RigPulse.Core.Abstractions;

/// <summary>
/// Provides physical memory usage as <see cref="MemoryStats"/>.
/// </summary>
public interface IMemoryProvider : IMetricProvider<MemoryStats>;
