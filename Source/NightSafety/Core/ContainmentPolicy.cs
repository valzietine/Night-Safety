
namespace NightSafety.Core
{
    public enum ContainmentAction
    {
        /// <summary>Leave the pawn's allowed-area assignment alone.</summary>
        None,

        /// <summary>Stash the pawn's current assignment and restrict it to the night safe zone.</summary>
        Apply,

        /// <summary>Restore the assignment stashed by <see cref="Apply"/>.</summary>
        Release,

        /// <summary>Forget the pawn without restoring; the player re-zoned it and their choice stands.</summary>
        Yield
    }

    /// <summary>
    /// Night containment restricts eligible pawns to a mod-owned allowed area covering the active
    /// oven radii, so vanilla job selection stops offering work outside the ring at night.
    /// <c>JobGiver_SeekProtection</c> still handles pawns already outside when night lands.
    /// </summary>
    public static class NightContainmentPolicy
    {
        /// <summary>
        /// Durable properties that decide whether a pawn is ours to manage at all. Transient states
        /// belong in <see cref="Decide"/>, because they must defer a mutation rather than release one.
        /// <paramref name="respectsAllowedArea"/> mirrors vanilla <c>Pawn_PlayerSettings.RespectsAllowedArea</c>,
        /// which already excludes lorded (caravan/quest) pawns, guests, and area-control-disabled races.
        /// </summary>
        public static bool CanContain(bool spawned, bool playerFaction, bool downed, bool prisoner,
            bool slave, bool behaviorEnabled, bool respectsAllowedArea)
        {
            return spawned && playerFaction && !downed && !prisoner && !slave
                && behaviorEnabled && respectsAllowedArea;
        }

        public static ContainmentAction Decide(bool isNight, bool zoneAvailable, bool managed,
            bool eligible, bool drafted, bool playerForcedJob, bool mentalState, bool playerOverride)
        {
            // The player re-zoned a pawn we had taken over. Drop it without restoring, so dawn
            // cannot clobber the assignment they just chose.
            if (playerOverride) return managed ? ContainmentAction.Yield : ContainmentAction.None;

            // Restoring outranks the transient deferral below: a pawn drafted across dawn must
            // still get its own area back, not keep the night zone until it happens to be
            // undrafted during some later scheduled pass.
            if (managed && (!isNight || !eligible || !zoneAvailable)) return ContainmentAction.Release;

            // Vanilla ignores allowed areas while drafted, and right-click orders may target cells
            // outside the area, so mutating assignment in these states only fights the player.
            // Deferring leaves an already-managed pawn managed, keeping dawn restore correct.
            if (drafted || playerForcedJob || mentalState) return ContainmentAction.None;

            return !managed && isNight && eligible && zoneAvailable
                ? ContainmentAction.Apply
                : ContainmentAction.None;
        }
    }
}
