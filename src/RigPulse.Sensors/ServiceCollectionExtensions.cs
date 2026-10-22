using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RigPulse.Core.Abstractions;

namespace RigPulse.Sensors;

/// <summary>
/// Registers the Windows implementations of the RigPulse metric providers.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all metric providers as singletons. The container disposes them (and their
    /// performance counters) when it is disposed.
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
        return services;
    }
}
