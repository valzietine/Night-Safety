using System.Collections.Generic;
using System.Linq;
using NightSafety.Core;
using NightSafety.Lords;
using Verse;
using Verse.AI;

namespace NightSafety.AI
{
    public sealed class JobDriver_BuildHarassmentEffigy : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
            => pawn.Reserve(job.targetA.Cell, job, 1, -1, null, errorOnFailed);

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);
            Toil build = Toils_General.Wait(900);
            build.WithProgressBarToilDelay(TargetIndex.A);
            yield return build;
            yield return Toils_General.Do(() =>
            {
                LordJob_NightHarassers? lordJob = HarassmentUtility.LordJobFor(pawn);
                if (lordJob == null || lordJob.HasEffigy) return;
                Thing? supply = pawn.inventory.innerContainer.FirstOrDefault(thing => thing.def == lordJob.EffigyStuff);
                int cost = NightSafetyDefOf.NightSafety_HarassmentEffigy.costStuffCount;
                if (supply == null || supply.stackCount < cost) return;
                supply.SplitOff(cost).Destroy(DestroyMode.Vanish);
                Thing effigy = ThingMaker.MakeThing(NightSafetyDefOf.NightSafety_HarassmentEffigy, lordJob.EffigyStuff);
                GenSpawn.Spawn(effigy, job.targetA.Cell, pawn.Map);
                lordJob.RegisterEffigy(effigy);
                lordJob.RecordActionCompleted(pawn);
            });
        }
    }
}
