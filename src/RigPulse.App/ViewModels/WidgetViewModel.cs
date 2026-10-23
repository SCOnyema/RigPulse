using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RigPulse.App.Services;
using RigPulse.App.Settings;
using RigPulse.Core.Abstractions;
using RigPulse.Core.Formatting;
using RigPulse.Core.Models;
using RigPulse.Core.Sensors;

namespace RigPulse.App.ViewModels;

/// <summary>
/// State and commands for the floating widget.
/// </summary>
public sealed partial class WidgetViewModel : ObservableObject, IDisposable
{
    private const string FanLabel = "FAN";

    private readonly ISnapshotPublisher _publisher;
    private readonly IWidgetSettingsStore _settingsStore;
    private readonly HardwareSensorFeed _hardwareSensorFeed;
    private readonly IAdminRelauncher _adminRelauncher;
    private WidgetSettings _settings;

    public WidgetViewModel(
        ISnapshotPublisher publisher,
        IWidgetSettingsStore settingsStore,
        HardwareSensorFeed hardwareSensorFeed,
        IHardwareAccess hardwareAccess,
        IAdminRelauncher adminRelauncher)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(settingsStore);
        ArgumentNullException.ThrowIfNull(hardwareSensorFeed);
        ArgumentNullException.ThrowIfNull(hardwareAccess);
        ArgumentNullException.ThrowIfNull(adminRelauncher);

        _publisher = publisher;
        _settingsStore = settingsStore;
        _hardwareSensorFeed = hardwareSensorFeed;
        _adminRelauncher = adminRelauncher;
        _settings = settingsStore.Load();

        Opacity = Math.Clamp(_settings.Opacity, WidgetSettings.MinOpacity, WidgetSettings.MaxOpacity);
        IsTopmost = _settings.IsTopmost;
        IsExpanded = _settings.IsExpanded;
        _hardwareSensorFeed.IsRequested = IsExpanded;

        CanRestartAsAdmin = !hardwareAccess.IsElevated;
        AccessHint = SensorAccessHints.For(hardwareAccess.IsElevated, hardwareAccess.IsDriverInstalled);

