using System.Collections.Generic;
using System.Linq;
using NightSafety.Core;
using NightSafety.AI;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace NightSafety.Lords
{
    public sealed class LordJob_NightHarassers : LordJob_DefendPoint
    {
        private bool retreating;
        private Dictionary<int, float> observedHealth = new Dictionary<int, float>();
        private Dictionary<int, int> nextActionTickByPawnId = new Dictionary<int, int>();
        private HarassmentTheme theme;
        private IntVec3 harassmentPoint;
        private ThingDef? effigyStuff;
        private Thing? effigy;
        private int effigyBuilderId = -1;

        public LordJob_NightHarassers() { }

        public LordJob_NightHarassers(IntVec3 harassmentPoint, HarassmentTheme theme, ThingDef? effigyStuff)
            : base(harassmentPoint, wanderRadius: 10f, defendRadius: 8f, isCaravanSendable: false, addFleeToil: false)
        {
            this.harassmentPoint = harassmentPoint;
            this.theme = theme;
            this.effigyStuff = effigyStuff;
        }

        public HarassmentTheme Theme => theme;
        public IntVec3 HarassmentPoint => harassmentPoint;
        public ThingDef EffigyStuff => effigyStuff ?? ThingDefOf.WoodLog;
        public bool HasEffigy => effigy != null && !effigy.DestroyedOrNull() && effigy.Spawned;
        public bool Retreating => retreating;

        public void RestoreRetreating(bool persistedRetreating)
        {
            // Retreat is permanent for this pack, so transferred ownership may restore true but never clear it.
            retreating |= persistedRetreating;
        }

        private void PersistRetreatState()
        {
            if (lord == null) return;
            foreach (Pawn pawn in lord.ownedPawns.Where(pawn => !pawn.Dead))
            {
                var state = pawn.health.hediffSet.GetFirstHediffOfDef(NightSafetyDefOf.NightSafety_HarasserState)
                    as Hediff_NightHarasserState;
                if (state == null)
                {
                    state = (Hediff_NightHarasserState)HediffMaker.MakeHediff(NightSafetyDefOf.NightSafety_HarasserState, pawn);
                    pawn.health.AddHediff(state);
                }
                state.Initialize(theme, harassmentPoint, effigyStuff, isRetreating: true);
            }
        }

        public override void LordJobTick()
        {
            base.LordJobTick();
            if (!retreating && (lord?.Map?.GetComponent<NightSafetyMapComponent>()?.IsNight == false || IsConfronted()
                || (theme == HarassmentTheme.Theft && lord != null
                    && Find.TickManager.TicksGame % NightSafetyDefOf.NightSafety_HarassmentConfig.theftRecheckIntervalTicks == 0
                    && !HarassmentUtility.AnyTheftTarget(lord))))
            {
                retreating = true;
                PersistRetreatState();
            }

            AssignDuties();
        }

        private void AssignDuties()
        {
            if (lord == null) return;
            foreach (Pawn pawn in lord.ownedPawns)
            {
                if (retreating) pawn.mindState.duty = new PawnDuty(DutyDefOf.ExitMapBest);
                else pawn.mindState.duty = new PawnDuty(NightSafetyDefOf.NightSafety_Harass, harassmentPoint, 10f);
            }
        }

        private bool IsConfronted()
        {
            if (lord?.Map == null) return false;

            List<Pawn> defenders = lord.Map.mapPawns.FreeColonistsSpawned
                .Where(pawn => !pawn.Downed && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Moving))
                .OrderBy(pawn => pawn.thingIDNumber)
                .ToList();

            foreach (Pawn harasser in lord.ownedPawns.OrderBy(pawn => pawn.thingIDNumber))
            {
                float currentHealth = harasser.health.summaryHealth.SummaryHealthPercent;
                bool healthDecreased = observedHealth.TryGetValue(harasser.thingIDNumber, out float previousHealth)
                    && currentHealth < previousHealth;
                observedHealth[harasser.thingIDNumber] = currentHealth;
                if (healthDecreased) return true;

                foreach (Pawn defender in defenders)
                {
                    bool lineOfSight = GenSight.LineOfSightToThing(harasser.Position, defender, lord.Map);
                    if (HarasserConfrontationPolicy.IsConfronted(
                        harasser.Position.DistanceToSquared(defender.Position), capablePlayerPawn: true,
                        healthDecreased: false, lineOfSight: lineOfSight))
                        return true;
                }
            }

            return false;
        }

        public bool IsRegrouping(Pawn pawn)
            => nextActionTickByPawnId.TryGetValue(pawn.thingIDNumber, out int tick) && Find.TickManager.TicksGame < tick;

        public void RecordActionCompleted(Pawn pawn)
        {
            HarassmentConfigDef config = NightSafetyDefOf.NightSafety_HarassmentConfig;
            int duration = HarassmentThemePolicy.RegroupDuration(config.regroupMinTicks, config.regroupMaxTicks,
                pawn.thingIDNumber, Find.TickManager.TicksGame);
            nextActionTickByPawnId[pawn.thingIDNumber] = Find.TickManager.TicksGame + duration;
        }

        public bool TryClaimEffigyBuilder(Pawn pawn)
        {
            if (HasEffigy || theme != HarassmentTheme.Effigy) return false;
            if (effigyBuilderId >= 0 && lord?.ownedPawns.Any(item => item.thingIDNumber == effigyBuilderId && item.Spawned && !item.Downed) == true)
                return effigyBuilderId == pawn.thingIDNumber;

            Pawn? builder = lord?.ownedPawns.Where(item => item.Spawned && !item.Downed
                    && item.inventory.innerContainer.Any(thing => thing.def == EffigyStuff
                        && thing.stackCount >= NightSafetyDefOf.NightSafety_HarassmentEffigy.costStuffCount))
                .OrderBy(item => item.thingIDNumber).FirstOrDefault();
            effigyBuilderId = builder?.thingIDNumber ?? -1;
            return effigyBuilderId == pawn.thingIDNumber;
        }

        public void RegisterEffigy(Thing builtEffigy)
        {
            if (builtEffigy.Map == lord?.Map) effigy = builtEffigy;
            effigyBuilderId = -1;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref retreating, "nightSafetyRetreating", false);
            Scribe_Values.Look(ref theme, "nightSafetyHarassmentTheme", HarassmentTheme.Arson);
            Scribe_Values.Look(ref harassmentPoint, "nightSafetyHarassmentPoint");
            Scribe_Defs.Look(ref effigyStuff, "nightSafetyEffigyStuff");
            Scribe_References.Look(ref effigy, "nightSafetyEffigy");
            Scribe_Values.Look(ref effigyBuilderId, "nightSafetyEffigyBuilderId", -1);
            Scribe_Collections.Look(ref nextActionTickByPawnId, "nightSafetyNextActionTicks", LookMode.Value, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                observedHealth = new Dictionary<int, float>();
                nextActionTickByPawnId ??= new Dictionary<int, int>();
                if (effigy != null && (effigy.DestroyedOrNull() || effigy.Map != lord?.Map)) effigy = null;
                if (harassmentPoint == IntVec3.Zero && lord?.ownedPawns.Count > 0)
                    harassmentPoint = lord.ownedPawns.OrderBy(pawn => pawn.thingIDNumber).First().Position;
            }
        }
    }
}
