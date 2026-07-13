using System.Collections.Generic;
using System.Linq;
using NightSafety.Lords;
using NightSafety.AI;
using NightSafety.Core;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace NightSafety.Incidents
{
    public sealed class IncidentWorker_NightHarassers : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return parms.target is Map map && map.IsPlayerHome && map.mapPawns.FreeColonistsSpawnedCount > 0
                && map.GetComponent<NightSafetyMapComponent>().IsNight
                && !map.GetComponent<NightSafetyMapComponent>().HasActiveHarassers;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!(parms.target is Map map)) return false;
            NightSafetyMapComponent component = map.GetComponent<NightSafetyMapComponent>();
            HarassmentConfigDef config = NightSafetyDefOf.NightSafety_HarassmentConfig;
            if (!map.IsPlayerHome || map.mapPawns.FreeColonistsSpawnedCount == 0) return false;
            if (!component.IsNight || component.HasActiveHarassers) return false;

            // Deterministic generation: seed every random draw (edge cell, faction, pack size,
            // spawn cells, theme, effigy stuff) from stable map-local state, so a shared-map
            // RimWorld Together session produces the same pack on every peer for a given night
            // instead of diverging on ambient RNG.
            int harasserSeed = Gen.HashCombineInt(Gen.HashCombineInt(map.Tile, GenLocalDate.Year(map)), GenLocalDate.DayOfYear(map));
            Rand.PushState(harasserSeed);
            try
            {
            if (!CellFinder.TryFindRandomEdgeCellWith(c => c.Standable(map), map, CellFinder.EdgeRoadChance_Hostile, out IntVec3 center))
                return false;
            Faction faction = GetOrCreateHarasserFaction();

            // This is ambient pressure, not a raid: vary a small cohesive pack independently of storyteller points.
            int sizeOffset = Rand.Range(0, HarassmentThemePolicy.MaximumPackSize - HarassmentThemePolicy.MinimumPackSize + 1);
            int count = HarassmentThemePolicy.SelectPackSize(sizeOffset);
            var pawns = new List<Pawn>(count);
            for (int i = 0; i < count; i++)
            {
                Pawn pawn = PawnGenerator.GeneratePawn(NightSafetyDefOf.NightSafety_Harasser, faction);
                IntVec3 spawnCell = CellFinder.RandomClosewalkCellNear(center, map, (int)config.spawnClosewalkRadius);
                GenSpawn.Spawn(pawn, spawnCell, map);
                pawns.Add(pawn);
            }

            IntVec3 colonyAnchor = map.mapPawns.FreeColonistsSpawned
                .OrderBy(pawn => pawn.thingIDNumber)
                .Select(pawn => pawn.Position)
                .First();
            IntVec3 harassmentPoint = GenRadial.RadialCellsAround(colonyAnchor, config.harassmentPointRadius, true)
                .Where(cell => cell.InBounds(map)
                    && colonyAnchor.DistanceToSquared(cell) >= config.harassmentPointMinDistance * config.harassmentPointMinDistance
                    && cell.Standable(map)
                    && map.reachability.CanReach(center, cell, PathEndMode.OnCell, TraverseParms.For(TraverseMode.PassDoors)))
                .OrderBy(cell => map.cellIndices.CellToIndex(cell))
                .DefaultIfEmpty(IntVec3.Invalid)
                .First();
            if (!harassmentPoint.IsValid) harassmentPoint = center;
            // Draw across all defined themes; deriving the count from the enum keeps the
            // distribution correct if a theme is ever added or removed.
            HarassmentTheme theme = (HarassmentTheme)Rand.Range(0, System.Enum.GetValues(typeof(HarassmentTheme)).Length);
            ThingDef? effigyStuff = theme == HarassmentTheme.Effigy ? SelectEffigyStuff() : null;
            foreach (Pawn pawn in pawns)
            {
                var state = (Hediff_NightHarasserState)HediffMaker.MakeHediff(NightSafetyDefOf.NightSafety_HarasserState, pawn);
                state.Initialize(theme, harassmentPoint, effigyStuff);
                pawn.health.AddHediff(state);
            }
            if (effigyStuff != null)
            {
                int supplyCount = NightSafetyDefOf.NightSafety_HarassmentEffigy.costStuffCount;
                foreach (Pawn pawn in pawns.OrderBy(item => item.thingIDNumber))
                {
                    Thing supply = ThingMaker.MakeThing(effigyStuff);
                    supply.stackCount = supplyCount;
                    pawn.inventory.innerContainer.TryAdd(supply);
                }
            }

            LordMaker.MakeNewLord(faction, new LordJob_NightHarassers(harassmentPoint, theme, effigyStuff), map, pawns);
            SendStandardLetter(parms, pawns);
            return true;
            }
            finally
            {
                Rand.PopState();
            }
        }

        private static ThingDef SelectEffigyStuff()
        {
            if (Rand.Bool) return ThingDefOf.WoodLog;
            ThingDef[] stoneBlocks = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def => def.defName.StartsWith("Blocks") && def.stuffProps != null && def.stuffProps.categories.Contains(StuffCategoryDefOf.Stony))
                .OrderBy(def => def.defName)
                .ToArray();
            return stoneBlocks.Length == 0 ? ThingDefOf.WoodLog : stoneBlocks[Rand.Range(0, stoneBlocks.Length)];
        }

        private static Faction GetOrCreateHarasserFaction()
        {
            Faction? existing = Find.FactionManager.FirstFactionOfDef(NightSafetyDefOf.NightSafety_Harassers);
            if (existing != null) return existing;

            // This hidden faction is created lazily, the first time the incident needs ownership.
            // Its Def forbids world generation, settlements, raids, diplomacy surfaces, and goodwill play.
            Faction created = FactionGenerator.NewGeneratedFaction(new FactionGeneratorParms(NightSafetyDefOf.NightSafety_Harassers));
            Find.FactionManager.Add(created);
            return created;
        }
    }
}
