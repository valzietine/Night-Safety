using RimWorld;
using Verse;
using Verse.AI;
using NightSafety.Core;

namespace NightSafety
{
    [DefOf]
    public static class NightSafetyDefOf
    {
        public static ThingDef NightSafety_ProtectionOven = null!;
        public static PawnKindDef NightSafety_ForestSpirit = null!;
        public static PawnKindDef NightSafety_Harasser = null!;
        public static FactionDef NightSafety_Harassers = null!;
        public static HediffDef NightSafety_HarasserState = null!;
        public static IncidentDef NightSafety_NightHarassersIncident = null!;
        public static DutyDef NightSafety_Harass = null!;
        public static JobDef NightSafety_HarassThrow = null!;
        public static JobDef NightSafety_HarassTheft = null!;
        public static JobDef NightSafety_BuildHarassmentEffigy = null!;
        public static ThingDef NightSafety_HarassmentEffigy = null!;
        public static HarassmentConfigDef NightSafety_HarassmentConfig = null!;

        static NightSafetyDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(NightSafetyDefOf));
    }
}
