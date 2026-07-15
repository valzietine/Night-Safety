using System.Collections.Generic;
using System.Linq;
using NightSafety.Buildings;
using NightSafety.Core;
using NightSafety.AI;
using NightSafety.Lords;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace NightSafety
{
    public sealed class NightSafetyMapComponent : MapComponent
    {
        private readonly HashSet<CompProtectionOven> ovens = new HashSet<CompProtectionOven>();
        private Pawn? forestSpirit;
        private readonly Dictionary<Pawn, int> safetyRetryAfterTick = new Dictionary<Pawn, int>();
        private int nextHarasserLocalDay = -1;
        private int lastHarasserScheduleDay = -1;

        public NightSafetyMapComponent(Map map) : base(map)
        {
        }

        public bool IsNight => NightSafetyMath.IsNight(GenLocalDate.HourFloat(map),
            NightSafetyDefOf.NightSafety_HarassmentConfig.nightStartHour,
            NightSafetyDefOf.NightSafety_HarassmentConfig.nightEndHour);
        public bool CanStartForestSpirit => IsNight && map.IsPlayerHome && map.mapPawns.FreeColonistsSpawnedCount > 0 && !HasActiveForestSpirit;
        private bool HasActiveForestSpirit => forestSpirit != null && !forestSpirit.DestroyedOrNull()
            && NightEncounterTransitions.HasActiveOwner(true, forestSpirit.Spawned, forestSpirit.Map == map);
        public bool HasActiveHarassers => map.lordManager.lords.Any(lord => lord.LordJob is Lords.LordJob_NightHarassers);

        public bool IsSafetyBackoffActive(Pawn pawn)
        {
            return safetyRetryAfterTick.TryGetValue(pawn, out int retryTick) && Find.TickManager.TicksGame < retryTick;
        }

        public void RecordSafetyPathFailure(Pawn pawn)
        {
            safetyRetryAfterTick[pawn] = Find.TickManager.TicksGame
                + NightSafetyDefOf.NightSafety_HarassmentConfig.safetyPathRetryTicks;
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
            EnsureHarasserStateMarkers();
            RepairHarasserOwnership();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref forestSpirit, "nightSafetyForestSpirit");
            Scribe_Values.Look(ref nextHarasserLocalDay, "nightSafetyNextHarasserLocalDay", -1);
            Scribe_Values.Look(ref lastHarasserScheduleDay, "nightSafetyLastHarasserScheduleDay", -1);
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
            RepairForestSpiritOwnership();
            EnsureHarasserStateMarkers();
            RepairHarasserOwnership();
            if (CanStartForestSpirit) TrySpawnForestSpirit();
            TryScheduleHarassers();
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

        private void RepairHarasserOwnership()
        {
            List<Pawn> orphans = map.mapPawns.AllPawnsSpawned
                .Where(pawn => pawn.Faction?.def == NightSafetyDefOf.NightSafety_Harassers
                    && pawn.GetLord() == null && !pawn.Dead)
                .OrderBy(pawn => pawn.thingIDNumber)
                .ToList();
            if (orphans.Count == 0) return;

            Hediff_NightHarasserState? state = orphans
                .Select(pawn => pawn.health.hediffSet.GetFirstHediffOfDef(NightSafetyDefOf.NightSafety_HarasserState))
                .OfType<Hediff_NightHarasserState>()
                .FirstOrDefault();
            if (state == null) return;

            var lordJob = new LordJob_NightHarassers(state.HarassmentPoint, state.Theme, state.EffigyStuff);
            lordJob.RestoreRetreating(state.Retreating);
            LordMaker.MakeNewLord(orphans[0].Faction, lordJob, map, orphans);
            Thing? existingEffigy = map.listerThings.ThingsOfDef(NightSafetyDefOf.NightSafety_HarassmentEffigy)
                .OrderBy(thing => thing.thingIDNumber).FirstOrDefault();
            if (existingEffigy != null) lordJob.RegisterEffigy(existingEffigy);
        }

        private void TryScheduleHarassers()
        {
            if (!HarasserSchedulePolicy.CanAttempt(IsNight, map.IsPlayerHome,
                map.mapPawns.FreeColonistsSpawnedCount, HasActiveHarassers)) return;

            float hour = GenLocalDate.HourFloat(map);
            int nightKey = HarasserSchedulePolicy.NightKey(GenLocalDate.Year(map), GenLocalDate.DayOfYear(map),
                hour, NightSafetyDefOf.NightSafety_HarassmentConfig.nightEndHour, GenDate.DaysPerYear);
            if (!HarasserSchedulePolicy.ShouldAttempt(nightKey, lastHarasserScheduleDay, nextHarasserLocalDay)) return;

            // Mark the night before execution so a reload or failed worker cannot retry every 250 ticks.
            lastHarasserScheduleDay = nightKey;

            IncidentParms parms = StorytellerUtility.DefaultParmsNow(
                NightSafetyDefOf.NightSafety_NightHarassersIncident.category, map);
            bool fired = NightSafetyDefOf.NightSafety_NightHarassersIncident.Worker.TryExecute(parms);
            nextHarasserLocalDay = fired
                ? HarasserSchedulePolicy.NextEligibleAfterSuccess(nightKey)
                : HarasserSchedulePolicy.NextEligibleAfterFailure(nightKey);
        }

        private void EnsureHarasserStateMarkers()
        {
            foreach (Lord lord in map.lordManager.lords.Where(lord => lord.LordJob is LordJob_NightHarassers))
            {
                var lordJob = (LordJob_NightHarassers)lord.LordJob;
                foreach (Pawn pawn in lord.ownedPawns.Where(pawn => !pawn.Dead))
                {
                    var state = pawn.health.hediffSet.GetFirstHediffOfDef(NightSafetyDefOf.NightSafety_HarasserState)
                        as Hediff_NightHarasserState;
                    if (state == null)
                    {
                        state = (Hediff_NightHarasserState)HediffMaker.MakeHediff(NightSafetyDefOf.NightSafety_HarasserState, pawn);
                        pawn.health.AddHediff(state);
                    }
                    state.Initialize(lordJob.Theme, lordJob.HarassmentPoint, lordJob.EffigyStuff, lordJob.Retreating);
                }
            }
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
            // Non-mutating query: skip invalid ovens inline instead of calling PruneOvens() on
            // every invocation. IsProtected is evaluated inside per-cell pathfinding predicates,
            // so a set-mutation pass per candidate cell was a real hot-path cost. The set is still
            // pruned each tick (MapComponentTick) and on register/deregister.
            foreach (CompProtectionOven oven in ovens)
            {
                if (oven == null || !oven.parent.Spawned || oven.parent.Map != map) continue;
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
