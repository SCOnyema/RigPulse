using CommunityToolkit.Mvvm.ComponentModel;
using RigPulse.Core.Formatting;
using RigPulse.Core.Models;

namespace RigPulse.App.ViewModels;

/// <summary>
/// One row of the details section (CPU and GPU temperature, VRAM, a fan, battery): a label,
/// secondary detail text and a right-aligned value.
/// </summary>
public sealed partial class SensorRowViewModel(string label) : ObservableObject
{
    [ObservableProperty]
    public partial string Label { get; set; } = label;

    /// <summary>Formatted value shown on the right, e.g. "72°C", or "–" when unavailable.</summary>
    [ObservableProperty]
    public partial string Value { get; private set; } = ByteFormatter.Unavailable;

    /// <summary>Secondary text shown before the value, e.g. "1.35 GHz".</summary>
    [ObservableProperty]
    public partial string Detail { get; private set; } = string.Empty;

    /// <summary>Colours the value; used for temperatures (thresholds in <see cref="TemperatureLevels"/>).</summary>
    [ObservableProperty]
    public partial LoadLevel Level { get; private set; }

    /// <summary>Optional hover text, e.g. the GPU's name.</summary>
    [ObservableProperty]
    public partial string? ToolTip { get; private set; }

    public void Update(string value, string detail = "", LoadLevel level = LoadLevel.Normal, string? toolTip = null)
    {
        Value = value;
        Detail = detail;
        Level = level;
        ToolTip = toolTip;
    }
}
