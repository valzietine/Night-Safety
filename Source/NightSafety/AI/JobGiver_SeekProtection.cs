using RimWorld;
using Verse;
using Verse.AI;

namespace NightSafety.AI
{
    public sealed class JobGiver_SeekProtection : ThinkNode_JobGiver
    {
        protected override Job? TryGiveJob(Pawn pawn)
        {
            NightSafetyMapComponent component = pawn.Map.GetComponent<NightSafetyMapComponent>();
            if (!component.TryFindSafeDestination(pawn, out IntVec3 destination))
            {
                component.RecordSafetyPathFailure(pawn);
                return null;
            }

            Job job = JobMaker.MakeJob(JobDefOf.Goto, destination);
            job.expiryInterval = 600;
            job.checkOverrideOnExpire = true;
            job.locomotionUrgency = LocomotionUrgency.Jog;
            return job;
        }
    }
}
