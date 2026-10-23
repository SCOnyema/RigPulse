namespace RigPulse.Core.Models;

/// <summary>
/// The single source of truth for temperature colour thresholds.
/// </summary>
/// <remarks>
/// Temperatures reuse <see cref="LoadLevel"/>, so they are coloured with the same normal, amber and
/// red brushes as the load bars.
/// </remarks>
public static class TemperatureLevels
{
    /// <summary>Temperature in °C at which a reading counts as <see cref="LoadLevel.Elevated"/> (amber).</summary>
    public const double ElevatedThreshold = 70;

    /// <summary>Temperature in °C above which a reading counts as <see cref="LoadLevel.Critical"/> (red).</summary>
    public const double CriticalThreshold = 85;

    /// <summary>
    /// Classifies a temperature.
    /// </summary>
    /// <param name="celsius">The temperature in °C. NaN is treated as <see cref="LoadLevel.Normal"/>.</param>
    /// <returns>The level for <paramref name="celsius"/>.</returns>
    public static LoadLevel FromCelsius(double celsius) => celsius switch
    {
        > CriticalThreshold => LoadLevel.Critical,
        >= ElevatedThreshold => LoadLevel.Elevated,
        _ => LoadLevel.Normal,
    };
}
