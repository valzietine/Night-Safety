using NightSafety.Core;
using RimWorld;
using Verse;

namespace NightSafety.UI
{
    public sealed class PawnColumnWorker_SafetySeeking : PawnColumnWorker_Checkbox
    {
        protected override bool HasCheckbox(Pawn pawn)
        {
            return pawn.Faction == Faction.OfPlayer && pawn.RaceProps.Humanlike && !pawn.IsPrisoner && !pawn.IsSlave;
        }

        protected override bool GetValue(Pawn pawn)
        {
            return Current.Game?.GetComponent<NightSafetyGameComponent>()?.IsSafetySeekingEnabled(pawn) ?? true;
        }

        protected override void SetValue(Pawn pawn, bool value, PawnTable table)
        {
            NightSafetyPreferences.SetSafetySeekingEnabled(pawn, value);
        }

        protected override string GetTip(Pawn pawn)
        {
            return "NightSafety_SafetySeekingTip".Translate(pawn.LabelShortCap);
        }
    }
}
