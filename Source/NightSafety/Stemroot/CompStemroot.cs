using System.Collections.Generic;
using NightSafety.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace NightSafety.Stemroot
{
    /// <summary>
    /// One cell of stemroot. The comp owns the surface state (plain, bleeding, blooming,
    /// fruiting, flushing), the countdown attached to it, the sap it drips while bleeding, and
    /// the slow regrowth after damage. Every decision goes through <see cref="StemrootPolicy"/>
    /// so the rules stay testable and land the same way on every peer.
    /// </summary>
    public sealed class CompStemroot : ThingComp
    {
        private StemrootState state = StemrootState.Plain;
        private int stateEndTick = -1;
        private int lastDamageTick = -99999;
        private int nextStateCheckTick = -1;
        private int nextFilthTick = -1;
        private bool wasExposed;

        private Graphic? bleedingOverlay;
        private Graphic? bloomingOverlay;
        private Graphic? fruitingOverlay;
        private Graphic? flushingOverlay;

        public CompProperties_Stemroot Props => (CompProperties_Stemroot)props;
        private static StemrootConfigDef Config => NightSafetyDefOf.NightSafety_StemrootConfig;

        public StemrootState State => state;
        public bool CanHarvestNow => StemrootPolicy.IsHarvestable(state);

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref state, "nightSafetyStemrootState", StemrootState.Plain);
            Scribe_Values.Look(ref stateEndTick, "nightSafetyStemrootStateEndTick", -1);
            Scribe_Values.Look(ref lastDamageTick, "nightSafetyStemrootLastDamageTick", -99999);
            Scribe_Values.Look(ref nextStateCheckTick, "nightSafetyStemrootNextStateCheckTick", -1);
            Scribe_Values.Look(ref nextFilthTick, "nightSafetyStemrootNextFilthTick", -1);
            Scribe_Values.Look(ref wasExposed, "nightSafetyStemrootWasExposed", false);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                // Stagger the first check so a freshly generated stand does not evaluate every
                // cell on the same tick.
                nextStateCheckTick = Find.TickManager.TicksGame
                    + (parent.thingIDNumber % Mathf.Max(1, Config.stateCheckIntervalTicks));
                wasExposed = IsExposed();
            }
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            // Cells that were walled in by this one are now open to the air, and they should
            // weep for a few hours. Tell them straight away instead of waiting for their own
            // rare tick to come round.
            IntVec3 position = parent.Position;
            base.PostDeSpawn(map, mode);
            foreach (IntVec3 offset in GenAdj.CardinalDirections)
            {
                IntVec3 neighbour = position + offset;
                if (!neighbour.InBounds(map)) continue;
                StemrootAt(neighbour, map)?.Notify_NeighbourRemoved();
            }
        }

        public void Notify_NeighbourRemoved()
        {
            if (!parent.Spawned) return;
            RefreshExposure();
        }

        public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostPostApplyDamage(dinfo, totalDamageDealt);
            if (totalDamageDealt <= 0f) return;

            lastDamageTick = Find.TickManager.TicksGame;
            StemrootState damaged = StemrootPolicy.AfterDamage(state, parent.HitPoints, parent.MaxHitPoints);
            if (damaged != state) SetState(damaged);
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            if (!parent.Spawned) return;

            Regenerate();
            RefreshExposure();

            bool exposed = wasExposed;
            int now = Find.TickManager.TicksGame;

            StemrootState lapsed = StemrootPolicy.Lapse(state, exposed, GlowHere());
            if (lapsed != state)
            {
                SetState(lapsed);
                return;
            }

            if (StemrootPolicy.HasTimer(state) && stateEndTick >= 0 && now >= stateEndTick)
            {
                SetState(StemrootPolicy.Expire(state));
                return;
            }

            if (state == StemrootState.Bleeding) DripSap(now);
            if (now >= nextStateCheckTick) RollForState(now, exposed);
        }

        private void Regenerate()
        {
            if (!StemrootPolicy.ShouldRegenerate(parent.HitPoints, parent.MaxHitPoints,
                Find.TickManager.TicksGame - lastDamageTick, Config.regenDelayTicks)) return;

            int perRareTick = Mathf.Max(1, GenTicks.TickRareInterval / Mathf.Max(1, Config.regenTicksPerHitPoint));
            parent.HitPoints = Mathf.Min(parent.MaxHitPoints, parent.HitPoints + perRareTick);
        }

        private void RefreshExposure()
        {
            bool exposed = IsExposed();
            if (exposed && !wasExposed && state == StemrootState.Plain)
            {
                // Freshly opened to the air.
                SetState(StemrootState.Bleeding);
            }
            wasExposed = exposed;
        }

        private void RollForState(int now, bool exposed)
        {
            nextStateCheckTick = now + Mathf.Max(1, Config.stateCheckIntervalTicks);
            if (state != StemrootState.Plain) return;

            float glow = GlowHere();
            float temperature = parent.Position.GetTemperature(parent.Map);
            float pollution = PollutionHere();

            float bleed = StemrootPolicy.BleedChance(exposed, state, Config.bleedChance,
                pollution, Config.bleedPollutionScale);
            float bloom = StemrootPolicy.BloomChance(exposed, state, Config.bloomChance, glow,
                temperature, Config.bloomMinTemperature, Config.bloomIdealTemperature);
            float flush = StemrootPolicy.FlushChance(exposed, state, Config.flushChance, glow);

            float roll = StemrootPolicy.UnitRoll(parent.thingIDNumber, now);
            StemrootState picked = StemrootPolicy.SelectSpontaneous(roll, bleed, bloom, flush);
            if (picked != StemrootState.Plain) SetState(picked);
        }

        private void DripSap(int now)
        {
            if (now < nextFilthTick) return;
            nextFilthTick = now + Mathf.Max(1, Config.sapFilthIntervalTicks);

            IntVec3 target = OpenNeighbour(now);
            if (target.IsValid) FilthMaker.TryMakeFilth(target, parent.Map, NightSafetyDefOf.NightSafety_FilthSap);
        }

        /// <summary>
        /// Sap lands on a walkable cell beside the stemroot, picked deterministically so two
        /// peers drop it in the same place.
        /// </summary>
        private IntVec3 OpenNeighbour(int seed)
        {
            Map map = parent.Map;
            var open = new List<IntVec3>();
            foreach (IntVec3 offset in GenAdj.CardinalDirections)
            {
                IntVec3 neighbour = parent.Position + offset;
                if (neighbour.InBounds(map) && neighbour.Walkable(map)) open.Add(neighbour);
            }
            if (open.Count == 0) return IntVec3.Invalid;

            int index = (int)(StemrootPolicy.UnitRoll(parent.thingIDNumber, seed) * open.Count);
            if (index >= open.Count) index = open.Count - 1;
            return open[index];
        }

        private bool IsExposed()
        {
            Map? map = parent.Map;
            if (map == null) return false;
            foreach (IntVec3 offset in GenAdj.CardinalDirections)
            {
                IntVec3 neighbour = parent.Position + offset;
                if (!neighbour.InBounds(map)) continue;
                if (StemrootAt(neighbour, map) == null) return true;
            }
            return false;
        }

        private float GlowHere() => parent.Map.glowGrid.GroundGlowAt(parent.Position);

        private float PollutionHere()
        {
            if (!ModsConfig.BiotechActive) return 0f;
            return parent.Map.pollutionGrid.IsPolluted(parent.Position) ? 1f : 0f;
        }

        private void SetState(StemrootState next)
        {
            if (next == state) return;
            state = next;
            stateEndTick = -1;
            nextFilthTick = -1;

            int now = Find.TickManager.TicksGame;
            switch (next)
            {
                case StemrootState.Bleeding:
                    stateEndTick = now + StemrootPolicy.DurationTicks(Config.bleedDurationMinTicks,
                        Config.bleedDurationMaxTicks, parent.thingIDNumber, now);
                    break;
                case StemrootState.Blooming:
                    stateEndTick = now + StemrootPolicy.DurationTicks(Config.bloomDurationMinTicks,
                        Config.bloomDurationMaxTicks, parent.thingIDNumber, now);
                    break;
                case StemrootState.Fruiting:
                    stateEndTick = now + Mathf.Max(1, Config.fruitDurationTicks);
                    break;
            }

            if (parent.Spawned)
            {
                // The state is not harvestable any more, so drop any order that assumed it was.
                if (!StemrootPolicy.IsHarvestable(state))
                {
                    parent.Map.designationManager
                        .TryRemoveDesignationOn(parent, NightSafetyDefOf.NightSafety_HarvestStemrootDesignation);
                }
                parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlagDefOf.Things);
            }
        }

        public float CutWork => Config.cutWork;
        public float HarvestWork => Config.harvestWork;

        /// <summary>The whole cell comes down: wood, a dose of the affliction, and a bad memory.</summary>
        public void FinishCut(Pawn actor)
        {
            Map map = parent.Map;
            IntVec3 position = parent.Position;

            StemrootWork.ApplyAffliction(actor, Config.cutAfflictionSeverity);
            if (StemrootPolicy.AppliesUnsettled(actor.IsColonist))
                actor.needs?.mood?.thoughts?.memories?.TryGainMemory(NightSafetyDefOf.NightSafety_Unsettled);

            parent.Destroy(DestroyMode.Vanish);
            StemrootWork.DropYield(ThingDefOf.WoodLog, Config.cutWoodCount, position, map, actor);
        }

        /// <summary>The crop comes off and the wall stays up.</summary>
        public void FinishHarvest(Pawn actor)
        {
            if (!StemrootPolicy.IsHarvestable(state)) return;

            ThingDef? product = ProductFor(state);
            int count = StemrootPolicy.HarvestCount(state, Config.bleedingYieldCount,
                Config.fruitingYieldCount, Config.flushingYieldCount);

            StemrootWork.ApplyAffliction(actor, Config.harvestAfflictionSeverity);
            if (product != null) StemrootWork.DropYield(product, count, parent.Position, parent.Map, actor);
            SetState(StemrootPolicy.AfterHarvest(state));
        }

        private static ThingDef? ProductFor(StemrootState state)
        {
            switch (state)
            {
                case StemrootState.Bleeding: return ThingDefOf.Chemfuel;
                case StemrootState.Fruiting: return NightSafetyDefOf.NightSafety_OddFruit;
                case StemrootState.Flushing: return NightSafetyDefOf.NightSafety_OddFungus;
                default: return null;
            }
        }

        public override string CompInspectStringExtra()
        {
            var text = new System.Text.StringBuilder();
            text.Append("NightSafety_StemrootInspectState".Translate(StateLabel(state)));

            if (StemrootPolicy.HasTimer(state) && stateEndTick >= 0)
            {
                int remaining = Mathf.Max(0, stateEndTick - Find.TickManager.TicksGame);
                string period = remaining.ToStringTicksToPeriod();
                switch (state)
                {
                    case StemrootState.Bleeding:
                        text.AppendLine().Append("NightSafety_StemrootInspectStopsBleedingIn".Translate(period));
                        break;
                    case StemrootState.Blooming:
                        text.AppendLine().Append("NightSafety_StemrootInspectFruitIn".Translate(period));
                        break;
                    case StemrootState.Fruiting:
                        text.AppendLine().Append("NightSafety_StemrootInspectFruitDropsIn".Translate(period));
                        break;
                }
            }

            if (StemrootPolicy.IsHarvestable(state))
            {
                ThingDef? product = ProductFor(state);
                int count = StemrootPolicy.HarvestCount(state, Config.bleedingYieldCount,
                    Config.fruitingYieldCount, Config.flushingYieldCount);
                if (product != null)
                    text.AppendLine().Append("NightSafety_StemrootInspectHarvest".Translate(count, product.label));
            }

            if (parent.HitPoints < parent.MaxHitPoints)
                text.AppendLine().Append("NightSafety_StemrootInspectRegenerating".Translate());

            return text.ToString();
        }

        private static string StateLabel(StemrootState state)
        {
            switch (state)
            {
                case StemrootState.Bleeding: return "NightSafety_StemrootStateBleeding".Translate();
                case StemrootState.Blooming: return "NightSafety_StemrootStateBlooming".Translate();
                case StemrootState.Fruiting: return "NightSafety_StemrootStateFruiting".Translate();
                case StemrootState.Flushing: return "NightSafety_StemrootStateFlushing".Translate();
                default: return "NightSafety_StemrootStatePlain".Translate();
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();
            Graphic? overlay = OverlayFor(state);
            if (overlay == null) return;

            Vector3 position = parent.DrawPos;
            position.y = AltitudeLayer.BuildingOnTop.AltitudeFor();
            Graphics.DrawMesh(MeshPool.plane10, position, Quaternion.identity, overlay.MatSingle, 0);
        }

        private Graphic? OverlayFor(StemrootState state)
        {
            switch (state)
            {
                case StemrootState.Bleeding:
                    return bleedingOverlay ??= LoadOverlay(Props.bleedingOverlayPath);
                case StemrootState.Blooming:
                    return bloomingOverlay ??= LoadOverlay(Props.bloomingOverlayPath);
                case StemrootState.Fruiting:
                    return fruitingOverlay ??= LoadOverlay(Props.fruitingOverlayPath);
                case StemrootState.Flushing:
                    return flushingOverlay ??= LoadOverlay(Props.flushingOverlayPath);
                default:
                    return null;
            }
        }

        private static Graphic LoadOverlay(string path)
        {
            return GraphicDatabase.Get<Graphic_Single>(path, ShaderDatabase.Transparent,
                Vector2.one, Color.white);
        }

        public static CompStemroot? StemrootAt(IntVec3 cell, Map map)
        {
            Building edifice = cell.GetEdifice(map);
            return edifice?.def == NightSafetyDefOf.NightSafety_Stemroot
                ? edifice.TryGetComp<CompStemroot>()
                : null;
        }
    }
}
