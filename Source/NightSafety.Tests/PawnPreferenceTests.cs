using NightSafety.Core;
using Xunit;

namespace NightSafety.Tests;

public sealed class PawnPreferenceTests
{
    [Theory]
    [InlineData(true, false, true, true, true, true)]
    [InlineData(true, false, true, false, false, true)]
    [InlineData(true, false, true, true, false, true)]
    [InlineData(true, false, false, true, true, false)]
    [InlineData(true, true, true, false, false, false)]
    [InlineData(false, true, false, false, false, false)]
    public void PawnPreferenceRetentionSurvivesDespawnCaravanAndMapTransfer(bool hasReference,
        bool destroyed, bool playerFaction, bool spawned, bool onOwningMap, bool expected)
        => Assert.Equal(expected, PawnPreferenceLifecycle.ShouldRetain(
            hasReference, destroyed, playerFaction, spawned, onOwningMap));
}
