using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using NightSafety.Lords;

namespace NightSafety.Buildings
{
    public sealed class CompProtectionOven : ThingComp
    {
        private CompRefuelable? refuelable;

        public CompProperties_ProtectionOven Props => (CompProperties_ProtectionOven)props;
        public float Radius => Props.radius;
        public bool HasFuel => refuelable?.HasFuel == true;
        public bool ActiveNow => parent.Spawned && parent.Map?.GetComponent<NightSafetyMapComponent>()?.IsNight == true && HasFuel;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            refuelable = parent.GetComp<CompRefuelable>();
            parent.Map?.GetComponent<NightSafetyMapComponent>()?.Register(this);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            map.GetComponent<NightSafetyMapComponent>().Deregister(this);
            base.PostDeSpawn(map, mode);
        }

        public override void ReceiveCompSignal(string signal)
        {
            base.ReceiveCompSignal(signal);
            if (signal == CompRefuelable.RefueledSignal || signal == CompRefuelable.RanOutOfFuelSignal)
            {
                parent.MapHeld?.GetComponent<NightSafetyMapComponent>()?.PruneOvens();
            }
        }

        public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            // Harassers are ambient pressure and must never invalidate the core safety loop by destroying its oven.
            absorbed = dinfo.Instigator is Pawn pawn && pawn.GetLord()?.LordJob is LordJob_NightHarassers;
        }

        public override string CompInspectStringExtra()
        {
            string state = ActiveNow
                ? "NightSafety_OvenActive".Translate()
                : !HasFuel ? "NightSafety_OvenNoFuel".Translate() : "NightSafety_OvenDaylight".Translate();
            return "NightSafety_OvenInspect".Translate(Radius.ToString("0.#"), state);
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            GenDraw.DrawRadiusRing(parent.Position, Radius, ActiveNow ? Color.green : Color.gray);
        }
    }
}
