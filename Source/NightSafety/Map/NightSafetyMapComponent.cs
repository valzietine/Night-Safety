using System.Collections.Generic;
using System.Linq;
using NightSafety.Buildings;
using NightSafety.Core;
using RimWorld;
using Verse;
using Verse.AI;

namespace NightSafety
{
    public sealed class NightSafetyMapComponent : MapComponent
    {
        private readonly HashSet<CompProtectionOven> ovens = new HashSet<CompProtectionOven>();
        private Pawn? forestSpirit;
        private readonly Dictionary<Pawn, int> safetyRetryAfterTick = new Dictionary<Pawn, int>();

        public NightSafetyMapComponent(Map map) : base(map)
        {
        }

        public bool IsNight => NightSafetyMath.IsNight(GenLocalDate.HourFloat(map), 20f, 6f);
        public bool CanStartForestSpirit => IsNight && map.IsPlayerHome && map.mapPawns.FreeColonistsSpawnedCount > 0 && !HasActiveForestSpirit;
        private bool HasActiveForestSpirit => forestSpirit != null && !forestSpirit.DestroyedOrNull()
            && NightEncounterTransitions.HasActiveOwner(true, forestSpirit.Spawned, forestSpirit.Map == map);

        public bool IsSafetyBackoffActive(Pawn pawn)
        {
            return safetyRetryAfterTick.TryGetValue(pawn, out int retryTick) && Find.TickManager.TicksGame < retryTick;
        }

        public void RecordSafetyPathFailure(Pawn pawn)
        {
            safetyRetryAfterTick[pawn] = Find.TickManager.TicksGame + 600;
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            ovens.Clear();
            foreach (Thing thing in map.listerThings.ThingsOfDef(NightSafetyDefOf.NightSafety_ProtectionOven))
            {
                CompProtectionOven? oven = thing.TryGetComp<CompProtectionOven>();
                if (oven != null) ovens.Add(oven);
            }
            RepairForestSpiritOwnership();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref forestSpirit, "nightSafetyForestSpirit");
            if (Scribe.mode == LoadSaveMode.PostLoadInit
                && forestSpirit != null && (forestSpirit.DestroyedOrNull() || forestSpirit.Map != map))
            {
                forestSpirit = null;
            }
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (!map.IsHashIntervalTick(250)) return;
            PruneOvens();
            if (CanStartForestSpirit) TrySpawnForestSpirit();
            foreach (Pawn pawn in safetyRetryAfterTick.Keys.Where(pawn => pawn == null || pawn.DestroyedOrNull() || pawn.MapHeld != map).ToList())
                safetyRetryAfterTick.Remove(pawn);
        }

        public void RegisterForestSpirit(Pawn pawn)
        {
            if (pawn.Map == map) forestSpirit = pawn;
        }

        private void RepairForestSpiritOwnership()
        {
            List<Pawn> candidates = map.mapPawns.AllPawnsSpawned
                .Where(pawn => pawn.kindDef == NightSafetyDefOf.NightSafety_ForestSpirit && !pawn.Dead)
                .OrderBy(pawn => pawn.thingIDNumber)
                .ToList();
            forestSpirit = candidates.FirstOrDefault();

            // Only self-repaired Spirit pawns on this map are eligible, so stray copies from reloads or
            // old saves are destroyed.
            foreach (Pawn duplicate in candidates.Where(pawn => pawn != forestSpirit))
                duplicate.Destroy(DestroyMode.Vanish);
        }

        private void TrySpawnForestSpirit()
        {
            if (!CellFinder.TryFindRandomEdgeCellWith(c => c.Standable(map), map, CellFinder.EdgeRoadChance_Ignore, out IntVec3 spawnCell))
                return;

            Pawn spirit = PawnGenerator.GeneratePawn(NightSafetyDefOf.NightSafety_ForestSpirit, null);
            GenSpawn.Spawn(spirit, spawnCell, map);
            RegisterForestSpirit(spirit);
        }

        public void Register(CompProtectionOven oven)
        {
            if (oven.parent.Map == map) ovens.Add(oven);
        }

        public void Deregister(CompProtectionOven oven) => ovens.Remove(oven);

        public void PruneOvens()
        {
            ovens.RemoveWhere(oven => oven == null || !oven.parent.Spawned || oven.parent.Map != map);
        }

        public bool IsProtected(IntVec3 cell)
        {
            PruneOvens();
            foreach (CompProtectionOven oven in ovens)
            {
                if (oven.ActiveNow && NightSafetyMath.IsWithinRadius(
                    cell.x, cell.z, oven.parent.Position.x, oven.parent.Position.z, oven.Radius))
                    return true;
            }
            return false;
        }

        public bool IsPawnExposed(Pawn pawn)
        {
            return IsNight && pawn.Spawned && pawn.Map == map && !pawn.Dead && !IsProtected(pawn.Position);
        }

        public bool TryFindSafeDestination(Pawn pawn, out IntVec3 destination)
        {
            destination = IntVec3.Invalid;
            PruneOvens();
            var candidates = new List<(IntVec3 cell, int distance, int ovenId)>();
            foreach (CompProtectionOven oven in ovens.Where(item => item.ActiveNow).OrderBy(item => item.parent.thingIDNumber))
            {
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(oven.parent.Position, oven.Radius, true))
                {
                    if (!cell.InBounds(map) || !cell.Standable(map) || IsForbidden(pawn, cell)) continue;
                    Area? allowedArea = pawn.playerSettings?.EffectiveAreaRestrictionInPawnCurrentMap;
                    if (allowedArea != null && !allowedArea[cell]) continue;
                    if (!pawn.CanReach(cell, PathEndMode.OnCell, Danger.Some)) continue;
                    candidates.Add((cell, pawn.Position.DistanceToSquared(cell), oven.parent.thingIDNumber));
                }
            }

            if (candidates.Count == 0) return false;
            destination = candidates.OrderBy(c => c.distance).ThenBy(c => c.ovenId).ThenBy(c => map.cellIndices.CellToIndex(c.cell)).First().cell;
            return true;
        }

        private static bool IsForbidden(Pawn pawn, IntVec3 cell)
        {
            Building edifice = cell.GetEdifice(pawn.Map);
            return edifice != null && edifice.IsForbidden(pawn);
        }
    }
}
