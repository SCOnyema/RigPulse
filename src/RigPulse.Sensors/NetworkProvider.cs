using System.ComponentModel;
using Microsoft.Extensions.Logging;
using RigPulse.Core.Abstractions;
using RigPulse.Core.Models;
using RigPulse.Sensors.Internal;
using RigPulse.Sensors.Interop;

namespace RigPulse.Sensors;

/// <summary>
/// Computes network throughput from the byte counters of all connected physical network adapters.
/// </summary>
/// <remarks>
/// <para>
/// Only hardware interfaces that are up are counted. Loopback, tunnels, VPN adapters, Hyper-V
/// virtual switches and NDIS filter layers are skipped, because their traffic also passes through a
/// physical adapter and would otherwise be counted more than once.
/// </para>
/// <para>
/// Deltas are tracked per interface, so an adapter that connects or disconnects between polls
/// does not cause a spike. The first read establishes the baseline and returns 0.
/// </para>
/// </remarks>
internal sealed class NetworkProvider(ILogger<NetworkProvider> logger, TimeProvider timeProvider) : INetworkProvider
{
    private readonly MetricHealth _health = new("Network interface table", logger, timeProvider);
    private Dictionary<ulong, (ulong Sent, ulong Received)> _previous = [];
    private Dictionary<ulong, (ulong Sent, ulong Received)> _current = [];
    private long _previousTimestamp;
    private bool _hasBaseline;

    public NetworkStats? Read()
    {
        var error = IpHelper.GetInterfaceTable(out var table);
        if (error != 0)
        {
            _hasBaseline = false;
            _previous.Clear();
            _health.ReportFailure($"GetIfTable2 failed: {new Win32Exception((int)error).Message}");
            return null;
        }

        var timestamp = timeProvider.GetTimestamp();
        ulong sentDelta = 0;
        ulong receivedDelta = 0;
        _current.Clear();

        using (table)
        {
            foreach (ref readonly var row in table.Rows)
            {
                if (!IsPhysicalAdapterUp(in row))
                {
                    continue;
                }

                var sample = (Sent: row.OutOctets, Received: row.InOctets);
                _current[row.InterfaceLuid] = sample;

                // Counters only go backwards if the adapter was reset; count that as no traffic.
                if (_previous.TryGetValue(row.InterfaceLuid, out var previous))
                {
                    sentDelta += sample.Sent >= previous.Sent ? sample.Sent - previous.Sent : 0;
                    receivedDelta += sample.Received >= previous.Received ? sample.Received - previous.Received : 0;
                }
            }
        }

        var elapsedSeconds = _hasBaseline ? timeProvider.GetElapsedTime(_previousTimestamp, timestamp).TotalSeconds : 0;

        (_previous, _current) = (_current, _previous);
        _previousTimestamp = timestamp;
        _hasBaseline = true;
        _health.ReportSuccess();

        return elapsedSeconds > 0
            ? new NetworkStats(sentDelta / elapsedSeconds, receivedDelta / elapsedSeconds)
            : new NetworkStats(0, 0);
    }

    private static bool IsPhysicalAdapterUp(in IpHelper.MibIfRow2 row) =>
        row.OperStatus == IpHelper.IfOperStatusUp
        && row.Flags.HasFlag(IpHelper.InterfaceFlags.HardwareInterface)
        && !row.Flags.HasFlag(IpHelper.InterfaceFlags.FilterInterface);
}
