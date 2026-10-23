namespace RigPulse.App.Services;

/// <summary>
/// Settings for the monitoring loops, bound from the "Monitor" section of appsettings.json.
/// </summary>
public sealed class MonitorOptions
{
    public const string SectionName = "Monitor";

    /// <summary>How often CPU, RAM, GPU load, disk and network are sampled.</summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// How often hardware sensors (temperatures, clocks, VRAM, fans) and the battery are read.
    /// Reading them costs far more than the other metrics, so it runs on its own slower loop,
    /// and only while the details section is open.
    /// </summary>
    public TimeSpan SensorPollInterval { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>How many recent snapshots are kept in memory for graphs.</summary>
    public int HistoryCapacity { get; set; } = 60;
}
