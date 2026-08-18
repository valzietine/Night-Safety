
namespace NightSafety.Core
{
    public static class PawnPreferenceLifecycle
    {
        public static bool ShouldRetain(bool hasReference, bool destroyed, bool playerFaction, bool spawned, bool onOwningMap)
        {
            // Spawn and map state do not matter here: caravans, pods, and transfers must keep their preferences.
            return hasReference && !destroyed && playerFaction;
        }
    }
}
