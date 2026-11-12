using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Vehicles;
using Verse;
using Verse.AI;

namespace Phobos.VehicleSystems;

/// <summary>Coupling a trailer (Framework 0.101.0): a colonist walks to the trailer a vehicle waits on and couples it, and the trailer is put
/// behind the vehicle.</summary>
public class WorkGiver_PhobosHitchUp : VehicleWorkGiver
{
    public override JobDef JobDef => VehicleSystemsJobDefOf.PhobosVehicleSystems_HitchUp;

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => Hitches.TrailersWaiting(pawn.Map);

    public override bool ShouldSkip(Pawn pawn, bool forced = false) => !Hitches.TrailersWaiting(pawn.Map).Any();

    public override bool CanBeWorkedOn(VehiclePawn vehicle) => Hitches.WaitingOn(vehicle) != null;
}

public class JobDriver_PhobosHitchUp : JobDriver_WorkVehicle, Phobos.Framework.Interface.IDrawsOwnWorkBar
{
    protected override JobDef JobDef => VehicleSystemsJobDefOf.PhobosVehicleSystems_HitchUp;

    protected override float TotalWork => PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>().hitchUpWork;

    protected override StatDef Stat => StatDefOf.GeneralLaborSpeed;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        // The vehicle gave up the hitch, or drove off: stop.
        this.FailOn(() => Hitches.WaitingOn(Vehicle) == null);
        foreach (Toil toil in WorkBars.WithBar(base.MakeNewToils(), EffecterDef != null, GetProgressPct)) yield return toil;
    }

    protected override void WorkComplete(Pawn actor)
    {
        Hitches.WaitingOn(Vehicle)?.CoupledBy(actor);
        ReadyForNextToil();
    }
}
