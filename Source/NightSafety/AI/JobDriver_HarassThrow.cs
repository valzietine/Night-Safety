using System.Collections.Generic;
using NightSafety.Core;
using NightSafety.Lords;
using Verse;
using Verse.AI;

namespace NightSafety.AI
{
    public sealed class JobDriver_HarassThrow : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
            // Throw jobs skip reservation so the whole pack can pressure the same target together.
            => true;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);

            Toil warmup = Toils_General.Wait(NightSafetyDefOf.NightSafety_HarassmentConfig.throwWarmupTicks);
            warmup.WithProgressBarToilDelay(TargetIndex.A);
            yield return warmup;

            yield return Toils_General.Do(() =>
            {
                Thing target = job.targetA.Thing;
                LordJob_NightHarassers? lordJob = HarassmentUtility.LordJobFor(pawn);
                if (target == null || lordJob == null) return;
                bool requireFlammable = lordJob.Theme == HarassmentTheme.Arson;
                // Eligibility can change during approach/warmup as roofs or protection activate.
                // Recheck at the irreversible launch boundary so stale jobs cannot violate safety.
                if (!HarassmentUtility.IsAllowedDestructiveTarget(target, pawn.Map, requireFlammable)) return;
                ThingDef projectileDef = lordJob.Theme == HarassmentTheme.Arson
                    ? NightSafetyDefOf.NightSafety_HarassmentConfig.arsonProjectile
                    : NightSafetyDefOf.NightSafety_HarassmentConfig.bombardmentProjectile;
                Projectile projectile = (Projectile)GenSpawn.Spawn(projectileDef, pawn.Position, pawn.Map);
                projectile.Launch(pawn, target, target, ProjectileHitFlags.IntendedTarget | ProjectileHitFlags.NonTargetPawns, preventFriendlyFire: false);
                lordJob.RecordActionCompleted(pawn);
            });
        }
    }
}
