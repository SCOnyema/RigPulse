using System.Windows.Threading;
using RigPulse.Core.Abstractions;
using RigPulse.Core.Models;

namespace RigPulse.App.Services;

/// <summary>
/// Publishes snapshots onto the WPF UI thread.
/// </summary>
/// <remarks>
/// <see cref="Publish"/> never blocks: it stores the snapshot and, if no delivery is already
/// queued, schedules one with <see cref="Dispatcher.BeginInvoke(Delegate, DispatcherPriority, object[])"/>.
/// If the UI falls behind, older undelivered snapshots are replaced by the newest one, so the
/// dispatcher queue can never grow unbounded.
/// </remarks>
internal sealed class DispatcherSnapshotPublisher : ISnapshotPublisher
{
    private readonly Dispatcher _dispatcher;
    private readonly Action _deliver; // cached to avoid allocating a delegate per publish
    private SystemSnapshot? _pending;

    public DispatcherSnapshotPublisher(Dispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        _dispatcher = dispatcher;
        _deliver = Deliver;
    }

    /// <inheritdoc />
    /// <remarks>Raised on the UI thread.</remarks>
    public event EventHandler<SystemSnapshot>? SnapshotPublished;

    public void Publish(SystemSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        // Only the publish that finds no pending snapshot needs to schedule a delivery.
        if (Interlocked.Exchange(ref _pending, snapshot) is null && !_dispatcher.HasShutdownStarted)
        {
            _dispatcher.BeginInvoke(_deliver, DispatcherPriority.Background);
        }
    }

    private void Deliver()
    {
        if (Interlocked.Exchange(ref _pending, null) is { } snapshot)
        {
            SnapshotPublished?.Invoke(this, snapshot);
        }
    }
}
