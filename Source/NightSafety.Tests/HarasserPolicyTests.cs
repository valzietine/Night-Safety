using NightSafety.Core;
using Xunit;

namespace NightSafety.Tests;

public sealed class HarasserPolicyTests
{
    [Theory]
    [InlineData(64, true, false, true, true)]
    [InlineData(65, true, false, true, false)]
    [InlineData(1, true, false, false, false)]
    [InlineData(1, false, false, true, false)]
    [InlineData(999, false, true, false, true)]
    public void HarassersFleeOnCapableApproachOrDamage(int distanceSquared, bool capablePlayerPawn,
        bool healthDecreased, bool lineOfSight, bool expected)
        => Assert.Equal(expected, HarasserConfrontationPolicy.IsConfronted(
            distanceSquared, capablePlayerPawn, healthDecreased, lineOfSight));

    [Theory]
    [InlineData(5500, 10, 21f, 330010)]
    [InlineData(5500, 11, 2f, 330010)]
    [InlineData(5500, 11, 21f, 330011)]
    public void HarasserNightKeyTreatsPostMidnightAsThePreviousNight(int year, int day, float hour, int expected)
        => Assert.Equal(expected, HarasserSchedulePolicy.NightKey(year, day, hour, 6f, 60));

    [Theory]
    [InlineData(100, -1, -1, true)]
    [InlineData(100, 100, -1, false)]
    [InlineData(101, 100, 102, false)]
    [InlineData(102, 100, 102, true)]
    public void HarasserScheduleAttemptsOnlyOneEligibleObservationPerNight(
        int night, int lastProcessed, int nextEligible, bool expected)
        => Assert.Equal(expected, HarasserSchedulePolicy.ShouldAttempt(night, lastProcessed, nextEligible));

    [Theory]
    [InlineData(true, true, 1, false, true)]
    [InlineData(false, true, 1, false, false)]
    [InlineData(true, false, 1, false, false)]
    [InlineData(true, true, 0, false, false)]
    [InlineData(true, true, 1, true, false)]
    public void HarasserScheduleRequiresAnEligibleHomeMap(
        bool night, bool playerHome, int colonists, bool active, bool expected)
        => Assert.Equal(expected, HarasserSchedulePolicy.CanAttempt(night, playerHome, colonists, active));

    [Theory]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, false, false)]
    public void DestructiveTargetMustStillBeExteriorAndUnprotectedAtLaunch(
        bool protectedCell, bool exterior, bool expected)
        => Assert.Equal(expected, HarassmentTargetPolicy.AllowsDestructiveTarget(
            spawned: true, sameMap: true, playerFaction: true, usesHitPoints: true,
            pawnOrCorpse: false, protectionOven: false, protectedCell: protectedCell,
            exterior: exterior, flammable: true, requireFlammable: true));
}
