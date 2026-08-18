
namespace NightSafety.Core
{
    public static class HarasserConfrontationPolicy
    {
        public const int ApproachRadius = 8;

        public static bool IsConfronted(int distanceSquared, bool capablePlayerPawn, bool healthDecreased, bool lineOfSight = true)
        {
            return healthDecreased || (capablePlayerPawn && lineOfSight && distanceSquared <= ApproachRadius * ApproachRadius);
        }
    }

    public static class HarasserSchedulePolicy
    {
        public const int NightIntervalDays = 2;

        public static int NightKey(int year, int dayOfYear, float hour, float nightEndHour, int daysPerYear)
        {
            int localDay = checked((year * daysPerYear) + dayOfYear);
            return hour < nightEndHour ? localDay - 1 : localDay;
        }

        public static bool ShouldAttempt(int nightKey, int lastProcessedNightKey, int nextEligibleNightKey)
        {
            return nightKey != lastProcessedNightKey
                && (nextEligibleNightKey < 0 || nightKey >= nextEligibleNightKey);
        }

        public static bool CanAttempt(bool isNight, bool isPlayerHome, int freeColonistCount, bool hasActiveHarassers)
        {
            // The fixed cadence ignores storyteller and difficulty gates.
            return isNight && isPlayerHome && freeColonistCount > 0 && !hasActiveHarassers;
        }

        public static int NextEligibleAfterSuccess(int nightKey) => checked(nightKey + NightIntervalDays);

        public static int NextEligibleAfterFailure(int nightKey) => checked(nightKey + 1);
    }

    public static class HarassmentTargetPolicy
    {
        public static bool AllowsDestructiveTarget(bool spawned, bool sameMap, bool playerFaction,
            bool usesHitPoints, bool pawnOrCorpse, bool protectionOven, bool protectedCell,
            bool exterior, bool flammable, bool requireFlammable)
        {
            return spawned && sameMap && playerFaction && usesHitPoints && !pawnOrCorpse
                && !protectionOven && !protectedCell && exterior && (!requireFlammable || flammable);
        }
    }
}
