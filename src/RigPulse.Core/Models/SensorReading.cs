namespace RigPulse.Core.Models;

/// <summary>
/// The kind of device a <see cref="SensorReading"/> belongs to.
/// </summary>
public enum SensorHardwareKind
{
    /// <summary>Anything not listed below, e.g. a USB fan controller.</summary>
    Other,

    /// <summary>A processor package.</summary>
    Cpu,

    /// <summary>A graphics card with its own memory (NVIDIA, AMD Radeon, Intel Arc).</summary>
    DiscreteGpu,

    /// <summary>A GPU built into the processor that shares system memory.</summary>
    IntegratedGpu,

    /// <summary>The mainboard, its Super I/O chip or embedded controller.</summary>
    Motherboard,
}

/// <summary>
/// What a <see cref="SensorReading"/> measures, and therefore its unit.
/// </summary>
public enum SensorKind
{
    /// <summary>Degrees Celsius.</summary>
    Temperature,

    /// <summary>Megahertz.</summary>
    Clock,

    /// <summary>Percent, from 0 to 100.</summary>
    Load,

    /// <summary>Revolutions per minute.</summary>
    Fan,

    /// <summary>Megabytes (binary: 1 MB = 1024 × 1024 bytes), e.g. GPU memory.</summary>
    SmallData,
}

/// <summary>
/// One raw hardware sensor value, independent of the library that read it.
/// </summary>
/// <param name="HardwareId">A stable identifier of the device, unique on this machine.</param>
/// <param name="HardwareName">The device's display name, e.g. "NVIDIA GeForce RTX 4070".</param>
/// <param name="HardwareKind">The kind of device.</param>
/// <param name="Kind">What the sensor measures.</param>
/// <param name="Name">The sensor's name as reported by the hardware library, e.g. "CPU Package".</param>
/// <param name="Value">The current value in the unit implied by <paramref name="Kind"/>, or <see langword="null"/> if the sensor could not be read.</param>
public sealed record SensorReading(
    string HardwareId,
    string HardwareName,
    SensorHardwareKind HardwareKind,
    SensorKind Kind,
    string Name,
    double? Value);
