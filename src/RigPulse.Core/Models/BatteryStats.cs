namespace RigPulse.Core.Models;

/// <summary>
/// Whether the battery is gaining or losing charge.
/// </summary>
public enum BatteryChargeState
{
    /// <summary>Running on battery.</summary>
    Discharging,

    /// <summary>Plugged in and charging.</summary>
    Charging,

    /// <summary>Plugged in, not charging, and full.</summary>
    Full,

    /// <summary>Plugged in but not charging and not full, e.g. because a charge limit is set.</summary>
    PluggedIn,
}

/// <summary>
/// The system battery's state.
/// </summary>
/// <param name="ChargePercent">Remaining charge, from 0 to 100.</param>
/// <param name="State">Whether the battery is charging or discharging.</param>
/// <param name="TimeRemaining">Estimated time until empty while discharging, or <see langword="null"/> when unknown or not discharging.</param>
public sealed record BatteryStats(double ChargePercent, BatteryChargeState State, TimeSpan? TimeRemaining);
