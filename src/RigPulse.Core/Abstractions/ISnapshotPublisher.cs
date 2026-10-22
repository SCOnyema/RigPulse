using RigPulse.Core.Models;

namespace RigPulse.Core.Abstractions;

/// <summary>
/// Hands snapshots from the producer (the monitoring loop) to consumers (such as the UI).
/// </summary>
/// <remarks>
/// <see cref="Publish"/> may be called from any thread and must not block. Implementations decide
/// which thread raises <see cref="SnapshotPublished"/>; a UI implementation raises it on the UI
/// thread so subscribers can update bound state directly. Implementations may coalesce snapshots
/// when consumers fall behind, so subscribers should treat each event as "the latest state".
/// </remarks>
public interface ISnapshotPublisher
{
    /// <summary>
    /// Raised when a new snapshot is delivered to consumers.
    /// </summary>
    event EventHandler<SystemSnapshot>? SnapshotPublished;

    /// <summary>
    /// Publishes a snapshot to consumers without blocking the caller.
    /// </summary>
    /// <param name="snapshot">The snapshot to publish.</param>
    void Publish(SystemSnapshot snapshot);
}
