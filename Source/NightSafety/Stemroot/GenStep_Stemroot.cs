using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace NightSafety.Stemroot
{
    /// <summary>
    /// Lays down the starting stands of stemroot. Patches are seeded away from the player's
    /// landing spot and away from anything map generation has already claimed, then filled out
    /// with a rough blob rather than a circle so the edges read as overgrowth.
    /// </summary>
    public sealed class GenStep_Stemroot : GenStep
    {
        public FloatRange countPer10kCellsRange = new FloatRange(1.2f, 2.4f);
        public IntRange patchRadiusRange = new IntRange(4, 9);
        public float patchDensity = 0.72f;
        public int playerStartClearance = 22;

        public override int SeedPart => 1476339021;

        public override void Generate(Map map, GenStepParams parms)
        {
            int patches = Mathf.RoundToInt(countPer10kCellsRange.RandomInRange * map.Area / 10000f);
            if (patches <= 0) return;

            IntVec3 playerStart = MapGenerator.PlayerStartSpot;
            for (int i = 0; i < patches; i++)
            {
                if (!TryFindPatchCenter(map, playerStart, out IntVec3 center)) continue;
                SpawnPatch(map, center);
            }
        }

        private bool TryFindPatchCenter(Map map, IntVec3 playerStart, out IntVec3 center)
        {
            return CellFinder.TryFindRandomCell(map, cell =>
                cell.DistanceToSquared(playerStart) > playerStartClearance * playerStartClearance
                && CanPlace(map, cell)
                && !MapGenerator.UsedRects.Any(rect => rect.Contains(cell)),
                out center);
        }

        private void SpawnPatch(Map map, IntVec3 center)
        {
            int radius = patchRadiusRange.RandomInRange;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, useCenter: true))
            {
                if (!CanPlace(map, cell)) continue;

                // Thin the patch out towards its edge so it does not come out as a disc.
                float distance = Mathf.Sqrt(cell.DistanceToSquared(center));
                float chance = patchDensity * (1f - (distance / (radius + 1f)));
                if (Rand.Value > chance) continue;

                foreach (Thing thing in cell.GetThingList(map).ToList())
                {
                    if (thing is Plant plant) plant.Destroy(DestroyMode.Vanish);
                }
                GenSpawn.Spawn(NightSafetyDefOf.NightSafety_Stemroot, cell, map);
            }
        }

        private static bool CanPlace(Map map, IntVec3 cell)
        {
            if (!cell.InBounds(map)) return false;
            if (cell.GetEdifice(map) != null) return false;

            TerrainDef terrain = cell.GetTerrain(map);
            if (terrain == null || !terrain.affordances.Contains(TerrainAffordanceDefOf.Light)) return false;

            foreach (Thing thing in cell.GetThingList(map))
            {
                if (thing is Plant) continue;
                if (thing.def.category == ThingCategory.Building) return false;
                if (thing.def.category == ThingCategory.Item) return false;
            }

            return true;
        }
    }
}
