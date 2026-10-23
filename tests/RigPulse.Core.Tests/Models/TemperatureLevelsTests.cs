using RigPulse.Core.Models;

namespace RigPulse.Core.Tests.Models;

public sealed class TemperatureLevelsTests
{
    [Theory]
    [InlineData(30, LoadLevel.Normal)]
    [InlineData(69.9, LoadLevel.Normal)]
    [InlineData(70, LoadLevel.Elevated)]
    [InlineData(85, LoadLevel.Elevated)]
    [InlineData(85.1, LoadLevel.Critical)]
    [InlineData(100, LoadLevel.Critical)]
    [InlineData(double.NaN, LoadLevel.Normal)]
    public void FromCelsius_ClassifiesByThreshold(double celsius, LoadLevel expected)
    {
        TemperatureLevels.FromCelsius(celsius).ShouldBe(expected);
    }
}
