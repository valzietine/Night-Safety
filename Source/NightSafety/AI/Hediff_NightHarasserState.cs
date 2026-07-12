using NightSafety.Core;
using RimWorld;
using Verse;

namespace NightSafety.AI
{
    // RimWorld Together's synchronous map serializer transfers pawns and hediffs but omits LordManager.
    // Keep only the minimum pack identity on each pawn so a transferred map can rebuild its owner.
    public sealed class Hediff_NightHarasserState : Hediff
    {
        private HarassmentTheme theme;
        private IntVec3 harassmentPoint;
        private ThingDef? effigyStuff;
        private bool retreating;

        public override bool Visible => false;
        public HarassmentTheme Theme => theme;
        public IntVec3 HarassmentPoint => harassmentPoint;
        public ThingDef EffigyStuff => effigyStuff ?? ThingDefOf.WoodLog;
        public bool Retreating => retreating;

        public void Initialize(HarassmentTheme selectedTheme, IntVec3 point, ThingDef? selectedEffigyStuff, bool isRetreating = false)
        {
            theme = selectedTheme;
            harassmentPoint = point;
            effigyStuff = selectedEffigyStuff;
            retreating = isRetreating;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref theme, "nightSafetyHarassmentTheme", HarassmentTheme.Arson);
            Scribe_Values.Look(ref harassmentPoint, "nightSafetyHarassmentPoint");
            Scribe_Defs.Look(ref effigyStuff, "nightSafetyEffigyStuff");
            Scribe_Values.Look(ref retreating, "nightSafetyRetreating", false);
        }
    }
}
