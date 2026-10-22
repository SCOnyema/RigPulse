namespace RigPulse.App.Services;

/// <summary>
/// Settings for the monitoring loop, bound from the "Monitor" section of appsettings.json.
/// </summary>
public sealed class MonitorOptions
{
    public const string SectionName = "Monitor";

    /// <summary>How often all providers are sampled.</summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>How many recent snapshots are kept in memory for graphs.</summary>
    public int HistoryCapacity { get; set; } = 60;
}
