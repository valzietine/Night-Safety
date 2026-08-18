using Verse;

namespace NightSafety.Core
{
    // Central XML-tunable configuration for Night Safety gameplay values. Keeping these
    // in a def means balance can be tuned from XML without a recompile. The effigy material
    // cost is not repeated here; it comes straight from the NightSafety_HarassmentEffigy
    // ThingDef's costStuffCount.
    public sealed class HarassmentConfigDef : Def
    {
        // Harasser throwing and effigy work.
        public ThingDef arsonProjectile = null!;
        public ThingDef bombardmentProjectile = null!;
        public float minThrowRange = 4f;
        public float maxThrowRange = 12f;
        public int throwWarmupTicks = 90;
        public int regroupMinTicks = 1200;
        public int regroupMaxTicks = 2400;
        public int effigyWorkTicks = 900;

        // Night boundary (map-local hours). Night runs [nightStartHour, nightEndHour).
        public float nightStartHour = 20f;
        public float nightEndHour = 6f;

        // Colonist safety-seeking pathfinding backoff after a failed path.
        public int safetyPathRetryTicks = 600;

        // Harasser incident spawn geometry.
        public float harassmentPointRadius = 22f;
        public float harassmentPointMinDistance = 18f;
        public float spawnClosewalkRadius = 5f;

        // Harasser job timings and geometry.
        public int breachJobExpiryTicks = 600;
        public int throwJobExpiryTicks = 1200;
        public int regroupWaitTicks = 180;
        public float regroupReturnDistance = 5f;
        public int theftRecheckIntervalTicks = 120;
        public float effigySearchRadius = 8f;

        // Forest Spirit hunt job timings.
        public int spiritHuntWaitTicks = 180;
        public int spiritAttackExpiryTicks = 120;
    }
}
