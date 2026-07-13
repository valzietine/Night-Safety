using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace NightSafety.AI
{
    public sealed class JobGiver_HuntExposedPawn : ThinkNode_JobGiver
    {
        protected override Job? TryGiveJob(Pawn pawn)
        {
            if (!pawn.Spawned || pawn.Map == null) return null;
            NightSafetyMapComponent component = pawn.Map.GetComponent<NightSafetyMapComponent>();
            if (!component.IsNight)
            {
                IntVec3 position = pawn.Position;
                int west = position.x;
                int east = pawn.Map.Size.x - 1 - position.x;
                int south = position.z;
                int north = pawn.Map.Size.z - 1 - position.z;
                IntVec3 edge = west <= east && west <= south && west <= north ? new IntVec3(0, 0, position.z)
                    : east <= south && east <= north ? new IntVec3(pawn.Map.Size.x - 1, 0, position.z)
                    : south <= north ? new IntVec3(position.x, 0, 0)
                    : new IntVec3(position.x, 0, pawn.Map.Size.z - 1);
                Job exit = JobMaker.MakeJob(JobDefOf.Goto, edge);
                exit.exitMapOnArrival = true;
                return exit;
            }

            Pawn? target = pawn.Map.mapPawns.FreeColonistsSpawned
                .Where(candidate => component.IsPawnExposed(candidate) && pawn.CanReach(candidate, PathEndMode.Touch, Danger.Deadly))
                .OrderBy(candidate => pawn.Position.DistanceToSquared(candidate.Position))
                .ThenBy(candidate => candidate.thingIDNumber)
                .FirstOrDefault();
            if (target == null) return JobMaker.MakeJob(JobDefOf.Wait, NightSafetyDefOf.NightSafety_HarassmentConfig.spiritHuntWaitTicks);

            Job attack = JobMaker.MakeJob(JobDefOf.AttackMelee, target);
            attack.expiryInterval = NightSafetyDefOf.NightSafety_HarassmentConfig.spiritAttackExpiryTicks;
            attack.checkOverrideOnExpire = true;
            // The Spirit is a lethal boundary pressure, not a predator that leaves an exposed pawn merely downed.
            attack.killIncappedTarget = true;
            return attack;
        }
    }
}
