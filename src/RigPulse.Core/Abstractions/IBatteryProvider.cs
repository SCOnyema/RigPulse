using RigPulse.Core.Models;

namespace RigPulse.Core.Abstractions;

/// <summary>
/// Provides the system battery's state as <see cref="BatteryStats"/>. Returns <see langword="null"/>
/// on machines without a battery.
/// </summary>
public interface IBatteryProvider : IMetricProvider<BatteryStats>;
