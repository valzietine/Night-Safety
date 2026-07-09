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

        static NightSafetyDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(NightSafetyDefOf));
    }
}
