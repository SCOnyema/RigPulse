using RigPulse.Core.Models;

namespace RigPulse.Core.Tests.Models;

public sealed class MemoryStatsTests
{
    [Theory]
    [InlineData(0UL, 16UL, 0)]
    [InlineData(4UL, 16UL, 25)]
    [InlineData(16UL, 16UL, 100)]
    public void UsagePercent_IsUsedShareOfTotal(ulong used, ulong total, double expected)
    {
        new MemoryStats(used, total).UsagePercent.ShouldBe(expected, tolerance: 1e-9);
    }

    [Fact]
    public void UsagePercent_ZeroTotal_IsZero()
    {
        new MemoryStats(0, 0).UsagePercent.ShouldBe(0);
    }
}
