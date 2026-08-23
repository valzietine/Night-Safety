using RimWorld;
using UnityEngine;
using Verse;

namespace NightSafety.Stemroot
{
    /// <summary>
    /// Shared drag-and-designate behaviour for the two stemroot orders. Stemroot is a building
    /// rather than a plant, so the vanilla plant designators cannot see it and these stand in.
    /// </summary>
    public abstract class Designator_Stemroot : Designator
    {
        protected Designator_Stemroot()
        {
            soundDragSustain = SoundDefOf.Designate_DragStandard;
            soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
            useMouseIcon = true;
            hotKey = KeyBindingDefOf.Misc1;
        }

        public override DrawStyleCategoryDef DrawStyleCategory => DrawStyleCategoryDefOf.Plants;

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            if (!loc.InBounds(Map) || loc.Fogged(Map)) return false;

            Thing? stemroot = StemrootIn(loc);
            if (stemroot == null) return "NightSafety_MessageMustDesignateStemroot".Translate();
            return CanDesignateThing(stemroot);
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            Thing? stemroot = StemrootIn(c);
            if (stemroot != null) DesignateThing(stemroot);
        }

        public override void DesignateThing(Thing t)
        {
            Map.designationManager.RemoveAllDesignationsOn(t);
            Map.designationManager.AddDesignation(new Designation(t, Designation));
        }

        public override void SelectedUpdate() => GenUI.RenderMouseoverBracket();

        protected Thing? StemrootIn(IntVec3 cell)
        {
            Building edifice = cell.GetEdifice(Map);
            return edifice?.def == NightSafetyDefOf.NightSafety_Stemroot ? edifice : null;
        }

        protected bool AlreadyDesignated(Thing t) =>
            Map.designationManager.DesignationOn(t, Designation) != null;
    }

    public sealed class Designator_CutStemroot : Designator_Stemroot
    {
        public Designator_CutStemroot()
        {
            defaultLabel = "NightSafety_DesignatorCutStemroot".Translate();
            defaultDesc = "NightSafety_DesignatorCutStemrootDesc".Translate();
            icon = ContentFinder<Texture2D>.Get("UI/Designators/CutPlants");
            soundSucceeded = SoundDefOf.Designate_CutPlants;
        }

        protected override DesignationDef Designation => NightSafetyDefOf.NightSafety_CutStemrootDesignation;

        public override AcceptanceReport CanDesignateThing(Thing t)
        {
            if (t.def != NightSafetyDefOf.NightSafety_Stemroot) return false;
            return !AlreadyDesignated(t);
        }
    }

    public sealed class Designator_HarvestStemroot : Designator_Stemroot
    {
        public Designator_HarvestStemroot()
        {
            defaultLabel = "NightSafety_DesignatorHarvestStemroot".Translate();
            defaultDesc = "NightSafety_DesignatorHarvestStemrootDesc".Translate();
            icon = ContentFinder<Texture2D>.Get("UI/Designators/Harvest");
            soundSucceeded = SoundDefOf.Designate_HarvestPlants;
        }

        protected override DesignationDef Designation => NightSafetyDefOf.NightSafety_HarvestStemrootDesignation;

        public override AcceptanceReport CanDesignateThing(Thing t)
        {
            if (t.def != NightSafetyDefOf.NightSafety_Stemroot) return false;
            if (AlreadyDesignated(t)) return false;
            if (t.TryGetComp<CompStemroot>()?.CanHarvestNow != true)
                return "NightSafety_MessageNothingToHarvest".Translate();
            return true;
        }
    }
}
