using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using RigPulse.Core.Abstractions;
using RigPulse.Core.Models;
using RigPulse.Sensors.Internal;
using RigPulse.Sensors.Interop;

namespace RigPulse.Sensors;

/// <summary>
/// Reads the battery state with the Win32 <c>GetSystemPowerStatus</c> API, which needs no admin
/// rights. Returns <see langword="null"/> on desktops.
/// </summary>
internal sealed class BatteryProvider(ILogger<BatteryProvider> logger, TimeProvider timeProvider) : IBatteryProvider
{
    private readonly MetricHealth _health = new("Battery status", logger, timeProvider);

    public BatteryStats? Read()
    {
        if (!NativeMethods.GetSystemPowerStatus(out var status))
        {
            _health.ReportFailure($"GetSystemPowerStatus failed: {Marshal.GetLastPInvokeErrorMessage()}");
            return null;
        }

        _health.ReportSuccess();
        return Map(status.AcLineStatus, status.BatteryFlag, status.BatteryLifePercent, status.BatteryLifeTime);
    }

    /// <summary>
    /// Interprets the raw <c>SYSTEM_POWER_STATUS</c> fields.
    /// </summary>
    /// <returns>The battery state, or <see langword="null"/> if there is no battery or its state is unknown.</returns>
    internal static BatteryStats? Map(byte acLineStatus, byte batteryFlag, byte lifePercent, uint lifeTimeSeconds)
    {
        if (batteryFlag == NativeMethods.BatteryFlagUnknown
            || (batteryFlag & NativeMethods.BatteryFlagNoSystemBattery) != 0
            || lifePercent > 100) // 255 means unknown
        {
            return null;
        }

        var state = (batteryFlag & NativeMethods.BatteryFlagCharging) != 0 ? BatteryChargeState.Charging
            : acLineStatus == NativeMethods.AcLineOnline ? (lifePercent >= 100 ? BatteryChargeState.Full : BatteryChargeState.PluggedIn)
            : BatteryChargeState.Discharging;

        // Windows only estimates time left while discharging, and needs a few seconds after
        // unplugging before it has one (until then it reports "unknown").
        TimeSpan? timeRemaining = state == BatteryChargeState.Discharging && lifeTimeSeconds != NativeMethods.BatteryLifeUnknown
            ? TimeSpan.FromSeconds(lifeTimeSeconds)
            : null;

        return new BatteryStats(lifePercent, state, timeRemaining);
    }
}
