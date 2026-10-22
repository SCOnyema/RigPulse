using RigPulse.Core.Models;

namespace RigPulse.Core.Tests.Models;

public sealed class LoadLevelsTests
{
    [Theory]
    [InlineData(0, LoadLevel.Normal)]
    [InlineData(69.9, LoadLevel.Normal)]
    [InlineData(70, LoadLevel.Elevated)]
    [InlineData(85, LoadLevel.Elevated)]
    [InlineData(90, LoadLevel.Elevated)]
    [InlineData(90.1, LoadLevel.Critical)]
    [InlineData(100, LoadLevel.Critical)]
    [InlineData(double.NaN, LoadLevel.Normal)]
    public void FromPercent_ClassifiesByThreshold(double percent, LoadLevel expected)
    {
        LoadLevels.FromPercent(percent).ShouldBe(expected);
    }
}
