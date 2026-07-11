using RimWorld;
using Verse;
using Verse.AI;

namespace NightSafety.AI
{
    public sealed class ThinkNode_ConditionalSeekProtection : ThinkNode_Conditional
    {
        protected override bool Satisfied(Pawn pawn)
        {
            if (pawn.Map == null) return false;
            NightSafetyMapComponent component = pawn.Map.GetComponent<NightSafetyMapComponent>();
            bool exposed = component.IsPawnExposed(pawn);
            return Core.SafetyEligibilityPolicy.CanSeekSafety(
                pawn.Spawned,
                pawn.Faction == Faction.OfPlayer,
                pawn.Drafted,
                pawn.Downed,
                pawn.InMentalState,
                pawn.IsPrisoner,
                pawn.IsSlave,
                pawn.CurJob?.playerForced == true,
                Current.Game?.GetComponent<Core.NightSafetyGameComponent>()?.IsSafetySeekingEnabled(pawn) ?? true,
                exposed,
                component.IsSafetyBackoffActive(pawn));
        }
    }
}
