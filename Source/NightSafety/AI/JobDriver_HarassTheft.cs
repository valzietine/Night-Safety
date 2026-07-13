using System.Collections.Generic;
using NightSafety.Core;
using NightSafety.Lords;
using RimWorld;
using Verse;
using Verse.AI;

namespace NightSafety.AI
{
    public sealed class JobDriver_HarassTheft : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
            => pawn.Reserve(job.targetA, job, 1, job.count, null, errorOnFailed);

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
            yield return Toils_General.Do(() =>
            {
                Thing target = job.targetA.Thing;
                LordJob_NightHarassers? lordJob = HarassmentUtility.LordJobFor(pawn);
                if (target == null || lordJob == null || !HarassmentUtility.IsAllowedTheftTarget(target, pawn.Map)) return;
                int capacity = MassUtility.CountToPickUpUntilOverEncumbered(pawn, target);
                int count = System.Math.Min(target.stackCount, System.Math.Min(job.count, capacity));
                if (count <= 0) return;

                Thing taken = target.SplitOff(count);
                if (!pawn.inventory.innerContainer.TryAdd(taken))
                {
                    GenPlace.TryPlaceThing(taken, pawn.Position, pawn.Map, ThingPlaceMode.Near);
                    return;
                }

                // Only a completed transfer earns the post-action regroup interval.
                lordJob.RecordActionCompleted(pawn);
            });
        }
    }
}
