namespace RigPulse.Core.Formatting;

/// <summary>
/// Formats byte counts and byte rates as short, human-readable strings such as "1.2 GB" or "3.4 MB/s".
/// </summary>
/// <remarks>
/// Uses binary multiples (1 KB = 1024 bytes) with the familiar KB/MB/GB labels, matching Windows
/// Explorer and Task Manager. Bytes and kilobytes are shown as whole numbers (e.g. "512 B/s",
/// "340 KB/s"); megabytes and above use one decimal place (e.g. "3.4 MB/s"). Negative, NaN and
/// infinite inputs are treated as invalid and render as
/// <see cref="Unavailable"/>.
/// </remarks>
public static class ByteFormatter
{
    /// <summary>
    /// The placeholder shown when a value is missing or invalid.
    /// </summary>
    public const string Unavailable = "–";

    private const double Step = 1024d;
    private const int KilobyteUnit = 1; // index of "KB" in Units

    private static readonly string[] Units = ["B", "KB", "MB", "GB", "TB", "PB"];

    /// <summary>
    /// Formats a byte count using the given culture, for example "1.2 GB".
    /// </summary>
    /// <param name="bytes">The number of bytes.</param>
    /// <param name="provider">The culture used for the decimal separator; <see langword="null"/> means the current culture.</param>
    /// <returns>The formatted size, or <see cref="Unavailable"/> for invalid input.</returns>
    public static string FormatBytes(double bytes, IFormatProvider? provider) => Format(bytes, suffix: string.Empty, provider);

    /// <summary>
    /// Formats a transfer rate using the given culture, for example "3.4 MB/s".
    /// </summary>
    /// <param name="bytesPerSecond">The rate in bytes per second.</param>
    /// <param name="provider">The culture used for the decimal separator; <see langword="null"/> means the current culture.</param>
    /// <returns>The formatted rate, or <see cref="Unavailable"/> for invalid input.</returns>
    public static string FormatRate(double bytesPerSecond, IFormatProvider? provider) => Format(bytesPerSecond, suffix: "/s", provider);

    private static string Format(double value, string suffix, IFormatProvider? provider)
    {
        if (!double.IsFinite(value) || value < 0)
        {
            return Unavailable;
        }

        var unit = 0;
        while (value >= Step && unit < Units.Length - 1)
        {
            value /= Step;
            unit++;
        }

        // Rounding can turn e.g. 1023.6 KB into "1024 KB"; promote it to the next unit instead.
        if (Math.Round(value, DecimalsFor(unit), MidpointRounding.AwayFromZero) >= Step && unit < Units.Length - 1)
        {
            value /= Step;
            unit++;
        }

        var number = value.ToString(DecimalsFor(unit) == 0 ? "0" : "0.0", provider);

        return string.Create(provider, $"{number} {Units[unit]}{suffix}");
    }

    /// <summary>B and KB are whole numbers; MB and above get one decimal.</summary>
    private static int DecimalsFor(int unit) => unit <= KilobyteUnit ? 0 : 1;
}
