using NightSafety.Core;
using Xunit;

namespace NightSafety.Tests;

public sealed class SafetyEligibilityTests
{
    [Theory]
    [InlineData(true, true, false, false, false, false, false, false, true, true, false, true)]
    [InlineData(true, true, true, false, false, false, false, false, true, true, false, false)]
    [InlineData(true, true, false, false, false, false, false, true, true, true, false, false)]
    [InlineData(true, true, false, false, false, false, false, false, false, true, false, false)]
    [InlineData(true, true, false, false, false, false, false, false, true, true, true, false)]
    public void PawnSafetyEligibilityHonorsControlAndOptOutBoundaries(bool spawned, bool playerFaction,
        bool drafted, bool downed, bool mentalState, bool prisoner, bool slave, bool forced, bool enabled,
        bool exposed, bool backoff, bool expected)
        => Assert.Equal(expected, SafetyEligibilityPolicy.CanSeekSafety(spawned, playerFaction, drafted, downed,
            mentalState, prisoner, slave, forced, enabled, exposed, backoff));
}
