using System.Collections.Generic;
using System.Linq;
using NightSafety.Buildings;
using NightSafety.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace NightSafety.Stemroot
{
    /// <summary>
    /// Owns the slow shifting of the stemroot mass: a few cells grown and a few receded per
    /// event, rather than a live creep. Every cell that opens or closes makes the game rebuild
    /// the pathing regions around it, so the budget is the whole point of doing it this way.
    /// </summary>
    public sealed class StemrootMapComponent : MapComponent
    {
        private int nextShiftTick = -1;

        // Terrain barely moves once a map is generated, so the density denominator is worked out
        // once and kept rather than rescanning every cell on every shift.
        private int growableCellCount = -1;

        public StemrootMapComponent(Map map) : base(map)
        {
        }

        private static StemrootConfigDef Config => NightSafetyDefOf.NightSafety_StemrootConfig;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextShiftTick, "nightSafetyStemrootNextShiftTick", -1);
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (!map.IsHashIntervalTick(250)) return;

            int now = Find.TickManager.TicksGame;
            if (nextShiftTick >= 0 && now < nextShiftTick) return;
            nextShiftTick = now + Mathf.Max(1, Config.shiftIntervalTicks);
            Shift(now);
        }

        private void Shift(int now)
        {
            List<Thing> existing = map.listerThings
                .ThingsOfDef(NightSafetyDefOf.NightSafety_Stemroot)
                .Where(thing => thing.Spawned)
                .ToList();
            if (existing.Count == 0) return;

            Recede(existing, now);
            Grow(existing, now);
        }

        private void Grow(List<Thing> existing, int now)
        {
            int target = StemrootPolicy.TargetCount(GrowableCellCount(), Config.densityFraction);
            int budget = StemrootPolicy.GrowBudget(existing.Count, target, Config.growCellsPerShift);
            if (budget <= 0) return;

            List<IntVec3> ovens = OvenCells();
            var candidates = new HashSet<IntVec3>();
            foreach (Thing stemroot in existing)
            {
                foreach (IntVec3 offset in GenAdj.CardinalDirections)
                {
                    IntVec3 cell = stemroot.Position + offset;
                    if (CanGrowInto(cell, ovens)) candidates.Add(cell);
                }
            }
            if (candidates.Count == 0) return;

            foreach (IntVec3 cell in PickDeterministically(candidates, budget, now))
            {
                foreach (Thing thing in cell.GetThingList(map).ToList())
                {
                    if (thing is Plant plant) plant.Destroy(DestroyMode.Vanish);
                }
                GenSpawn.Spawn(NightSafetyDefOf.NightSafety_Stemroot, cell, map);
            }
        }

        private void Recede(List<Thing> existing, int now)
        {
            int budget = StemrootPolicy.RecedeBudget(existing.Count, Config.recedeCellsPerShift);
            if (budget <= 0) return;

            // Anything a colonist has been told to work on is left alone, so a shift event cannot
            // quietly cancel an order that is already under way.
            var candidates = new HashSet<IntVec3>(existing
                .Where(thing => map.designationManager.AllDesignationsOn(thing).Count == 0)
                .Select(thing => thing.Position));
            if (candidates.Count == 0) return;

            foreach (IntVec3 cell in PickDeterministically(candidates, budget, now + 1))
            {
                Building edifice = cell.GetEdifice(map);
                if (edifice?.def == NightSafetyDefOf.NightSafety_Stemroot) edifice.Destroy(DestroyMode.Vanish);
            }
        }

        private bool CanGrowInto(IntVec3 cell, List<IntVec3> ovens)
        {
            if (!cell.InBounds(map)) return false;
            if (cell.GetEdifice(map) != null) return false;

            TerrainDef terrain = cell.GetTerrain(map);
            if (terrain == null || !terrain.affordances.Contains(TerrainAffordanceDefOf.Light)) return false;

            // The player's own ground is not the place for this. Home area plus the oven grace
            // is the promise: the safe zone can never be walled shut from outside.
            if (map.areaManager.Home[cell]) return false;
            foreach (IntVec3 oven in ovens)
            {
                if (StemrootPolicy.BlockedByOven(cell.DistanceToSquared(oven), OvenRadius(oven), Config.ovenGraceCells))
                    return false;
            }

            foreach (Thing thing in cell.GetThingList(map))
            {
                if (thing is Plant) continue;
                if (thing is Pawn) return false;
                if (thing.def.category == ThingCategory.Item) return false;
                if (thing.def.category == ThingCategory.Building) return false;
            }

            return true;
        }

        private List<IntVec3> OvenCells()
        {
            return map.listerThings.ThingsOfDef(NightSafetyDefOf.NightSafety_ProtectionOven)
                .Where(thing => thing.Spawned)
                .Select(thing => thing.Position)
                .ToList();
        }

        private float OvenRadius(IntVec3 cell)
        {
            Building edifice = cell.GetEdifice(map);
            CompProtectionOven? oven = edifice?.TryGetComp<CompProtectionOven>();
            return oven?.Radius ?? 0f;
        }

        private int GrowableCellCount()
        {
            // Water and hard rock are never stemroot, so the density target is measured against
            // the ground it could actually take rather than the whole map.
            if (growableCellCount >= 0) return growableCellCount;

            int count = 0;
            foreach (IntVec3 cell in map.AllCells)
            {
                TerrainDef terrain = cell.GetTerrain(map);
                if (terrain != null && terrain.affordances.Contains(TerrainAffordanceDefOf.Light)) count++;
            }
            growableCellCount = count;
            return count;
        }

        /// <summary>
        /// Stable pick: order the candidates by a hash of the cell and the shift tick and take
        /// the front of the list. No <c>Rand</c> state is touched, so two peers running the
        /// same tick shift the same cells.
        /// </summary>
        private IEnumerable<IntVec3> PickDeterministically(HashSet<IntVec3> candidates, int budget, int seed)
        {
            return candidates
                .OrderBy(cell => StemrootPolicy.UnitRoll(map.cellIndices.CellToIndex(cell), seed))
                .ThenBy(cell => map.cellIndices.CellToIndex(cell))
                .Take(budget)
                .ToList();
        }
    }
}
