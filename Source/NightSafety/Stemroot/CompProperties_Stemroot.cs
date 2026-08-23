using System.Collections.Generic;
using Verse;

namespace NightSafety.Stemroot
{
    public sealed class CompProperties_Stemroot : CompProperties
    {
        // Overlay art per state. These are drawn on top of the linked wall graphic rather than
        // being separate wall sets, which is why there is one wall atlas and four small pieces.
        public string bleedingOverlayPath = null!;
        public string bloomingOverlayPath = null!;
        public string fruitingOverlayPath = null!;
        public string flushingOverlayPath = null!;

        public CompProperties_Stemroot()
        {
            compClass = typeof(CompStemroot);
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef)) yield return error;

            if (bleedingOverlayPath.NullOrEmpty()) yield return parentDef.defName + " has no bleeding overlay path";
            if (bloomingOverlayPath.NullOrEmpty()) yield return parentDef.defName + " has no blooming overlay path";
            if (fruitingOverlayPath.NullOrEmpty()) yield return parentDef.defName + " has no fruiting overlay path";
            if (flushingOverlayPath.NullOrEmpty()) yield return parentDef.defName + " has no flushing overlay path";
        }
    }
}
