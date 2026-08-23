using RimWorld;
using UnityEngine;
using Verse;

namespace NightSafety.Stemroot
{
    /// <summary>Shared bookkeeping for cutting and harvesting stemroot.</summary>
    public static class StemrootWork
    {
        /// <summary>
        /// Pushes the forest affliction up by a step. A pawn who does not have it yet starts at
        /// the step rather than at the Def's initial severity, so one cut costs one cut's worth
        /// rather than the harasser starting dose.
        /// </summary>
        public static void ApplyAffliction(Pawn? pawn, float severity)
        {
            if (pawn?.health == null || severity <= 0f) return;

            HediffDef def = NightSafetyDefOf.NightSafety_ForestAffliction;
            Hediff? existing = pawn.health.hediffSet.GetFirstHediffOfDef(def);
            if (existing != null)
            {
                existing.Severity += severity;
                return;
            }

            Hediff added = HediffMaker.MakeHediff(def, pawn);
            added.Severity = severity;
            pawn.health.AddHediff(added);
        }

        public static void DropYield(ThingDef? def, int count, IntVec3 cell, Map? map, Pawn? actor)
        {
            if (def == null || map == null || count <= 0) return;

            int remaining = count;
            while (remaining > 0)
            {
                int stack = Mathf.Min(remaining, Mathf.Max(1, def.stackLimit));
                Thing thing = ThingMaker.MakeThing(def);
                thing.stackCount = stack;
                if (actor?.Faction != Faction.OfPlayer) thing.SetForbidden(true, false);
                GenPlace.TryPlaceThing(thing, cell, map, ThingPlaceMode.Near);
                remaining -= stack;
            }
        }
    }
}
