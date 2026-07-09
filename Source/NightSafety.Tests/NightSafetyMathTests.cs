using NightSafety.Core;
using System;
using Xunit;

namespace NightSafety.Tests;

public sealed class NightSafetyMathTests
{
    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(12, 0, true)]
    [InlineData(12, 1, false)]
    [InlineData(8, 8, true)]
    [InlineData(9, 9, false)]
    public void CircleUsesInclusiveSquaredRadius(int x, int z, bool expected)
        => Assert.Equal(expected, NightSafetyMath.IsWithinRadius(x, z, 0, 0, 12f));

    [Theory]
    [InlineData(19.99f, false)]
    [InlineData(20f, true)]
    [InlineData(0f, true)]
    [InlineData(5.99f, true)]
    [InlineData(6f, false)]
    public void WrappedNightBoundaryIsStartInclusiveEndExclusive(float hour, bool expected)
        => Assert.Equal(expected, NightSafetyMath.IsNight(hour, 20f, 6f));

    [Fact]
    public void StableCandidateOrderingUsesIdToBreakDistanceTie()
        => Assert.True(NightSafetyMath.CompareCandidates(4f, 10, 4f, 11) < 0);
}
