namespace RigPulse.Core.Models;

/// <summary>
/// How heavily a resource is being used, for colour-coding percentage metrics.
/// </summary>
public enum LoadLevel
{
    /// <summary>Below <see cref="LoadLevels.ElevatedThreshold"/>.</summary>
    Normal,

    /// <summary>From <see cref="LoadLevels.ElevatedThreshold"/> up to and including <see cref="LoadLevels.CriticalThreshold"/>.</summary>
    Elevated,

    /// <summary>Above <see cref="LoadLevels.CriticalThreshold"/>.</summary>
    Critical,
}

/// <summary>
/// The single source of truth for load thresholds.
/// </summary>
public static class LoadLevels
{
    /// <summary>Usage percentage at which load counts as <see cref="LoadLevel.Elevated"/>.</summary>
    public const double ElevatedThreshold = 70;

    /// <summary>Usage percentage above which load counts as <see cref="LoadLevel.Critical"/>.</summary>
    public const double CriticalThreshold = 90;

    /// <summary>
    /// Classifies a usage percentage.
    /// </summary>
    /// <param name="percent">Usage from 0 to 100. NaN is treated as <see cref="LoadLevel.Normal"/>.</param>
    /// <returns>The load level for <paramref name="percent"/>.</returns>
    public static LoadLevel FromPercent(double percent) => percent switch
    {
        > CriticalThreshold => LoadLevel.Critical,
        >= ElevatedThreshold => LoadLevel.Elevated,
        _ => LoadLevel.Normal,
    };
}
