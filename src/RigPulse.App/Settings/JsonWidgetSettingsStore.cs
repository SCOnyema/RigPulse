using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace RigPulse.App.Settings;

/// <summary>
/// Stores settings as JSON in <c>%AppData%\RigPulse\settings.json</c>.
/// </summary>
internal sealed partial class JsonWidgetSettingsStore(ILogger<JsonWidgetSettingsStore> logger) : IWidgetSettingsStore
{
    private readonly string _path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RigPulse", "settings.json");

    public WidgetSettings Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return new WidgetSettings();
            }

            using var stream = File.OpenRead(_path);
            return JsonSerializer.Deserialize(stream, SettingsJsonContext.Default.WidgetSettings) ?? new WidgetSettings();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            LogLoadFailed(_path, ex);
            return new WidgetSettings();
        }
    }

    public void Save(WidgetSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);

            // Write to a temp file and swap it in, so a crash mid-write can't corrupt the settings.
            var tempPath = _path + ".tmp";
            using (var stream = File.Create(tempPath))
            {
                JsonSerializer.Serialize(stream, settings, SettingsJsonContext.Default.WidgetSettings);
            }

            File.Move(tempPath, _path, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LogSaveFailed(_path, ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not read settings from {Path}; using defaults.")]
    private partial void LogLoadFailed(string path, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not save settings to {Path}.")]
    private partial void LogSaveFailed(string path, Exception exception);
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(WidgetSettings))]
internal sealed partial class SettingsJsonContext : JsonSerializerContext;
