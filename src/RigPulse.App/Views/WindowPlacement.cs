using System.Windows;
using System.Windows.Media;
using RigPulse.App.Interop;

namespace RigPulse.App.Views;

/// <summary>
/// Works out where the widget should appear so it is always fully visible, even if the monitor
/// it was last on has been disconnected or rearranged.
/// </summary>
internal static class WindowPlacement
{
    private const double DefaultMargin = 16;

    /// <summary>
    /// Returns a top-left position for a window of <paramref name="size"/>: the desired position
    /// clamped to the work area of the nearest monitor, or the top-right corner of the primary
    /// work area when there is no desired position.
    /// </summary>
    public static Point Resolve(Visual window, Point? desired, Size size)
    {
        if (desired is not { } position)
        {
            var primary = SystemParameters.WorkArea;
            return new Point(primary.Right - size.Width - DefaultMargin, primary.Top + DefaultMargin);
        }

        var workArea = GetNearestWorkArea(window, new Rect(position, size));
        return Clamp(position, size, workArea);
    }

    /// <summary>
    /// Moves <paramref name="position"/> the minimum distance needed to fit <paramref name="size"/>
    /// inside <paramref name="area"/>. If the window is larger than the area, its top-left corner wins.
    /// </summary>
    public static Point Clamp(Point position, Size size, Rect area) => new(
        Math.Clamp(position.X, area.Left, Math.Max(area.Left, area.Right - size.Width)),
        Math.Clamp(position.Y, area.Top, Math.Max(area.Top, area.Bottom - size.Height)));

    /// <summary>
    /// Gets the work area (screen minus taskbar) of the monitor nearest to <paramref name="bounds"/>,
    /// in device-independent pixels. Falls back to the whole virtual screen if the window has no
    /// handle yet or the Win32 call fails.
    /// </summary>
    private static Rect GetNearestWorkArea(Visual window, Rect bounds)
    {
        var virtualScreen = new Rect(
            SystemParameters.VirtualScreenLeft,
            SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenWidth,
            SystemParameters.VirtualScreenHeight);

        if (PresentationSource.FromVisual(window)?.CompositionTarget is not { } target)
        {
            return virtualScreen;
        }

        var toDevice = target.TransformToDevice;
        var deviceBounds = Rect.Transform(bounds, toDevice);
        var rect = new NativeMethods.NativeRect
        {
            Left = (int)deviceBounds.Left,
            Top = (int)deviceBounds.Top,
            Right = (int)deviceBounds.Right,
            Bottom = (int)deviceBounds.Bottom,
        };

        var monitor = NativeMethods.MonitorFromRect(ref rect, NativeMethods.MonitorDefaultToNearest);
        var info = NativeMethods.MonitorInfo.Create();
        if (monitor == 0 || !NativeMethods.GetMonitorInfo(monitor, ref info))
        {
            return virtualScreen;
        }

        var work = new Rect(
            new Point(info.Work.Left, info.Work.Top),
            new Point(info.Work.Right, info.Work.Bottom));
        return Rect.Transform(work, target.TransformFromDevice);
    }
}
