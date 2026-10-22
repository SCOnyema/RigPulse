using System.Runtime.InteropServices;

namespace RigPulse.Sensors.Interop;

internal static partial class NativeMethods
{
    /// <summary>
    /// Retrieves information about the system's current physical and virtual memory usage.
    /// </summary>
    /// <remarks>See https://learn.microsoft.com/windows/win32/api/sysinfoapi/nf-sysinfoapi-globalmemorystatusex.</remarks>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

    /// <summary>
    /// Managed mirror of the Win32 <c>MEMORYSTATUSEX</c> structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;

        public static MemoryStatusEx Create() => new() { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
    }
}
