using NightSafety.Core;
using NightSafety.AI;
using Xunit;

namespace NightSafety.Tests;

public sealed class HarassmentThemePolicyTests
{
    [Theory]
    [InlineData((int)HarassmentTheme.Arson, true)]
    [InlineData((int)HarassmentTheme.Theft, true)]
    [InlineData(-1, false)]
    [InlineData(4, false)]
    public void HarassmentThemeValuesRemainBounded(int value, bool expected)
        => Assert.Equal(expected, HarassmentThemePolicy.IsValid(value));

    [Fact]
    public void RegroupDurationIsStableAndWithinConfiguredBounds()
    {
        int first = HarassmentThemePolicy.RegroupDuration(1200, 2400, 42, 60000);
        int second = HarassmentThemePolicy.RegroupDuration(1200, 2400, 42, 60000);
        Assert.Equal(first, second);
        Assert.InRange(first, 1200, 2400);
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(1, 3)]
    [InlineData(2, 4)]
    [InlineData(3, 5)]
    public void HarasserPackSizeMapsUniformOffsetsAcrossTwoToFive(int offset, int expected)
        => Assert.Equal(expected, HarassmentThemePolicy.SelectPackSize(offset));
}
