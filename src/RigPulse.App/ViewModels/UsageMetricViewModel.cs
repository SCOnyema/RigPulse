using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using RigPulse.Core.Formatting;
using RigPulse.Core.Models;

namespace RigPulse.App.ViewModels;

/// <summary>
/// One percentage-based widget row (CPU, RAM, GPU): a label, a value, optional detail text and a bar.
/// </summary>
public sealed partial class UsageMetricViewModel(string label) : ObservableObject
{
    public string Label { get; } = label;

    /// <summary>Formatted percentage, e.g. "42%", or "–" when unavailable.</summary>
    [ObservableProperty]
    public partial string Value { get; private set; } = ByteFormatter.Unavailable;

    /// <summary>Optional secondary text, e.g. "8.1 GB / 15.9 GB".</summary>
    [ObservableProperty]
    public partial string Detail { get; private set; } = string.Empty;

    /// <summary>Bar fill from 0 to 100.</summary>
    [ObservableProperty]
    public partial double Percent { get; private set; }

    /// <summary>Load band of <see cref="Percent"/>, used to colour the bar.</summary>
    [ObservableProperty]
    public partial LoadLevel Level { get; private set; }

    /// <summary>
    /// Updates the row. Pass <see langword="null"/> when the metric is unavailable.
    /// </summary>
    public void Update(double? percent, string? detail = null)
    {
        Percent = percent ?? 0;
        Level = LoadLevels.FromPercent(Percent);
        Value = percent is { } p ? string.Create(CultureInfo.CurrentCulture, $"{p:0}%") : ByteFormatter.Unavailable;
        Detail = detail ?? string.Empty;
    }
}
