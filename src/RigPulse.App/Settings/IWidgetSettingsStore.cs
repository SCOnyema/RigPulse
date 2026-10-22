namespace RigPulse.App.Settings;

/// <summary>
/// Loads and saves <see cref="WidgetSettings"/>. Implementations never throw: load falls back to
/// defaults and save failures are logged.
/// </summary>
public interface IWidgetSettingsStore
{
    WidgetSettings Load();

    void Save(WidgetSettings settings);
}
