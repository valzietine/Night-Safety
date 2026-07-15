using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NightSafety.Core
{
    public sealed class NightSafetyGameComponent : GameComponent
    {
        // Set (not List) so the per-pawn IsSafetySeekingEnabled lookup on the think-node hot
        // path is O(1). Scribe_Collections serializes a HashSet under the same key, so existing
        // saves load unchanged.
        private HashSet<Pawn> safetyDisabledPawns = new HashSet<Pawn>();

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
                safetyDisabledPawns ??= new HashSet<Pawn>();
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
