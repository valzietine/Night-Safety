using RimWorld;
using Verse;
using NightSafety.Core;

namespace NightSafety.Incidents
{
    // Night-visitor suppression. These workers extend the vanilla neutral-arrival workers and add
    // a single gate in front of the vanilla eligibility check: while it is local night on a player
    // home map, trader/visitor/traveler arrivals do not fire. Everything else (raids, other events)
    // is untouched because only these three incidents have their workerClass swapped (see
    // Patches/NightVisitorSuppression_NightSafety.xml). Using worker subclasses keeps the core
    // Harmony-free, consistent with the mod's API-free design.
    internal static class NightArrivalGate
    {
        public static bool BlockedNow(IncidentParms parms)
        {
            if (!(parms.target is Map map)) return false;
            NightSafetyMapComponent component = map.GetComponent<NightSafetyMapComponent>();
            return NightVisitorPolicy.SuppressesArrival(map.IsPlayerHome, component?.IsNight == true);
        }
    }

    public sealed class IncidentWorker_NightGatedTraderCaravan : IncidentWorker_TraderCaravanArrival
    {
        protected override bool CanFireNowSub(IncidentParms parms)
            => !NightArrivalGate.BlockedNow(parms) && base.CanFireNowSub(parms);
    }

    public sealed class IncidentWorker_NightGatedVisitorGroup : IncidentWorker_VisitorGroup
    {
        protected override bool CanFireNowSub(IncidentParms parms)
            => !NightArrivalGate.BlockedNow(parms) && base.CanFireNowSub(parms);
    }

    public sealed class IncidentWorker_NightGatedTravelerGroup : IncidentWorker_TravelerGroup
    {
        protected override bool CanFireNowSub(IncidentParms parms)
            => !NightArrivalGate.BlockedNow(parms) && base.CanFireNowSub(parms);
    }
}
