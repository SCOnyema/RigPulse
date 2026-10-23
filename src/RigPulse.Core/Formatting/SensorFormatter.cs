using RigPulse.Core.Models;

namespace RigPulse.Core.Formatting;

/// <summary>
/// Formats hardware sensor values for the widget, e.g. "72°C", "3.41 GHz", "3.1 / 8.0 GB" and
/// "1,240 RPM". Missing, negative, NaN and infinite inputs render as <see cref="ByteFormatter.Unavailable"/>.
/// </summary>
public static class SensorFormatter
{
    private const double MegahertzPerGigahertz = 1000;
    private const double BytesPerGigabyte = 1024d * 1024d * 1024d;

    /// <summary>
    /// Formats a temperature as whole degrees, e.g. "72°C".
    /// </summary>
    /// <param name="celsius">The temperature in °C, or <see langword="null"/>.</param>
    /// <param name="provider">The culture to format with; <see langword="null"/> means the current culture.</param>
    /// <returns>The formatted temperature, or <see cref="ByteFormatter.Unavailable"/>.</returns>
    public static string FormatTemperature(double? celsius, IFormatProvider? provider) =>
        celsius is { } c && IsValid(c) ? string.Create(provider, $"{c:0}°C") : ByteFormatter.Unavailable;

    /// <summary>
    /// Formats a clock speed in gigahertz with two decimals, e.g. "3.41 GHz".
    /// </summary>
    /// <param name="megahertz">The clock in MHz, or <see langword="null"/>.</param>
    /// <param name="provider">The culture to format with; <see langword="null"/> means the current culture.</param>
    /// <returns>The formatted clock, or <see cref="ByteFormatter.Unavailable"/>.</returns>
    public static string FormatClock(double? megahertz, IFormatProvider? provider) =>
        megahertz is { } mhz && IsValid(mhz) ? string.Create(provider, $"{mhz / MegahertzPerGigahertz:0.00} GHz") : ByteFormatter.Unavailable;

    /// <summary>
    /// Formats average and maximum CPU clocks, e.g. "avg 3.14 · max 4.20 GHz".
    /// </summary>
    /// <param name="averageMegahertz">The average core clock in MHz, or <see langword="null"/>.</param>
    /// <param name="maxMegahertz">The fastest core clock in MHz, or <see langword="null"/>.</param>
    /// <param name="provider">The culture to format with; <see langword="null"/> means the current culture.</param>
    /// <returns>The formatted clocks, or <see cref="ByteFormatter.Unavailable"/> if either is missing.</returns>
    public static string FormatClockRange(double? averageMegahertz, double? maxMegahertz, IFormatProvider? provider) =>
        averageMegahertz is { } avg && maxMegahertz is { } max && IsValid(avg) && IsValid(max)
            ? string.Create(provider, $"avg {avg / MegahertzPerGigahertz:0.00} · max {max / MegahertzPerGigahertz:0.00} GHz")
            : ByteFormatter.Unavailable;

    /// <summary>
    /// Formats video memory use with one decimal and a shared unit, e.g. "3.1 / 8.0 GB".
    /// </summary>
    /// <param name="usedBytes">Memory in use, or <see langword="null"/>.</param>
    /// <param name="totalBytes">Total memory, or <see langword="null"/>.</param>
    /// <param name="provider">The culture to format with; <see langword="null"/> means the current culture.</param>
    /// <returns>The formatted usage, or <see cref="ByteFormatter.Unavailable"/> if either is missing or total is 0.</returns>
    public static string FormatVram(ulong? usedBytes, ulong? totalBytes, IFormatProvider? provider) =>
        usedBytes is { } used && totalBytes is { } total && total > 0
            ? string.Create(provider, $"{used / BytesPerGigabyte:0.0} / {total / BytesPerGigabyte:0.0} GB")
            : ByteFormatter.Unavailable;

    /// <summary>
    /// Formats a fan speed with a thousands separator, e.g. "1,240 RPM".
    /// </summary>
    /// <param name="rpm">The speed in revolutions per minute.</param>
    /// <param name="provider">The culture to format with; <see langword="null"/> means the current culture.</param>
    /// <returns>The formatted speed, or <see cref="ByteFormatter.Unavailable"/>.</returns>
    public static string FormatRpm(double rpm, IFormatProvider? provider) =>
        IsValid(rpm) ? string.Create(provider, $"{rpm:N0} RPM") : ByteFormatter.Unavailable;

    /// <summary>
    /// Formats a whole percentage, e.g. "87%".
    /// </summary>
    /// <param name="percent">The percentage.</param>
    /// <param name="provider">The culture to format with; <see langword="null"/> means the current culture.</param>
    /// <returns>The formatted percentage, or <see cref="ByteFormatter.Unavailable"/>.</returns>
    public static string FormatPercent(double percent, IFormatProvider? provider) =>
        IsValid(percent) ? string.Create(provider, $"{percent:0}%") : ByteFormatter.Unavailable;

    /// <summary>
    /// Formats a duration as hours and minutes, e.g. "2 h 05 m" or "45 m".
    /// </summary>
    /// <param name="duration">The duration. Seconds are rounded down.</param>
    /// <param name="provider">The culture to format with; <see langword="null"/> means the current culture.</param>
    /// <returns>The formatted duration, or <see cref="ByteFormatter.Unavailable"/> if negative.</returns>
    public static string FormatDuration(TimeSpan duration, IFormatProvider? provider)
    {
        if (duration < TimeSpan.Zero)
        {
            return ByteFormatter.Unavailable;
        }

        var hours = (int)duration.TotalHours;
        return hours > 0
            ? string.Create(provider, $"{hours} h {duration.Minutes:00} m")
            : string.Create(provider, $"{duration.Minutes} m");
    }

    /// <summary>
    /// Describes what the battery is doing, e.g. "Charging" or "Discharging · 2 h 05 m".
    /// </summary>
    /// <param name="battery">The battery state.</param>
    /// <param name="provider">The culture to format with; <see langword="null"/> means the current culture.</param>
    /// <returns>A short status text.</returns>
    public static string FormatBatteryStatus(BatteryStats battery, IFormatProvider? provider)
    {
        ArgumentNullException.ThrowIfNull(battery);

        return battery.State switch
        {
            BatteryChargeState.Charging => "Charging",
            BatteryChargeState.Full => "Full",
            BatteryChargeState.PluggedIn => "Plugged in",
            _ when battery.TimeRemaining is { } remaining => $"Discharging · {FormatDuration(remaining, provider)}",
            _ => "Discharging",
        };
    }

    private static bool IsValid(double value) => double.IsFinite(value) && value >= 0;
}
