using NightSafety.Core;
using NightSafety.Lords;
using RimWorld;
using Verse;
using Verse.AI;

namespace NightSafety.AI
{
    public sealed class JobGiver_Harass : ThinkNode_JobGiver
    {
        protected override Job? TryGiveJob(Pawn pawn)
        {
            LordJob_NightHarassers? lordJob = HarassmentUtility.LordJobFor(pawn);
            if (lordJob == null || lordJob.Retreating) return null;

            if (lordJob.IsRegrouping(pawn)) return RegroupJob(pawn, lordJob);

            switch (lordJob.Theme)
            {
                case HarassmentTheme.Arson:
                    return ThrowJob(pawn, requireFlammable: true) ?? BreachJob(pawn, requireFlammable: true, theft: false) ?? RegroupJob(pawn, lordJob);
                case HarassmentTheme.Bombardment:
                    return ThrowJob(pawn, requireFlammable: false) ?? BreachJob(pawn, requireFlammable: false, theft: false) ?? RegroupJob(pawn, lordJob);
                case HarassmentTheme.Effigy:
                    return EffigyJob(pawn, lordJob) ?? RegroupJob(pawn, lordJob);
                case HarassmentTheme.Theft:
                    return TheftJob(pawn) ?? BreachJob(pawn, requireFlammable: false, theft: true) ?? RegroupJob(pawn, lordJob);
                default:
                    return RegroupJob(pawn, lordJob);
            }
        }

        private static Job? BreachJob(Pawn pawn, bool requireFlammable, bool theft)
        {
            Building? blocker = HarassmentUtility.FindObjectiveBreachTarget(pawn, requireFlammable, theft);
            if (blocker == null) return null;
            Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, blocker);
            job.expiryInterval = NightSafetyDefOf.NightSafety_HarassmentConfig.breachJobExpiryTicks;
            job.checkOverrideOnExpire = true;
            return job;
        }

        private static Job? ThrowJob(Pawn pawn, bool requireFlammable)
        {
            Thing? target = HarassmentUtility.FindDestructiveTarget(pawn, requireFlammable);
            if (target == null || !HarassmentUtility.TryFindThrowCell(pawn, target, out IntVec3 castCell)) return null;
            Job job = JobMaker.MakeJob(NightSafetyDefOf.NightSafety_HarassThrow, target, castCell);
            job.expiryInterval = NightSafetyDefOf.NightSafety_HarassmentConfig.throwJobExpiryTicks;
            return job;
        }

        private static Job? EffigyJob(Pawn pawn, LordJob_NightHarassers lordJob)
        {
            if (!lordJob.TryClaimEffigyBuilder(pawn) || !HarassmentUtility.TryFindEffigyCell(pawn, lordJob.HarassmentPoint, out IntVec3 cell))
                return null;
            return JobMaker.MakeJob(NightSafetyDefOf.NightSafety_BuildHarassmentEffigy, cell);
        }

        private static Job? TheftJob(Pawn pawn)
        {
            Thing? target = HarassmentUtility.FindTheftTarget(pawn);
            if (target == null) return null;
            Job job = JobMaker.MakeJob(NightSafetyDefOf.NightSafety_HarassTheft, target);
            job.count = System.Math.Min(target.stackCount, MassUtility.CountToPickUpUntilOverEncumbered(pawn, target));
            return job;
        }

        private static Job RegroupJob(Pawn pawn, LordJob_NightHarassers lordJob)
        {
            HarassmentConfigDef config = NightSafetyDefOf.NightSafety_HarassmentConfig;
            float returnDistanceSquared = config.regroupReturnDistance * config.regroupReturnDistance;
            if (pawn.Position.DistanceToSquared(lordJob.HarassmentPoint) > returnDistanceSquared)
                return JobMaker.MakeJob(JobDefOf.Goto, lordJob.HarassmentPoint);
            Job wait = JobMaker.MakeJob(JobDefOf.Wait, config.regroupWaitTicks);
            wait.expiryInterval = config.regroupWaitTicks;
            return wait;
        }
    }
}
