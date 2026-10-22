using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using RigPulse.Core.Formatting;

namespace RigPulse.App.ViewModels;

/// <summary>
/// One rate-based widget row (disk, network) showing two directions, e.g. read/write or down/up.
/// </summary>
public sealed partial class ThroughputMetricViewModel(string label, string firstSymbol, string secondSymbol) : ObservableObject
{
    public string Label { get; } = label;

    /// <summary>Symbol shown before the first rate, e.g. "R" or "↓".</summary>
    public string FirstSymbol { get; } = firstSymbol;

    /// <summary>Symbol shown before the second rate, e.g. "W" or "↑".</summary>
    public string SecondSymbol { get; } = secondSymbol;

    [ObservableProperty]
    public partial string FirstRate { get; private set; } = ByteFormatter.Unavailable;

    [ObservableProperty]
    public partial string SecondRate { get; private set; } = ByteFormatter.Unavailable;

    /// <summary>
    /// Updates both rates, in bytes per second. Pass <see langword="null"/> when unavailable.
    /// </summary>
    public void Update(double? firstBytesPerSecond, double? secondBytesPerSecond)
    {
        FirstRate = firstBytesPerSecond is { } first ? ByteFormatter.FormatRate(first, CultureInfo.CurrentCulture) : ByteFormatter.Unavailable;
        SecondRate = secondBytesPerSecond is { } second ? ByteFormatter.FormatRate(second, CultureInfo.CurrentCulture) : ByteFormatter.Unavailable;
    }
}
