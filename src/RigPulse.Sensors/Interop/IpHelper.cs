using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RigPulse.Sensors.Interop;

/// <summary>
/// Minimal bindings for the IP Helper interface table (<c>GetIfTable2</c>).
/// </summary>
/// <remarks>
/// Used instead of <see cref="System.Net.NetworkInformation.NetworkInterface"/> because the managed
/// API lists every NDIS filter layer (WFP, QoS, VPN filters...) as a separate "Up" interface with
/// the same byte counters as the real adapter, so summing them multiplies the traffic. The native
/// table exposes the <c>HardwareInterface</c>/<c>FilterInterface</c> flags needed to avoid that,
/// and fetches every interface's counters in one cheap call.
/// See https://learn.microsoft.com/windows/win32/api/netioapi/nf-netioapi-getiftable2.
/// </remarks>
internal static unsafe partial class IpHelper
{
    public const uint IfOperStatusUp = 1;

    [LibraryImport("iphlpapi.dll")]
    private static partial uint GetIfTable2(out MibIfTable2* table);

    [LibraryImport("iphlpapi.dll")]
    private static partial void FreeMibTable(void* memory);

    /// <summary>
    /// Fetches every network interface with its counters. Dispose the result to free native memory.
    /// </summary>
    /// <returns>The Win32 error code; 0 on success.</returns>
    public static uint GetInterfaceTable(out InterfaceTable table)
    {
        Debug.Assert(sizeof(MibIfRow2) == 1352, "MibIfRow2 must match the native MIB_IF_ROW2 layout.");

        var error = GetIfTable2(out var native);
        table = error == 0 ? new InterfaceTable(native) : default;
        return error;
    }

    /// <summary>
    /// Owns a native <c>MIB_IF_TABLE2</c>; <see cref="Rows"/> is only valid until disposal.
    /// </summary>
    public readonly ref struct InterfaceTable
    {
        private readonly MibIfTable2* _table;

        internal InterfaceTable(MibIfTable2* table) => _table = table;

        public ReadOnlySpan<MibIfRow2> Rows =>
            _table is null ? default : new ReadOnlySpan<MibIfRow2>(&_table->Table, checked((int)_table->NumEntries));

        public void Dispose()
        {
            if (_table is not null)
            {
                FreeMibTable(_table);
            }
        }
    }

    [Flags]
    public enum InterfaceFlags : byte
    {
        None = 0,
        HardwareInterface = 1 << 0,
        FilterInterface = 1 << 1,
        ConnectorPresent = 1 << 2,
        NotAuthenticated = 1 << 3,
        NotMediaConnected = 1 << 4,
        Paused = 1 << 5,
        LowPower = 1 << 6,
        EndPointInterface = 1 << 7,
    }

    /// <summary>Managed mirror of <c>MIB_IF_TABLE2</c>; rows follow the header in memory.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MibIfTable2
    {
        public uint NumEntries;
        public MibIfRow2 Table; // first element of a variable-length array
    }

    /// <summary>Managed mirror of <c>MIB_IF_ROW2</c> (1352 bytes on x64).</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MibIfRow2
    {
        public ulong InterfaceLuid;
        public uint InterfaceIndex;
        public Guid InterfaceGuid;
        public InterfaceString Alias;
        public InterfaceString Description;
        public uint PhysicalAddressLength;
        public PhysicalAddress PhysicalAddress;
        public PhysicalAddress PermanentPhysicalAddress;
        public uint Mtu;
        public uint Type;
        public uint TunnelType;
        public uint MediaType;
        public uint PhysicalMediumType;
        public uint AccessType;
        public uint DirectionType;
        public InterfaceFlags Flags;
        public uint OperStatus;
        public uint AdminStatus;
        public uint MediaConnectState;
        public Guid NetworkGuid;
        public uint ConnectionType;
        public ulong TransmitLinkSpeed;
        public ulong ReceiveLinkSpeed;
        public ulong InOctets;
        public ulong InUcastPkts;
        public ulong InNUcastPkts;
        public ulong InDiscards;
        public ulong InErrors;
        public ulong InUnknownProtos;
        public ulong InUcastOctets;
        public ulong InMulticastOctets;
        public ulong InBroadcastOctets;
        public ulong OutOctets;
        public ulong OutUcastPkts;
        public ulong OutNUcastPkts;
        public ulong OutDiscards;
        public ulong OutErrors;
        public ulong OutUcastOctets;
        public ulong OutMulticastOctets;
        public ulong OutBroadcastOctets;
        public ulong OutQLen;
    }

    /// <summary>WCHAR[IF_MAX_STRING_SIZE + 1].</summary>
    [InlineArray(257)]
    public struct InterfaceString
    {
        private char _element;
    }

    /// <summary>UCHAR[IF_MAX_PHYS_ADDRESS_LENGTH].</summary>
    [InlineArray(32)]
    public struct PhysicalAddress
    {
        private byte _element;
    }
}
