using System.Diagnostics.CodeAnalysis;
using LibreHardwareMonitor.Hardware;
using Microsoft.Extensions.Logging;
using RigPulse.Core.Abstractions;
using RigPulse.Core.Models;
using RigPulse.Sensors.Internal;

namespace RigPulse.Sensors.Lhm;

/// <summary>
/// Reads hardware sensors through LibreHardwareMonitorLib. This is the only class that touches the
/// library's types; everything it returns is a plain <see cref="SensorReading"/>.
/// </summary>
/// <remarks>
/// <para>
/// One <see cref="Computer"/> is opened lazily on the first <see cref="ReadAll"/> (so its cost, up
/// to several seconds on a cold start, lands on the polling thread rather than app startup), kept
/// open, refreshed with <see cref="UpdateVisitor"/>, and closed on dispose.
/// </para>
/// <para>
/// Only CPU, GPU and motherboard are enabled. Memory and storage aren't shown. USB/serial fan
/// controllers are skipped on purpose: probing them means writing to arbitrary serial ports.
/// Without admin rights and the PawnIO driver, CPU temperatures, CPU clocks and mainboard fans read
/// <see langword="null"/>; GPU sensors come from vendor APIs and work regardless.
/// </para>
/// </remarks>
internal sealed class LhmSensorSource(ILogger<LhmSensorSource> logger, TimeProvider timeProvider) : ISensorSource, IDisposable
{
    /// <summary>How long dispose waits for a running read before leaving the cleanup to it.</summary>
    private static readonly TimeSpan DisposeWait = TimeSpan.FromSeconds(1);

    private readonly MetricHealth _health = new("Hardware sensors (LibreHardwareMonitor)", logger, timeProvider);
    private readonly UpdateVisitor _updateVisitor = new();
    private readonly List<SensorReading> _readings = [];
    private readonly Lock _gate = new();
    private Computer? _computer;
    private bool _disposed;

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Third-party hardware code can throw anything; one bad read must not stop polling.")]
    public IReadOnlyList<SensorReading> ReadAll()
    {
        lock (_gate)
        {
            if (_disposed || (_computer is null && !_health.ShouldAttempt))
            {
                return [];
            }

            try
            {
                _computer ??= Open();
                _computer.Accept(_updateVisitor);

                _readings.Clear();
                foreach (var hardware in _computer.Hardware)
                {
                    Collect(hardware);
                }

                _health.ReportSuccess();
                return _readings.ToArray();
            }
            catch (Exception ex)
            {
                CloseComputer();
                _health.ReportFailure(ex.Message, ex);
                return [];
            }
            finally
            {
                if (Volatile.Read(ref _disposed))
                {
                    CloseComputer();
                }
            }
        }
    }

    public void Dispose()
    {
        Volatile.Write(ref _disposed, true);

        // Never close the Computer underneath a running read. If one is still going (e.g. a slow
        // first Open during shutdown), don't hold up app exit for it: it closes the Computer
        // itself when it finishes, because it checks _disposed on the way out.
        if (_gate.TryEnter(DisposeWait))
        {
            try
            {
                CloseComputer();
            }
            finally
            {
                _gate.Exit();
            }
        }
    }

    private static Computer Open()
    {
        var computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMotherboardEnabled = true,
        };
        computer.Open();
        return computer;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Closing is best effort during shutdown or recovery.")]
    private void CloseComputer()
    {
        try
        {
            _computer?.Close();
        }
        catch (Exception ex)
        {
            logger.SensorLibraryCloseFailed(ex);
        }

        _computer = null;
    }

    private void Collect(IHardware hardware, SensorHardwareKind? parentKind = null)
    {
        // Super I/O chips and embedded controllers are sub-hardware of the motherboard.
        var hardwareKind = parentKind ?? MapHardwareKind(hardware);
        var hardwareId = hardware.Identifier.ToString();

        foreach (var sensor in hardware.Sensors)
        {
            if (MapSensorKind(sensor.SensorType) is { } kind)
            {
                _readings.Add(new SensorReading(hardwareId, hardware.Name, hardwareKind, kind, sensor.Name, sensor.Value));
            }
        }

        foreach (var sub in hardware.SubHardware)
        {
            Collect(sub, hardwareKind);
        }
    }

    private static SensorHardwareKind MapHardwareKind(IHardware hardware) => hardware.HardwareType switch
    {
        HardwareType.Cpu => SensorHardwareKind.Cpu,
        HardwareType.GpuNvidia or HardwareType.GpuAmd => SensorHardwareKind.DiscreteGpu,
        // The library tags integrated Intel GPUs "/gpu-intel-integrated/..."; anything else is Arc.
        HardwareType.GpuIntel => hardware.Identifier.ToString().Contains("integrated", StringComparison.OrdinalIgnoreCase)
            ? SensorHardwareKind.IntegratedGpu
            : SensorHardwareKind.DiscreteGpu,
        HardwareType.Motherboard or HardwareType.SuperIO or HardwareType.EmbeddedController => SensorHardwareKind.Motherboard,
        _ => SensorHardwareKind.Other,
    };

    private static SensorKind? MapSensorKind(SensorType type) => type switch
    {
        SensorType.Temperature => SensorKind.Temperature,
        SensorType.Clock => SensorKind.Clock,
        SensorType.Load => SensorKind.Load,
        SensorType.Fan => SensorKind.Fan,
        SensorType.SmallData => SensorKind.SmallData,
        _ => null,
    };

    /// <summary>
    /// The library's visitor for refreshing every device and its sub-devices.
    /// </summary>
    private sealed class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer) => computer.Traverse(this);

        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (var sub in hardware.SubHardware)
            {
                sub.Accept(this);
            }
        }

        public void VisitSensor(ISensor sensor)
        {
        }

        public void VisitParameter(IParameter parameter)
        {
        }
    }
}
