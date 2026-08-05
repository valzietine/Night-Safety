using NightSafety.Core;
using Xunit;

namespace NightSafety.Tests;

// Containment stops an eligible pawn from selecting work outside the safe ring at night.
// JobGiver_SeekProtection is unchanged and still retrieves anyone already outside when night
// lands. These tests cover only the pure decision.
public sealed class NightContainmentPolicyTests
{
    private const bool Night = true;
    private const bool Day = false;
    private const bool ZoneReady = true;
    private const bool Managed = true;
    private const bool Unmanaged = false;

    private static bool Eligible(bool spawned = true, bool playerFaction = true, bool downed = false,
        bool prisoner = false, bool slave = false, bool behaviorEnabled = true, bool respectsAllowedArea = true)
        => NightContainmentPolicy.CanContain(spawned, playerFaction, downed, prisoner, slave,
            behaviorEnabled, respectsAllowedArea);

    private static ContainmentAction Decide(bool isNight, bool zoneAvailable, bool managed, bool eligible,
        bool drafted = false, bool playerForcedJob = false, bool mentalState = false, bool playerOverride = false)
        => NightContainmentPolicy.Decide(isNight, zoneAvailable, managed, eligible, drafted, playerForcedJob,
            mentalState, playerOverride);

    // --- Durable eligibility -------------------------------------------------

    [Fact]
    public void OrdinaryFreeColonistIsContainable()
        => Assert.True(Eligible());

    [Theory]
    [InlineData(false, true, false, false, false, true, true)]  // despawned
    [InlineData(true, false, false, false, false, true, true)]  // not player faction
    [InlineData(true, true, true, false, false, true, true)]    // downed
    [InlineData(true, true, false, true, false, true, true)]    // prisoner
    [InlineData(true, true, false, false, true, true, true)]    // slave
    [InlineData(true, true, false, false, false, false, true)]  // opted out in the Assign table
    [InlineData(true, true, false, false, false, true, false)]  // vanilla RespectsAllowedArea is false
    public void ExcludedPawnsAreNotContainable(bool spawned, bool playerFaction, bool downed,
        bool prisoner, bool slave, bool behaviorEnabled, bool respectsAllowedArea)
        => Assert.False(Eligible(spawned, playerFaction, downed, prisoner, slave, behaviorEnabled, respectsAllowedArea));

    // The Assign-table checkbox is the single switch for both halves of the feature; there is no
    // second containment-only toggle.
    [Fact]
    public void AssignTableOptOutDisablesContainment()
        => Assert.False(Eligible(behaviorEnabled: false));

    // --- Dusk / dawn ---------------------------------------------------------

    [Fact]
    public void EligiblePawnIsContainedAtDusk()
        => Assert.Equal(ContainmentAction.Apply, Decide(Night, ZoneReady, Unmanaged, eligible: true));

    [Fact]
    public void ContainedPawnIsReleasedAtDawn()
        => Assert.Equal(ContainmentAction.Release, Decide(Day, ZoneReady, Managed, eligible: true));

    [Fact]
    public void DaytimeDoesNotContain()
        => Assert.Equal(ContainmentAction.None, Decide(Day, ZoneReady, Unmanaged, eligible: true));

    [Fact]
    public void ApplyIsIdempotentOnceManaged()
        => Assert.Equal(ContainmentAction.None, Decide(Night, ZoneReady, Managed, eligible: true));

    [Fact]
    public void ReleaseIsIdempotentOnceUnmanaged()
        => Assert.Equal(ContainmentAction.None, Decide(Day, ZoneReady, Unmanaged, eligible: true));

    // --- Slot-cap fallback ---------------------------------------------------

    // AreaManager.MaxAllowedAreas is 10. With no free slot there is no zone to assign, so the mod
    // falls back to recall-only rather than silently doing nothing to a pawn it claims to manage.
    [Fact]
    public void NoAvailableZoneMeansNoContainment()
        => Assert.Equal(ContainmentAction.None, Decide(Night, zoneAvailable: false, managed: Unmanaged, eligible: true));

    [Fact]
    public void LosingTheZoneMidNightReleasesManagedPawns()
        => Assert.Equal(ContainmentAction.Release, Decide(Night, zoneAvailable: false, managed: Managed, eligible: true));

    // --- Player intent -------------------------------------------------------

    // If the player re-zones a pawn by hand mid-night, their choice stands for the rest of the
    // night. Yield drops our bookkeeping WITHOUT restoring, so dawn cannot clobber what they set.
    [Fact]
    public void ManualRezoningMidNightYields()
        => Assert.Equal(ContainmentAction.Yield, Decide(Night, ZoneReady, Managed, eligible: true, playerOverride: true));

    [Fact]
    public void YieldOutranksRelease()
        => Assert.Equal(ContainmentAction.Yield, Decide(Day, ZoneReady, Managed, eligible: true, playerOverride: true));

    [Fact]
    public void OverrideOnAnUnmanagedPawnIsNotOurBusiness()
        => Assert.Equal(ContainmentAction.None, Decide(Night, ZoneReady, Unmanaged, eligible: true, playerOverride: true));

    // --- Transient states defer, they do not release -------------------------

    // Vanilla ignores allowed areas while drafted and lets right-click orders target outside the
    // area, so mutating assignment in these states only picks a fight with the player. Deferring
    // leaves an already-managed pawn managed, so dawn still restores correctly.
    [Theory]
    [InlineData(true, false, false)]   // drafted
    [InlineData(false, true, false)]   // player-forced job
    [InlineData(false, false, true)]   // mental break
    public void TransientStatesDeferApply(bool drafted, bool playerForcedJob, bool mentalState)
        => Assert.Equal(ContainmentAction.None,
            Decide(Night, ZoneReady, Unmanaged, eligible: true, drafted: drafted,
                playerForcedJob: playerForcedJob, mentalState: mentalState));

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public void TransientStatesDoNotReleaseAManagedPawn(bool drafted, bool playerForcedJob, bool mentalState)
        => Assert.Equal(ContainmentAction.None,
            Decide(Night, ZoneReady, Managed, eligible: true, drafted: drafted,
                playerForcedJob: playerForcedJob, mentalState: mentalState));

    // Dawn restore must win over a transient state, otherwise a pawn drafted across dawn keeps the
    // night zone until the next time it happens to be undrafted during a scheduled pass.
    [Fact]
    public void DawnReleaseOutranksTransientDeferral()
        => Assert.Equal(ContainmentAction.Release, Decide(Day, ZoneReady, Managed, eligible: true, drafted: true));

    // --- Durable ineligibility releases --------------------------------------

    [Fact]
    public void BecomingIneligibleMidNightRestoresTheOriginalArea()
        => Assert.Equal(ContainmentAction.Release, Decide(Night, ZoneReady, Managed, eligible: false));

    [Fact]
    public void IneligiblePawnIsNeverContained()
        => Assert.Equal(ContainmentAction.None, Decide(Night, ZoneReady, Unmanaged, eligible: false));
}
