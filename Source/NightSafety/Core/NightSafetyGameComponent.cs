using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NightSafety.Core
{
    public sealed class NightSafetyGameComponent : GameComponent
    {
        private List<Pawn> safetyDisabledPawns = new List<Pawn>();

        public NightSafetyGameComponent(Game game) { }

        public bool IsSafetySeekingEnabled(Pawn pawn) => !safetyDisabledPawns.Contains(pawn);

        public void SetSafetySeekingEnabled(Pawn pawn, bool enabled)
        {
            if (pawn.Faction != Faction.OfPlayer) return;
            if (enabled)
                safetyDisabledPawns.Remove(pawn);
            else
                safetyDisabledPawns.Add(pawn);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref safetyDisabledPawns, "nightSafetyDisabledPawns", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                safetyDisabledPawns ??= new List<Pawn>();
                PruneInvalidPreferences();
            }
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            if (Find.TickManager.TicksGame % 2500 == 0) PruneInvalidPreferences();
        }

        private void PruneInvalidPreferences()
        {
            safetyDisabledPawns.RemoveWhere(pawn => !PawnPreferenceLifecycle.ShouldRetain(
                pawn != null, pawn?.Destroyed ?? true, pawn?.Faction == Faction.OfPlayer,
                pawn?.Spawned ?? false, pawn?.MapHeld != null));
        }
    }
}
