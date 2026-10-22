using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RigPulse.App.Settings;
using RigPulse.Core.Abstractions;
using RigPulse.Core.Formatting;
using RigPulse.Core.Models;

namespace RigPulse.App.ViewModels;

/// <summary>
/// State and commands for the floating widget.
/// </summary>
public sealed partial class WidgetViewModel : ObservableObject, IDisposable
{
    private readonly ISnapshotPublisher _publisher;
    private readonly IWidgetSettingsStore _settingsStore;
    private WidgetSettings _settings;

    public WidgetViewModel(ISnapshotPublisher publisher, IWidgetSettingsStore settingsStore)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(settingsStore);

        _publisher = publisher;
        _settingsStore = settingsStore;
        _settings = settingsStore.Load();

        Opacity = Math.Clamp(_settings.Opacity, WidgetSettings.MinOpacity, WidgetSettings.MaxOpacity);
        IsTopmost = _settings.IsTopmost;

        _publisher.SnapshotPublished += OnSnapshotPublished;
    }

    /// <summary>Raised when the view should close (which exits the app).</summary>
    public event EventHandler? CloseRequested;

    public UsageMetricViewModel Cpu { get; } = new("CPU");

    public UsageMetricViewModel Memory { get; } = new("RAM");

    public UsageMetricViewModel Gpu { get; } = new("GPU");

    public ThroughputMetricViewModel Disk { get; } = new("DISK", "R", "W");

    public ThroughputMetricViewModel Network { get; } = new("NET", "↓", "↑");

    /// <summary>Whole-window opacity from settings.</summary>
    public double Opacity { get; }

    [ObservableProperty]
    public partial bool IsTopmost { get; set; }

    /// <summary>The last saved window position, or <see langword="null"/> on first run.</summary>
    public (double Left, double Top)? SavedPosition =>
        _settings is { Left: { } left, Top: { } top } ? (left, top) : null;

    /// <summary>
    /// Persists the window position if it changed.
    /// </summary>
    public void SavePosition(double left, double top)
    {
        if (!double.IsFinite(left) || !double.IsFinite(top) || SavedPosition == (left, top))
        {
            return;
        }

        UpdateSettings(_settings with { Left = left, Top = top });
    }

    public void Dispose() => _publisher.SnapshotPublished -= OnSnapshotPublished;

    [RelayCommand]
    private void Exit() => CloseRequested?.Invoke(this, EventArgs.Empty);

    partial void OnIsTopmostChanged(bool value)
    {
        if (_settings.IsTopmost != value)
        {
            UpdateSettings(_settings with { IsTopmost = value });
        }
    }

    private void UpdateSettings(WidgetSettings settings)
    {
        _settings = settings;
        _settingsStore.Save(settings);
    }

    private void OnSnapshotPublished(object? sender, SystemSnapshot snapshot)
    {
        Cpu.Update(snapshot.Cpu?.UsagePercent);
        Memory.Update(
            snapshot.Memory?.UsagePercent,
            snapshot.Memory is { } memory
                ? $"{ByteFormatter.FormatBytes(memory.UsedBytes, CultureInfo.CurrentCulture)} / {ByteFormatter.FormatBytes(memory.TotalBytes, CultureInfo.CurrentCulture)}"
                : null);
        Gpu.Update(snapshot.Gpu?.UsagePercent);
        Disk.Update(snapshot.Disk?.ReadBytesPerSecond, snapshot.Disk?.WriteBytesPerSecond);
        Network.Update(snapshot.Network?.DownloadBytesPerSecond, snapshot.Network?.UploadBytesPerSecond);
    }
}
