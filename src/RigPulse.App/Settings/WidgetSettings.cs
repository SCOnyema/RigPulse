namespace RigPulse.App.Settings;

/// <summary>
/// User preferences for the widget window, persisted between runs.
/// </summary>
public sealed record WidgetSettings
{
    public const double MinOpacity = 0.2;
    public const double MaxOpacity = 1.0;

    /// <summary>Window left edge in device-independent pixels, or <see langword="null"/> for the default position.</summary>
    public double? Left { get; init; }

    /// <summary>Window top edge in device-independent pixels, or <see langword="null"/> for the default position.</summary>
    public double? Top { get; init; }

    /// <summary>Whole-window opacity, from <see cref="MinOpacity"/> to <see cref="MaxOpacity"/>.</summary>
    public double Opacity { get; init; } = MaxOpacity;

    /// <summary>Whether the widget stays above other windows.</summary>
    public bool IsTopmost { get; init; } = true;
}
