using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace RigPulse.Core.Collections;

/// <summary>
/// A fixed-capacity, thread-safe circular buffer. Once full, each new item overwrites the oldest.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
/// <remarks>
/// All members are safe to call concurrently. Enumeration and <see cref="ToArray"/> work on a copy
/// taken under the lock, so a reader never observes a half-written buffer and never blocks writers
/// for longer than the copy takes.
/// </remarks>
[SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "\"Ring buffer\" is the established name for this data structure.")]
public sealed class RingBuffer<T> : IReadOnlyCollection<T>
{
    private readonly T[] _items;
    private readonly Lock _gate = new();
    private int _head; // index where the next item will be written
    private int _count;

    /// <summary>
    /// Initializes a new buffer that holds at most <paramref name="capacity"/> items.
    /// </summary>
    /// <param name="capacity">The maximum number of items retained. Must be positive.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is zero or negative.</exception>
    public RingBuffer(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _items = new T[capacity];
    }

    /// <summary>
    /// Gets the maximum number of items the buffer retains.
    /// </summary>
    public int Capacity => _items.Length;

    /// <summary>
    /// Gets the number of items currently stored.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_gate)
            {
                return _count;
            }
        }
    }

    /// <summary>
    /// Appends an item, evicting the oldest item if the buffer is full.
    /// </summary>
    /// <param name="item">The item to add.</param>
    public void Add(T item)
    {
        lock (_gate)
        {
            _items[_head] = item;
            _head = (_head + 1) % _items.Length;
            if (_count < _items.Length)
            {
                _count++;
            }
        }
    }

    /// <summary>
    /// Gets the most recently added item, if any.
    /// </summary>
    /// <param name="item">The newest item, or the default value when the buffer is empty.</param>
    /// <returns><see langword="true"/> if the buffer contained at least one item.</returns>
    public bool TryGetLatest(out T item)
    {
        lock (_gate)
        {
            if (_count == 0)
            {
                item = default!;
                return false;
            }

            item = _items[(_head - 1 + _items.Length) % _items.Length];
            return true;
        }
    }

    /// <summary>
    /// Removes all items.
    /// </summary>
    public void Clear()
    {
        lock (_gate)
        {
            Array.Clear(_items); // drop references so old items can be collected
            _head = 0;
            _count = 0;
        }
    }

    /// <summary>
    /// Copies the items into a new array, ordered from oldest to newest.
    /// </summary>
    /// <returns>A snapshot of the buffer's contents.</returns>
    public T[] ToArray()
    {
        lock (_gate)
        {
            if (_count == 0)
            {
                return [];
            }

            var result = new T[_count];
            var start = (_head - _count + _items.Length) % _items.Length;
            var firstPart = Math.Min(_count, _items.Length - start);

            Array.Copy(_items, start, result, 0, firstPart);
            Array.Copy(_items, 0, result, firstPart, _count - firstPart);
            return result;
        }
    }

    /// <summary>
    /// Returns an enumerator over a snapshot of the items, from oldest to newest.
    /// </summary>
    /// <returns>An enumerator that is unaffected by later writes.</returns>
    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)ToArray()).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
