using System.Collections.Generic;
using NightSafety.Buildings;
using NightSafety.Core;
using RimWorld;
using Verse;

namespace NightSafety
{
    public sealed class NightSafetyMapComponent : MapComponent
    {
        private readonly HashSet<CompProtectionOven> ovens = new HashSet<CompProtectionOven>();

        public NightSafetyMapComponent(Map map) : base(map)
        {
        }

        public bool IsNight => NightSafetyMath.IsNight(GenLocalDate.HourFloat(map), 20f, 6f);

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            ovens.Clear();
            foreach (Thing thing in map.listerThings.ThingsOfDef(NightSafetyDefOf.NightSafety_ProtectionOven))
            {
                CompProtectionOven? oven = thing.TryGetComp<CompProtectionOven>();
                if (oven != null) ovens.Add(oven);
            }
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (!map.IsHashIntervalTick(250)) return;
            PruneOvens();
        }

        public void Register(CompProtectionOven oven)
        {
            if (oven.parent.Map == map) ovens.Add(oven);
        }

        public void Deregister(CompProtectionOven oven) => ovens.Remove(oven);

        public void PruneOvens()
        {
            ovens.RemoveWhere(oven => oven == null || !oven.parent.Spawned || oven.parent.Map != map);
        }

        public bool IsProtected(IntVec3 cell)
        {
            PruneOvens();
            foreach (CompProtectionOven oven in ovens)
            {
                if (oven.ActiveNow && NightSafetyMath.IsWithinRadius(
                    cell.x, cell.z, oven.parent.Position.x, oven.parent.Position.z, oven.Radius))
                    return true;
            }
            return false;
        }

        public bool IsPawnExposed(Pawn pawn)
        {
            return IsNight && pawn.Spawned && pawn.Map == map && !pawn.Dead && !IsProtected(pawn.Position);
        }
    }
}
