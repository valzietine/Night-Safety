using System.Collections.Generic;
using System.Linq;
using NightSafety.Buildings;
using NightSafety.Core;
using RimWorld;
using Verse;

namespace NightSafety
{
    /// <summary>
    /// Maintains one mod-owned <see cref="Area_Allowed"/> per map covering the union of active oven
    /// radii and assigns eligible colonists to it for the night, so vanilla job selection stops
    /// offering work outside the ring. Restores each pawn's own assignment at dawn.
    /// <para/>
    /// Nothing here issues jobs. Vanilla does the work: every WorkGiver already filters targets through
    /// <c>ForbidUtility.InAllowedArea</c>, and the
    /// <c>Pawn_PlayerSettings.AreaRestrictionInPawnCurrentMap</c> setter itself interrupts an in-flight
    /// job whose target falls outside the new area. That interrupt is what closes the reported hole
    /// where a job taken before dusk walked a pawn out of the ring after nightfall.
    /// <para/>
    /// Scope matches <c>JobGiver_HuntExposedPawn</c>: free colonists only. Animals are never Spirit
    /// targets, so restricting them would stop grazing for no safety gain.
    /// </summary>
    public sealed class NightSafetyContainment : IExposable
    {
        private Map map = null!;
        private Area_Allowed? zone;

        // Pawns whose assignment we currently own, and the assignment to give back. Absence from
        // stashedAreas means the pawn had no restriction and must be restored to none, so null values
        // are never serialized.
        private HashSet<Pawn> managedPawns = new HashSet<Pawn>();
        private Dictionary<Pawn, Area> stashedAreas = new Dictionary<Pawn, Area>();

        // Pawns the player re-zoned by hand tonight. Their choice stands until dawn; without this the
        // next scheduled pass would simply re-apply and fight them.
        private HashSet<Pawn> yieldedPawns = new HashSet<Pawn>();

        private bool wasNight;
        private bool slotCapReported;
        private int builtSignature;

        private List<Pawn> scribeKeys = new List<Pawn>();
        private List<Area> scribeValues = new List<Area>();

        public NightSafetyContainment() { }

        public NightSafetyContainment(Map map) => this.map = map;

        public void Initialize(Map owningMap) => map = owningMap;

        /// <summary>True when a usable safe zone exists. An empty area would confine pawns to nothing.</summary>
        private bool ZoneAvailable => zone != null && zone.TrueCount > 0;

        public void ExposeData()
        {
            Scribe_References.Look(ref zone, "nightSafetyContainmentZone");
            Scribe_Collections.Look(ref managedPawns, "nightSafetyContainedPawns", LookMode.Reference);
            Scribe_Collections.Look(ref yieldedPawns, "nightSafetyYieldedPawns", LookMode.Reference);
            Scribe_Collections.Look(ref stashedAreas, "nightSafetyStashedAreas", LookMode.Reference, LookMode.Reference,
                ref scribeKeys, ref scribeValues);
            Scribe_Values.Look(ref wasNight, "nightSafetyContainmentWasNight");
            Scribe_Values.Look(ref slotCapReported, "nightSafetyContainmentSlotCapReported");

            if (Scribe.mode != LoadSaveMode.PostLoadInit) return;

            // Tolerate missing keys and pawns/areas that vanished while saved. Without this a reload
            // could strand every colonist in the night zone with no record of what to restore.
            managedPawns ??= new HashSet<Pawn>();
            yieldedPawns ??= new HashSet<Pawn>();
            stashedAreas ??= new Dictionary<Pawn, Area>();
            managedPawns.RemoveWhere(pawn => pawn == null || pawn.Destroyed);
            yieldedPawns.RemoveWhere(pawn => pawn == null || pawn.Destroyed);
            foreach (Pawn stale in stashedAreas.Where(pair => pair.Key == null || pair.Key.Destroyed || pair.Value == null)
                .Select(pair => pair.Key).ToList())
                stashedAreas.Remove(stale);

            // Derived cell state is never serialized; force a rebuild against current oven state.
            builtSignature = 0;
        }

        public void Tick(bool isNight)
        {
            // Dawn edge: the player's manual overrides only hold for the night that produced them.
            if (wasNight && !isNight) yieldedPawns.Clear();
            wasNight = isNight;

            DropDeadPawns();
            SyncZone(isNight);

            bool zoneAvailable = ZoneAvailable;
            foreach (Pawn pawn in PawnsToConsider())
            {
                // Only this map's assignment is ours to touch, and the vanilla setter works through
                // MapHeld. Off-map pawns (caravan, pod, transfer) stay managed and are restored when
                // they come back.
                if (pawn.MapHeld != map || pawn.playerSettings == null) continue;

                bool managed = managedPawns.Contains(pawn);
                bool eligible = NightContainmentPolicy.CanContain(
                    pawn.Spawned,
                    pawn.Faction == Faction.OfPlayer,
                    pawn.Downed,
                    pawn.IsPrisoner,
                    pawn.IsSlave,
                    Current.Game?.GetComponent<NightSafetyGameComponent>()?.IsSafetySeekingEnabled(pawn) ?? true,
                    pawn.playerSettings.RespectsAllowedArea)
                    && !yieldedPawns.Contains(pawn);

                // Only meaningful while we still have a zone to compare against; if the zone is gone
                // the pawn must be released, not treated as a player override.
                bool playerOverride = managed && zone != null
                    && pawn.playerSettings.AreaRestrictionInPawnCurrentMap != zone;

                switch (NightContainmentPolicy.Decide(isNight, zoneAvailable, managed, eligible,
                    pawn.Drafted, pawn.CurJob?.playerForced == true, pawn.InMentalState, playerOverride))
                {
                    case ContainmentAction.Apply:
                        Apply(pawn);
                        break;
                    case ContainmentAction.Release:
                        Release(pawn);
                        break;
                    case ContainmentAction.Yield:
                        managedPawns.Remove(pawn);
                        stashedAreas.Remove(pawn);
                        yieldedPawns.Add(pawn);
                        break;
                }
            }
        }

