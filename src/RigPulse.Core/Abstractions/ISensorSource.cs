using RigPulse.Core.Models;

namespace RigPulse.Core.Abstractions;

/// <summary>
/// Reads raw hardware sensors (temperatures, clocks, fans...) through a hardware-monitoring library.
/// </summary>
/// <remarks>
/// This is the seam between RigPulse and the library, so the logic that picks sensors can be tested
/// with fake readings. Implementations must never throw from <see cref="ReadAll"/>. A call may take
/// tens of milliseconds (seconds on the first one), so call it from a background thread.
/// Implementations are designed for a single polling caller.
/// </remarks>
public interface ISensorSource
{
    /// <summary>
    /// Refreshes and returns every supported sensor.
    /// </summary>
    /// <returns>The current readings, or an empty list if the hardware could not be read.</returns>
    IReadOnlyList<SensorReading> ReadAll();
}
