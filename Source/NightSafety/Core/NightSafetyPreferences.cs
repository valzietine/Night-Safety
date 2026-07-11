using Verse;

namespace NightSafety.Core
{
    public static class NightSafetyPreferences
    {
        // Keep the player mutation named and deterministic so compatibility layers can identify one narrow seam.
        public static void SetSafetySeekingEnabled(Pawn pawn, bool enabled)
        {
            if (pawn == null || Current.Game == null) return;
            Current.Game.GetComponent<NightSafetyGameComponent>()?.SetSafetySeekingEnabled(pawn, enabled);
        }
    }
}
