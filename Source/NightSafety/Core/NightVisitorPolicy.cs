
namespace NightSafety.Core
{
    public static class NightVisitorPolicy
    {
        // Trader/visitor/traveler arrivals are suppressed only while it is local night on a
        // player home map. Raids and every other incident are unaffected: the gate is applied
        // solely to the neutral-arrival incident workers via a workerClass swap. Daytime, and
        // any non-home target, always allow arrivals.
        public static bool SuppressesArrival(bool isPlayerHomeMap, bool isNight)
        {
            return isPlayerHomeMap && isNight;
        }
    }
}
