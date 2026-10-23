using RigPulse.Core.Abstractions;
using RigPulse.Core.Models;
using RigPulse.Core.Sensors;

namespace RigPulse.Sensors;

/// <summary>
/// Reads all hardware sensors from an <see cref="ISensorSource"/> and picks the ones the widget
/// shows, using <see cref="HardwareSensorSelector"/>.
/// </summary>
/// <remarks>
/// Remembers the chosen GPU between reads, so the selection only switches adapters when another
/// one is clearly busier.
/// </remarks>
internal sealed class HardwareSensorProvider(ISensorSource source) : IHardwareSensorProvider
{
    private string? _gpuHardwareId;

    public HardwareSensorStats? Read()
    {
        var readings = source.ReadAll();
        if (readings.Count == 0)
        {
            return null;
        }

        (var stats, _gpuHardwareId) = HardwareSensorSelector.Select(readings, _gpuHardwareId);
        return stats;
    }
}
