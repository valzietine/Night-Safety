using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace NightSafety.Stemroot
{
    /// <summary>
    /// Designation-driven work scanning for stemroot. Stemroot is an edifice, so the vanilla
    /// plant work givers never see it; these hand out the same shape of job for the plant
    /// cutting work type.
    /// </summary>
    public abstract class WorkGiver_Stemroot : WorkGiver_Scanner
    {
        protected abstract DesignationDef RequiredDesignation { get; }
        protected abstract JobDef WorkJob { get; }

        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override Danger MaxPathDanger(Pawn pawn) => Danger.Deadly;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            foreach (Designation designation in pawn.Map.designationManager.SpawnedDesignationsOfDef(RequiredDesignation))
            {
                if (designation.target.Thing != null) yield return designation.target.Thing;
            }
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            return !pawn.Map.designationManager.AnySpawnedDesignationOfDef(RequiredDesignation);
        }

        public override Job? JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t.def != NightSafetyDefOf.NightSafety_Stemroot) return null;
            if (pawn.Map.designationManager.DesignationOn(t, RequiredDesignation) == null) return null;
            if (t.IsForbidden(pawn)) return null;
            if (!pawn.CanReserve(t, 1, -1, null, forced)) return null;
            if (!ExtraRequirement(t)) return null;
            return JobMaker.MakeJob(WorkJob, t);
        }

        protected virtual bool ExtraRequirement(Thing t) => true;
    }

    public sealed class WorkGiver_CutStemroot : WorkGiver_Stemroot
    {
        protected override DesignationDef RequiredDesignation => NightSafetyDefOf.NightSafety_CutStemrootDesignation;
        protected override JobDef WorkJob => NightSafetyDefOf.NightSafety_CutStemrootJob;
    }

    public sealed class WorkGiver_HarvestStemroot : WorkGiver_Stemroot
    {
        protected override DesignationDef RequiredDesignation => NightSafetyDefOf.NightSafety_HarvestStemrootDesignation;
        protected override JobDef WorkJob => NightSafetyDefOf.NightSafety_HarvestStemrootJob;

        // A cell can lapse back to plain between the order and the pawn arriving, and there is
        // nothing left on it to take then.
        protected override bool ExtraRequirement(Thing t) => t.TryGetComp<CompStemroot>()?.CanHarvestNow == true;
    }
}
