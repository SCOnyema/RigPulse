using LibreHardwareMonitor.PawnIo;
using RigPulse.Core.Abstractions;

namespace RigPulse.Sensors.Lhm;

/// <summary>
/// Reports whether this process is elevated and whether the PawnIO driver is installed.
/// </summary>
/// <remarks>
/// Both values are read once at startup. Installing PawnIO or gaining elevation needs a restart
/// anyway, because the library loads its driver modules when it opens.
/// </remarks>
internal sealed class HardwareAccess : IHardwareAccess
{
    public bool IsElevated { get; } = Environment.IsPrivilegedProcess;

    // The library checks PawnIO's uninstall registry key, which needs no elevation.
    public bool IsDriverInstalled { get; } = PawnIo.IsInstalled;
}
