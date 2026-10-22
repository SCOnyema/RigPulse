using RigPulse.Core.Collections;

namespace RigPulse.Core.Tests.Collections;

public sealed class RingBufferTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_NonPositiveCapacity_Throws(int capacity)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new RingBuffer<int>(capacity));
    }

    [Fact]
    public void NewBuffer_IsEmpty()
    {
        var buffer = new RingBuffer<int>(3);

        buffer.Count.ShouldBe(0);
        buffer.Capacity.ShouldBe(3);
        buffer.ToArray().ShouldBeEmpty();
        buffer.TryGetLatest(out _).ShouldBeFalse();
    }

    [Fact]
    public void Add_BelowCapacity_KeepsItemsInInsertionOrder()
    {
        var buffer = new RingBuffer<int>(5);

        buffer.Add(1);
        buffer.Add(2);
        buffer.Add(3);

        buffer.Count.ShouldBe(3);
        buffer.ToArray().ShouldBe([1, 2, 3]);
    }

    [Fact]
    public void Add_BeyondCapacity_EvictsOldestFirst()
    {
        var buffer = new RingBuffer<int>(3);

        for (var i = 1; i <= 7; i++)
        {
            buffer.Add(i);
        }

        buffer.Count.ShouldBe(3);
        buffer.ToArray().ShouldBe([5, 6, 7]);
    }

    [Fact]
    public void Add_ExactlyCapacity_WrapsCleanly()
    {
        var buffer = new RingBuffer<int>(3);

        buffer.Add(1);
        buffer.Add(2);
        buffer.Add(3);

        buffer.ToArray().ShouldBe([1, 2, 3]);
        buffer.TryGetLatest(out var latest).ShouldBeTrue();
        latest.ShouldBe(3);
    }

    [Fact]
    public void TryGetLatest_AfterWrap_ReturnsNewestItem()
    {
        var buffer = new RingBuffer<string>(2);

        buffer.Add("a");
        buffer.Add("b");
        buffer.Add("c");

        buffer.TryGetLatest(out var latest).ShouldBeTrue();
        latest.ShouldBe("c");
    }

    [Fact]
    public void CapacityOfOne_AlwaysHoldsLastItem()
    {
        var buffer = new RingBuffer<int>(1);

        buffer.Add(1);
        buffer.Add(2);

        buffer.ToArray().ShouldBe([2]);
    }

    [Fact]
    public void Clear_RemovesEverything_AndBufferIsReusable()
    {
        var buffer = new RingBuffer<int>(3);
        buffer.Add(1);
        buffer.Add(2);
        buffer.Add(3);
        buffer.Add(4);

        buffer.Clear();

        buffer.Count.ShouldBe(0);
        buffer.ToArray().ShouldBeEmpty();

        buffer.Add(9);
        buffer.ToArray().ShouldBe([9]);
    }

    [Fact]
    public void Enumeration_IsSnapshot_UnaffectedByLaterWrites()
    {
        var buffer = new RingBuffer<int>(3);
        buffer.Add(1);
        buffer.Add(2);

        using var enumerator = buffer.GetEnumerator();
        buffer.Add(3);
        buffer.Add(4);

        var seen = new List<int>();
        while (enumerator.MoveNext())
        {
            seen.Add(enumerator.Current);
        }

        seen.ShouldBe([1, 2]);
        buffer.ShouldBe([2, 3, 4]);
    }

    [Fact]
    public async Task ConcurrentWritersAndReaders_KeepBufferConsistent()
    {
        const int Capacity = 60;
        const int Writers = 4;
        const int ItemsPerWriter = 10_000;
        var buffer = new RingBuffer<int>(Capacity);
        var cancellationToken = TestContext.Current.CancellationToken;

        var writers = Enumerable.Range(0, Writers).Select(w => Task.Run(
            () =>
            {
                for (var i = 0; i < ItemsPerWriter; i++)
                {
                    buffer.Add((w * ItemsPerWriter) + i);
                }
            },
            cancellationToken));

        var reader = Task.Run(
            () =>
            {
                for (var i = 0; i < 2_000; i++)
                {
                    buffer.ToArray().Length.ShouldBeLessThanOrEqualTo(Capacity);
                }
            },
            cancellationToken);

        await Task.WhenAll([.. writers, reader]);

        buffer.Count.ShouldBe(Capacity);
        var final = buffer.ToArray();
        final.Length.ShouldBe(Capacity);
        final.ShouldBeUnique();

        // Each writer's items must still appear in the order that writer added them.
        foreach (var group in final.GroupBy(x => x / ItemsPerWriter))
        {
            group.ShouldBeInOrder(SortDirection.Ascending);
        }
    }
}
