namespace RigPulse.Core.Sensors;

/// <summary>
/// Picks the short hint shown when some sensors can't be read.
/// </summary>
public static class SensorAccessHints
{
    /// <summary>Shown when the process is not elevated.</summary>
    public const string RunAsAdmin = "Run as admin for temps";

    /// <summary>Shown when the process is elevated but the PawnIO driver is missing.</summary>
    public const string InstallDriver = "Install PawnIO for CPU temps";

    /// <summary>
    /// Returns the hint for the given access level.
    /// </summary>
    /// <param name="isElevated">Whether the process runs as administrator.</param>
    /// <param name="isDriverInstalled">Whether the PawnIO driver is installed.</param>
    /// <returns>The hint text, or <see langword="null"/> when every sensor is accessible.</returns>
    public static string? For(bool isElevated, bool isDriverInstalled) =>
        !isElevated ? RunAsAdmin
        : !isDriverInstalled ? InstallDriver
        : null;
}
