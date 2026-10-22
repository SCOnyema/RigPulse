using System.Runtime.InteropServices;

namespace RigPulse.App.Interop;

internal static partial class NativeMethods
{
    /// <summary>Return the monitor nearest to the rectangle if it doesn't intersect any.</summary>
    internal const uint MonitorDefaultToNearest = 0x00000002;

    [LibraryImport("user32.dll")]
    internal static partial nint MonitorFromRect(ref NativeRect rect, uint flags);

    [LibraryImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetMonitorInfo(nint monitor, ref MonitorInfo info);

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MonitorInfo
    {
        public uint Size;
        public NativeRect Monitor;
        public NativeRect Work;
        public uint Flags;

        public static MonitorInfo Create() => new() { Size = (uint)Marshal.SizeOf<MonitorInfo>() };
    }
}
