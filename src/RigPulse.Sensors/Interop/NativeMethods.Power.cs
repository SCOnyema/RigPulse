using System.Runtime.InteropServices;

namespace RigPulse.Sensors.Interop;

internal static partial class NativeMethods
{
    /// <summary><c>ACLineStatus</c>: plugged in.</summary>
    internal const byte AcLineOnline = 1;

    /// <summary><c>BatteryFlag</c>: charging.</summary>
    internal const byte BatteryFlagCharging = 8;

    /// <summary><c>BatteryFlag</c>: no system battery.</summary>
    internal const byte BatteryFlagNoSystemBattery = 128;

    /// <summary><c>BatteryFlag</c>: unable to read the battery flag information.</summary>
    internal const byte BatteryFlagUnknown = 255;

    /// <summary><c>BatteryLifeTime</c>: remaining time is unknown, or the device is plugged in.</summary>
    internal const uint BatteryLifeUnknown = uint.MaxValue;

    /// <summary>
    /// Retrieves the power status of the system: AC or battery, and the battery's charge.
    /// </summary>
    /// <remarks>See https://learn.microsoft.com/windows/win32/api/winbase/nf-winbase-getsystempowerstatus.</remarks>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetSystemPowerStatus(out SystemPowerStatus status);

    /// <summary>
    /// Managed mirror of the Win32 <c>SYSTEM_POWER_STATUS</c> structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct SystemPowerStatus
    {
        public byte AcLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public uint BatteryLifeTime;
        public uint BatteryFullLifeTime;
    }
}
