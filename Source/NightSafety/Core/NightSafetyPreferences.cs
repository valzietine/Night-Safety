using Verse;

namespace NightSafety.Core
{
    public static class NightSafetyPreferences
    {
        // Keep the mutation named and deterministic so other mods can find and wrap this one call.
        public static void SetSafetySeekingEnabled(Pawn pawn, bool enabled)
        {
            if (pawn == null || Current.Game == null) return;
            Current.Game.GetComponent<NightSafetyGameComponent>()?.SetSafetySeekingEnabled(pawn, enabled);
        }
    }
}