        _publisher.SnapshotPublished += OnSnapshotPublished;
    }

    /// <summary>Raised when the view should close (which exits the app).</summary>
    public event EventHandler? CloseRequested;

    public UsageMetricViewModel Cpu { get; } = new("CPU");

    public UsageMetricViewModel Memory { get; } = new("RAM");

    public UsageMetricViewModel Gpu { get; } = new("GPU");

    public ThroughputMetricViewModel Disk { get; } = new("DISK", "R", "W");

    public ThroughputMetricViewModel Network { get; } = new("NET", "↓", "↑");

    /// <summary>Package temperature with average and max core clocks.</summary>
    public SensorRowViewModel CpuSensors { get; } = new("CPU");

    /// <summary>Core temperature and clock of the selected GPU.</summary>
    public SensorRowViewModel GpuSensors { get; } = new("GPU");

    public SensorRowViewModel Vram { get; } = new("VRAM");

    /// <summary>One row per spinning fan.</summary>
    public ObservableCollection<SensorRowViewModel> Fans { get; } = [];

    public SensorRowViewModel Battery { get; } = new("BAT");

    /// <summary>Whether the machine has a battery; the battery row is hidden on desktops.</summary>
    [ObservableProperty]
    public partial bool HasBattery { get; private set; }

    /// <summary>Whole-window opacity from settings.</summary>
    public double Opacity { get; }

    [ObservableProperty]
    public partial bool IsTopmost { get; set; }

    /// <summary>Whether the details section is open.</summary>
    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    /// <summary>Whether "Restart as administrator" is offered (only when not already elevated).</summary>
    public bool CanRestartAsAdmin { get; }

    /// <summary>A short hint explaining why some sensors show "–", or <see langword="null"/>.</summary>
    public string? AccessHint { get; }

    public bool HasAccessHint => AccessHint is not null;

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

    [RelayCommand]
    private void RestartAsAdmin()
    {
        // If the user declines the UAC prompt (or it fails), simply keep running as we are.
        if (_adminRelauncher.RelaunchElevated() == RelaunchResult.Started)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    partial void OnIsTopmostChanged(bool value)
    {
        if (_settings.IsTopmost != value)
        {
            UpdateSettings(_settings with { IsTopmost = value });
        }
    }

    partial void OnIsExpandedChanged(bool value)
    {
        _hardwareSensorFeed.IsRequested = value;

        if (_settings.IsExpanded != value)
        {
            UpdateSettings(_settings with { IsExpanded = value });
        }
    }

    private void UpdateSettings(WidgetSettings settings)
    {
        _settings = settings;
        _settingsStore.Save(settings);
    }

    private void OnSnapshotPublished(object? sender, SystemSnapshot snapshot)
    {
        var culture = CultureInfo.CurrentCulture;

        Cpu.Update(snapshot.Cpu?.UsagePercent);
        Memory.Update(
            snapshot.Memory?.UsagePercent,
            snapshot.Memory is { } memory
                ? $"{ByteFormatter.FormatBytes(memory.UsedBytes, culture)} / {ByteFormatter.FormatBytes(memory.TotalBytes, culture)}"
                : null);
        Gpu.Update(snapshot.Gpu?.UsagePercent);
        Disk.Update(snapshot.Disk?.ReadBytesPerSecond, snapshot.Disk?.WriteBytesPerSecond);
        Network.Update(snapshot.Network?.DownloadBytesPerSecond, snapshot.Network?.UploadBytesPerSecond);

        // Cheap, and while collapsed the snapshot has no sensor data, so reopening never shows stale values.
        UpdateDetails(snapshot, culture);
    }

    private void UpdateDetails(SystemSnapshot snapshot, CultureInfo culture)
    {
        var cpu = snapshot.Sensors?.Cpu;
        CpuSensors.Update(
            SensorFormatter.FormatTemperature(cpu?.PackageTemperatureCelsius, culture),
            SensorFormatter.FormatClockRange(cpu?.AverageCoreClockMhz, cpu?.MaxCoreClockMhz, culture),
            TemperatureLevel(cpu?.PackageTemperatureCelsius));

        var gpu = snapshot.Sensors?.Gpu;
        GpuSensors.Update(
            SensorFormatter.FormatTemperature(gpu?.TemperatureCelsius, culture),
            SensorFormatter.FormatClock(gpu?.CoreClockMhz, culture),
            TemperatureLevel(gpu?.TemperatureCelsius),
            gpu?.Name);
        Vram.Update(SensorFormatter.FormatVram(gpu?.VramUsedBytes, gpu?.VramTotalBytes, culture), toolTip: gpu?.Name);

        UpdateFans(snapshot.Sensors?.Fans ?? [], culture);

        HasBattery = snapshot.Battery is not null;
        if (snapshot.Battery is { } battery)
        {
            Battery.Update(
                SensorFormatter.FormatPercent(battery.ChargePercent, culture),
                SensorFormatter.FormatBatteryStatus(battery, culture));
        }
    }

    /// <summary>
    /// Reuses existing fan rows so the list only changes when fans start or stop.
    /// </summary>
    private void UpdateFans(IReadOnlyList<FanReading> fans, CultureInfo culture)
    {
        while (Fans.Count > fans.Count)
        {
            Fans.RemoveAt(Fans.Count - 1);
        }

        while (Fans.Count < fans.Count)
        {
            Fans.Add(new SensorRowViewModel(Fans.Count == 0 ? FanLabel : string.Empty));
        }

        for (var i = 0; i < fans.Count; i++)
        {
            Fans[i].Update(SensorFormatter.FormatRpm(fans[i].Rpm, culture), fans[i].Name);
        }
    }

    private static LoadLevel TemperatureLevel(double? celsius) =>
        celsius is { } c ? TemperatureLevels.FromCelsius(c) : LoadLevel.Normal;
}
