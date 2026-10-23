namespace RigPulse.Core.Abstractions;

/// <summary>
/// Describes how much hardware access this process has, which decides which sensors can be read.
/// </summary>
public interface IHardwareAccess
{
    /// <summary>Gets whether the process runs with administrator rights.</summary>
    bool IsElevated { get; }

    /// <summary>
    /// Gets whether the PawnIO kernel driver is installed. It is needed for CPU temperatures,
    /// CPU clocks and mainboard fans.
    /// </summary>
    bool IsDriverInstalled { get; }
}
