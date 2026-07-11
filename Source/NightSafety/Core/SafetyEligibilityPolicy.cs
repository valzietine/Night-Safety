
namespace NightSafety.Core
{
    public static class SafetyEligibilityPolicy
    {
        public static bool CanSeekSafety(bool spawned, bool playerFaction, bool drafted, bool downed,
            bool mentalState, bool prisoner, bool slave, bool playerForcedJob, bool behaviorEnabled,
            bool exposed, bool backoffActive)
        {
            return spawned && playerFaction && !drafted && !downed && !mentalState && !prisoner && !slave
                && !playerForcedJob && behaviorEnabled && exposed && !backoffActive;
        }
    }
}
