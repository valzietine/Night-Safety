using System.Collections.Generic;
using System.Linq;
using NightSafety.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace NightSafety.Stemroot
{
    /// <summary>
    /// Lays down the starting stands of stemroot. Patches are seeded away from the player's
    /// landing spot and away from anything map generation has already claimed, then filled out
    /// as a solid core with a broken edge so a stand reads as a mass of roots rather than a
    /// speckle. Each patch runs along its own axis rather than growing as a disc, which is what
    /// gives the stands their length.
    /// </summary>
    public sealed class GenStep_Stemroot : GenStep
    {
        public FloatRange countPer10kCellsRange = new FloatRange(1.2f, 2.4f);
        public IntRange patchRadiusRange = new IntRange(4, 9);

        /// <summary>Share of the patch radius that fills solid before the edge starts breaking up.</summary>
        public float solidCoreFraction = 0.55f;

        /// <summary>How much longer than it is wide a patch runs. 1 is the old circular blob.</summary>
        public FloatRange patchStretchRange = new FloatRange(1.7f, 2.5f);

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

            // One axis and one stretch per patch, so every cell in it is measured against the
            // same ellipse and the stand comes out as a single run rather than a cloud of them.
            float axis = Rand.Range(0f, Mathf.PI);
            float stretch = patchStretchRange.RandomInRange;

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, useCenter: true))
            {
                if (!CanPlace(map, cell)) continue;

                float distance = StemrootPolicy.PatchDistance(
                    cell.x - center.x, cell.z - center.z, axis, stretch);
                if (Rand.Value >= StemrootPolicy.PatchFillChance(distance, radius, solidCoreFraction)) continue;

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
