using NightSafety.Core;
using Xunit;

namespace NightSafety.Tests;

public sealed class SpiritOwnershipTests
{
    [Fact]
    public void SpiritOwnershipSelectsLowestStableIdAndHandlesEmptyCandidates()
    {
        Assert.Null(SpiritOwnershipPolicy.SelectOwnerId(System.Array.Empty<int>()));
        Assert.Equal(42, SpiritOwnershipPolicy.SelectOwnerId(new[] { 42 }));
        Assert.Equal(7, SpiritOwnershipPolicy.SelectOwnerId(new[] { 91, 7, 42 }));
    }
}
