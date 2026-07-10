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

        static NightSafetyDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(NightSafetyDefOf));
    }
}
