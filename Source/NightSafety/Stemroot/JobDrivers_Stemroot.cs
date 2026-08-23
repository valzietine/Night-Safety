using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace NightSafety.Stemroot
{
    /// <summary>
    /// The work loop for both stemroot orders. It is shaped like vanilla plant work rather than
    /// mining: progress accumulates at the pawn's plant work speed and the experience goes to
    /// Plants, which is what the design doc asks for.
    /// </summary>
    public abstract class JobDriver_StemrootWork : JobDriver
    {
        private float workDone;

        protected Thing Stemroot => job.GetTarget(TargetIndex.A).Thing;
        protected CompStemroot? Comp => Stemroot?.TryGetComp<CompStemroot>();

        protected abstract DesignationDef RequiredDesignation { get; }
        protected abstract float TotalWork { get; }
        protected abstract EffecterDef WorkEffecter { get; }
        protected abstract void FinishWork(Pawn actor, CompStemroot comp);

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null, errorOnFailed);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref workDone, "nightSafetyStemrootWorkDone", 0f);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnThingMissingDesignation(TargetIndex.A, RequiredDesignation);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            Toil work = ToilMaker.MakeToil("MakeNewToils");
            work.tickIntervalAction = delegate(int delta)
            {
                Pawn actor = work.actor;
                actor.skills?.Learn(SkillDefOf.Plants, 0.085f * delta);
                workDone += actor.GetStatValue(StatDefOf.PlantWorkSpeed) * delta;
                if (workDone < TotalWork) return;

                CompStemroot? comp = Comp;
                if (comp == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                // Clear the order before the work resolves: cutting destroys the thing the
                // designation is attached to.
                actor.Map.designationManager.TryRemoveDesignationOn(Stemroot, RequiredDesignation);
                FinishWork(actor, comp);
                workDone = 0f;
                ReadyForNextToil();
            };
            work.defaultCompleteMode = ToilCompleteMode.Never;
            work.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            work.FailOnThingMissingDesignation(TargetIndex.A, RequiredDesignation);
            work.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            work.WithEffect(WorkEffecter, TargetIndex.A);
            work.WithProgressBar(TargetIndex.A, () => workDone / TotalWork, interpolateBetweenActorAndTarget: true);
            work.activeSkill = () => SkillDefOf.Plants;
            yield return work;
        }
    }

    public sealed class JobDriver_CutStemroot : JobDriver_StemrootWork
    {
        protected override DesignationDef RequiredDesignation => NightSafetyDefOf.NightSafety_CutStemrootDesignation;
        protected override float TotalWork => Comp?.CutWork ?? 1000f;
        protected override EffecterDef WorkEffecter => EffecterDefOf.Harvest_Tree;

        protected override void FinishWork(Pawn actor, CompStemroot comp) => comp.FinishCut(actor);
    }

    public sealed class JobDriver_HarvestStemroot : JobDriver_StemrootWork
    {
        protected override DesignationDef RequiredDesignation => NightSafetyDefOf.NightSafety_HarvestStemrootDesignation;
        protected override float TotalWork => Comp?.HarvestWork ?? 400f;
        protected override EffecterDef WorkEffecter => EffecterDefOf.Harvest_Plant;

        protected override void FinishWork(Pawn actor, CompStemroot comp) => comp.FinishHarvest(actor);
    }
}
