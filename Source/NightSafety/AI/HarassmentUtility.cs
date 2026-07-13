using System.Collections.Generic;
using System.Linq;
using NightSafety.Buildings;
using NightSafety.Core;
using NightSafety.Lords;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace NightSafety.AI
{
    public static class HarassmentUtility
    {
        public static LordJob_NightHarassers? LordJobFor(Pawn pawn) => pawn.GetLord()?.LordJob as LordJob_NightHarassers;

        public static bool IsAllowedDestructiveTarget(Thing thing, Map map, bool requireFlammable)
        {
            NightSafetyMapComponent component = map.GetComponent<NightSafetyMapComponent>();
            return HarassmentTargetPolicy.AllowsDestructiveTarget(thing.Spawned, thing.Map == map,
                thing.Faction == Faction.OfPlayer, thing.def.useHitPoints, thing is Pawn || thing is Corpse,
                thing.def == NightSafetyDefOf.NightSafety_ProtectionOven, component.IsProtected(thing.Position),
                IsExterior(thing.Position, map), thing.FlammableNow, requireFlammable);
        }

        public static bool IsAllowedTheftTarget(Thing thing, Map map)
        {
            return thing.Spawned && thing.Map == map && thing.def.EverHaulable && !thing.IsBurning()
                && !(thing is Pawn) && !(thing is Corpse)
                && thing.Position.GetSlotGroup(map) != null && !thing.Position.Roofed(map)
                && !map.GetComponent<NightSafetyMapComponent>().IsProtected(thing.Position);
        }

        public static Thing? FindDestructiveTarget(Pawn pawn, bool requireFlammable)
        {
            return pawn.Map.listerThings.AllThings
                .Where(thing => IsAllowedDestructiveTarget(thing, pawn.Map, requireFlammable))
                .Where(thing => TryFindThrowCell(pawn, thing, out _))
                .OrderBy(thing => pawn.Position.DistanceToSquared(thing.Position))
                .ThenBy(thing => thing.thingIDNumber)
                .FirstOrDefault();
        }

        public static Thing? FindTheftTarget(Pawn pawn)
        {
            return pawn.Map.listerThings.AllThings
                .Where(thing => IsAllowedTheftTarget(thing, pawn.Map))
                .Where(thing => MassUtility.CountToPickUpUntilOverEncumbered(pawn, thing) > 0)
                .Where(thing => pawn.CanReserveAndReach(thing, PathEndMode.ClosestTouch, Danger.Deadly))
                .OrderByDescending(thing => thing.MarketValue * thing.stackCount)
                .ThenBy(thing => pawn.Position.DistanceToSquared(thing.Position))
                .ThenBy(thing => thing.thingIDNumber)
                .FirstOrDefault();
        }

        public static Building? FindObjectiveBreachTarget(Pawn pawn, bool requireFlammable, bool theft)
        {
            Map map = pawn.Map;
            NightSafetyMapComponent component = map.GetComponent<NightSafetyMapComponent>();
            IEnumerable<Thing> objectives = map.listerThings.AllThings
                .Where(thing => theft ? IsAllowedTheftTarget(thing, map) : IsAllowedDestructiveTarget(thing, map, requireFlammable))
                .OrderBy(thing => pawn.Position.DistanceToSquared(thing.Position))
                .ThenBy(thing => thing.thingIDNumber);

            foreach (Thing objective in objectives)
            {
                if (pawn.CanReach(objective, PathEndMode.ClosestTouch, Danger.Deadly)) continue;
                PawnPath path = map.pathFinder.FindPathNow(pawn.Position, objective, TraverseParms.For(pawn, Danger.Deadly,
                    TraverseMode.PassAllDestroyableThings), peMode: PathEndMode.ClosestTouch);
                try
                {
                    if (!path.Found) continue;
                    // NodesReversed is destination-to-start; inspect from the pawn outward so the job is reachable.
                    foreach (IntVec3 cell in path.NodesReversed.Reverse<IntVec3>())
                    {
                        Building? edifice = cell.GetEdifice(map);
                        if (edifice == null || edifice.Faction != Faction.OfPlayer) continue;
                        if (edifice.def != ThingDefOf.Wall && !edifice.def.IsDoor) continue;
                        if (edifice.def == NightSafetyDefOf.NightSafety_ProtectionOven
                            || component.IsProtected(edifice.Position)) continue;
                        return edifice;
                    }
                }
                finally
                {
                    path.ReleaseToPool();
                }
            }

            return null;
        }

        public static bool AnyTheftTarget(Lord lord)
        {
            Pawn[] availablePawns = lord.ownedPawns.Where(pawn => pawn.Spawned && !pawn.Downed)
                .OrderBy(pawn => pawn.thingIDNumber)
                .ToArray();
            // A target reserved by a packmate is temporarily invisible to CanReserveAndReach.
            // Keep the encounter alive while that ordinary TakeInventory job completes so the
            // rest of the pack can evaluate any remainder instead of retreating prematurely.
            if (availablePawns.Any(pawn => pawn.CurJobDef == NightSafetyDefOf.NightSafety_HarassTheft
                && pawn.CurJob?.targetA.Thing is Thing target && IsAllowedTheftTarget(target, pawn.Map)))
                return true;
            return availablePawns.Any(pawn => FindTheftTarget(pawn) != null);
        }

        public static bool TryFindThrowCell(Pawn pawn, Thing target, out IntVec3 result)
        {
            HarassmentConfigDef config = NightSafetyDefOf.NightSafety_HarassmentConfig;
            Map map = pawn.Map;
            NightSafetyMapComponent component = map.GetComponent<NightSafetyMapComponent>();
            IEnumerable<IntVec3> cells = GenRadial.RadialCellsAround(target.Position, config.maxThrowRange, true)
                .Where(cell => cell.InBounds(map))
                .Where(cell => cell.DistanceTo(target.Position) >= config.minThrowRange)
                .Where(cell => cell.DistanceTo(target.Position) <= config.maxThrowRange)
                .Where(cell => cell.Standable(map) && !component.IsProtected(cell))
                .Where(cell => GenSight.LineOfSight(cell, target.Position, map))
                .Where(cell => pawn.CanReach(cell, PathEndMode.OnCell, Danger.Deadly))
                .OrderBy(cell => pawn.Position.DistanceToSquared(cell))
                .ThenBy(cell => map.cellIndices.CellToIndex(cell));
            result = cells.DefaultIfEmpty(IntVec3.Invalid).First();
            return result.IsValid;
        }

        public static bool TryFindEffigyCell(Pawn pawn, IntVec3 focus, out IntVec3 result)
        {
            Map map = pawn.Map;
            HarassmentConfigDef config = NightSafetyDefOf.NightSafety_HarassmentConfig;
            NightSafetyMapComponent component = map.GetComponent<NightSafetyMapComponent>();
            result = GenRadial.RadialCellsAround(focus, config.effigySearchRadius, true)
                .Where(cell => cell.InBounds(map) && cell.Standable(map) && !cell.Roofed(map))
                .Where(cell => !component.IsProtected(cell))
                .Where(cell => GenConstruct.CanBuildOnTerrain(NightSafetyDefOf.NightSafety_HarassmentEffigy, cell, map, Rot4.North))
                .Where(cell => pawn.CanReach(cell, PathEndMode.OnCell, Danger.Deadly))
                .OrderBy(cell => focus.DistanceToSquared(cell))
                .ThenBy(cell => map.cellIndices.CellToIndex(cell))
                .DefaultIfEmpty(IntVec3.Invalid)
                .First();
            return result.IsValid;
        }

        private static bool IsExterior(IntVec3 cell, Map map)
        {
            if (!cell.Roofed(map)) return true;
            return GenRadial.RadialCellsAround(cell, 1.5f, false)
                .Any(adjacent => adjacent.InBounds(map) && !adjacent.Roofed(map));
        }
    }
}
