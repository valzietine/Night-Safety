
namespace NightSafety.Core
{
    public static class PawnPreferenceLifecycle
    {
        public static bool ShouldRetain(bool hasReference, bool destroyed, bool playerFaction, bool spawned, bool onOwningMap)
        {
            // Spawn and map state are deliberately irrelevant: caravans, pods, and transfers must retain preferences.
            return hasReference && !destroyed && playerFaction;
        }
    }
}
