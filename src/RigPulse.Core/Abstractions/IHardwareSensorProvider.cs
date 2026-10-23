using RigPulse.Core.Models;

namespace RigPulse.Core.Abstractions;

/// <summary>
/// Provides temperatures, clocks, VRAM and fan speeds as <see cref="HardwareSensorStats"/>.
/// </summary>
/// <remarks>
/// Hardware sensors are much slower to read than the other metrics, so poll this provider on its
/// own, slower interval and off the UI thread.
/// </remarks>
public interface IHardwareSensorProvider : IMetricProvider<HardwareSensorStats>;
