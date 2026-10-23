using RigPulse.Core.Models;

namespace RigPulse.App.Services;

/// <summary>
/// One read of the slow hardware sensors.
/// </summary>
/// <param name="Sensors">Temperatures, clocks, VRAM and fans, or <see langword="null"/> if unavailable.</param>
/// <param name="Battery">Battery state, or <see langword="null"/> on desktops.</param>
public sealed record HardwareSample(HardwareSensorStats? Sensors, BatteryStats? Battery);

/// <summary>
/// Connects the slow hardware-sensor loop to the rest of the app: the UI says whether sensor data
/// is wanted, the sensor loop stores its latest read here, and the 1 s monitor loop copies that
/// read into each snapshot.
/// </summary>
/// <remarks>Thread-safe: written and read from different threads without locks.</remarks>
public sealed class HardwareSensorFeed
{
    private volatile bool _isRequested;
    private HardwareSample? _latest;

    /// <summary>
    /// Gets or sets whether anything is showing hardware sensors. While <see langword="false"/>,
    /// the sensors aren't read at all, so a collapsed widget costs the same as one without them.
    /// </summary>
    public bool IsRequested
    {
        get => _isRequested;
        set
        {
            _isRequested = value;
            if (!value)
            {
                Volatile.Write(ref _latest, null); // never show stale values when reopened
            }
        }
    }

    /// <summary>Gets the most recent read, or <see langword="null"/> if there is none yet.</summary>
    public HardwareSample? Latest => Volatile.Read(ref _latest);

    /// <summary>Stores a new read.</summary>
    public void Publish(HardwareSample? sample) => Volatile.Write(ref _latest, sample);
}
