using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RigPulse.Core.Abstractions;
using RigPulse.Sensors.Lhm;

namespace RigPulse.Sensors;

/// <summary>
/// Registers the Windows implementations of the RigPulse metric providers.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all metric providers as singletons. The container disposes them (and their
    /// performance counters and the hardware-monitoring library) when it is disposed.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddRigPulseSensors(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<ICpuProvider, CpuProvider>();
        services.AddSingleton<IMemoryProvider, MemoryProvider>();
        services.AddSingleton<IGpuProvider, GpuProvider>();
        services.AddSingleton<IDiskProvider, DiskProvider>();
        services.AddSingleton<INetworkProvider, NetworkProvider>();

        // Hardware sensors: one LibreHardwareMonitor instance for the whole app.
        services.AddSingleton<ISensorSource, LhmSensorSource>();
        services.AddSingleton<IHardwareSensorProvider, HardwareSensorProvider>();
        services.AddSingleton<IBatteryProvider, BatteryProvider>();
        services.AddSingleton<IHardwareAccess, HardwareAccess>();
        return services;
    }
}