        private void Apply(Pawn pawn)
        {
            Area? previous = pawn.playerSettings!.AreaRestrictionInPawnCurrentMap;
            if (previous != null) stashedAreas[pawn] = previous;
            else stashedAreas.Remove(pawn);
            managedPawns.Add(pawn);
            // Assignment itself interrupts any in-flight job targeting outside the ring.
            pawn.playerSettings.AreaRestrictionInPawnCurrentMap = zone;
        }

        private void Release(Pawn pawn)
        {
            pawn.playerSettings!.AreaRestrictionInPawnCurrentMap =
                stashedAreas.TryGetValue(pawn, out Area restored) ? restored : null;
            stashedAreas.Remove(pawn);
            managedPawns.Remove(pawn);
        }

        private List<Pawn> PawnsToConsider()
        {
            // Managed pawns are included even once they leave the colonist list, so a pawn that is
            // captured, enslaved, or downed still gets its own area back.
            //
            // Materialized deliberately: Apply/Release/Yield mutate managedPawns inside the loop, and
            // a lazy Concat over that set throws "Collection was modified" mid-tick.
            return map.mapPawns.FreeColonistsSpawned.Concat(managedPawns).Distinct().ToList();
        }

        private void DropDeadPawns()
        {
            managedPawns.RemoveWhere(pawn => pawn == null || pawn.Destroyed || pawn.Dead);
            yieldedPawns.RemoveWhere(pawn => pawn == null || pawn.Destroyed || pawn.Dead);
            foreach (Pawn gone in stashedAreas.Keys.Where(pawn => pawn == null || pawn.Destroyed || pawn.Dead).ToList())
                stashedAreas.Remove(gone);
        }

        private void SyncZone(bool isNight)
        {
            // The player can delete our area from the Zone tab like any other.
            if (zone != null && !map.areaManager.AllAreas.Contains(zone))
            {
                zone = null;
                builtSignature = 0;
            }

            if (!isNight) return;

            int signature = ActiveOvenSignature();
            if (signature == 0)
            {
                // No active oven: clear the ring rather than leaving pawns confined to a dead one.
                if (zone != null && zone.TrueCount > 0)
                {
                    zone.Clear();
                    builtSignature = 0;
                }
                return;
            }

            if (zone == null && !TryCreateZone()) return;
            if (signature == builtSignature) return;

            RebuildZoneCells();
            builtSignature = signature;
        }

        private bool TryCreateZone()
        {
            if (!map.areaManager.CanMakeNewAllowed())
            {
                if (!slotCapReported)
                {
                    slotCapReported = true;
                    Find.LetterStack.ReceiveLetter(
                        "NightSafety_ContainmentUnavailableLabel".Translate(),
                        "NightSafety_ContainmentUnavailableText".Translate(),
                        LetterDefOf.NeutralEvent);
                }
                return false;
            }

            // Area_Allowed's constructor draws a random colour. Seed it from stable map-local state so
            // shared-map RimWorld Together peers do not diverge on this draw.
            Rand.PushState(map.uniqueID);
            try
            {
                if (!map.areaManager.TryMakeNewAllowed(out Area_Allowed created)) return false;
                created.RenamableLabel = "NightSafety_ContainmentZoneLabel".Translate();
                zone = created;
            }
            finally
            {
                Rand.PopState();
            }

            slotCapReported = false;
            builtSignature = 0;
            return true;
        }

        private void RebuildZoneCells()
        {
            zone!.Clear();
            foreach (CompProtectionOven oven in ActiveOvens())
            {
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(oven.parent.Position, oven.Radius, true))
                {
                    if (cell.InBounds(map)) zone[cell] = true;
                }
            }
        }

        private IEnumerable<CompProtectionOven> ActiveOvens()
        {
            return map.listerThings.ThingsOfDef(NightSafetyDefOf.NightSafety_ProtectionOven)
                .Select(thing => thing.TryGetComp<CompProtectionOven>())
                .Where(oven => oven != null && oven.ActiveNow)
                .OrderBy(oven => oven!.parent.thingIDNumber)!;
        }

        /// <summary>
        /// Cheap change detector over the active ovens, so cells are rebuilt when one is built,
        /// destroyed, moved, refuelled, or burns out — and not otherwise.
        /// </summary>
        private int ActiveOvenSignature()
        {
            int signature = 0;
            foreach (CompProtectionOven oven in ActiveOvens())
            {
                unchecked
                {
                    signature = (signature * 397) ^ oven.parent.thingIDNumber;
                    signature = (signature * 397) ^ map.cellIndices.CellToIndex(oven.parent.Position);
                    signature = (signature * 397) ^ oven.Radius.GetHashCode();
                }
            }
            return signature;
        }
    }
}
