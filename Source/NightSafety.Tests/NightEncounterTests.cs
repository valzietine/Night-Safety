using NightSafety.Core;
using Xunit;

namespace NightSafety.Tests;

public sealed class NightEncounterTests
{
    [Theory]
    [InlineData(NightEncounterPhase.Active, false, NightEncounterPhase.Inactive)]
    [InlineData(NightEncounterPhase.Active, true, NightEncounterPhase.Active)]
    [InlineData(NightEncounterPhase.Inactive, false, NightEncounterPhase.Inactive)]
    public void MissingOwnerRepairsToInactive(NightEncounterPhase phase, bool hasOwner, NightEncounterPhase expected)
        => Assert.Equal(expected, NightEncounterTransitions.Repair(phase, hasOwner));

    [Fact]
    public void DawnTransitionIsIdempotent()
    {
        NightEncounterPhase once = NightEncounterTransitions.AtDawn(NightEncounterPhase.Active);
        Assert.Equal(once, NightEncounterTransitions.AtDawn(once));
    }

    [Theory]
    [InlineData(true, true, true, true)]
    [InlineData(true, false, true, false)]
    [InlineData(true, true, false, false)]
    [InlineData(false, false, false, false)]
    public void EncounterOwnerMustRemainSpawnedOnItsOwningMap(bool hasReference, bool spawned, bool onMap, bool expected)
        => Assert.Equal(expected, NightEncounterTransitions.HasActiveOwner(hasReference, spawned, onMap));
}
