
namespace NightSafety.Core
{
    public enum NightEncounterPhase
    {
        Inactive,
        Active,
        Retreating
    }

    public static class NightEncounterTransitions
    {
        public static bool HasActiveOwner(bool hasReference, bool spawned, bool onOwningMap)
        {
            return hasReference && spawned && onOwningMap;
        }

        public static NightEncounterPhase Repair(NightEncounterPhase phase, bool hasOwner)
        {
            return hasOwner ? phase : NightEncounterPhase.Inactive;
        }

        public static NightEncounterPhase AtDawn(NightEncounterPhase phase)
        {
            return phase == NightEncounterPhase.Active ? NightEncounterPhase.Retreating : phase;
        }
    }
}
