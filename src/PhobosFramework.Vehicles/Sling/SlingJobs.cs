using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Vehicles;
using Verse;
using Verse.AI;

namespace Phobos.VehicleSystems;

/// <summary>The helicopter waiting on this load, if any (sling loads: the load is the job's target, the helicopter is found from it).</summary>
public static class SlingWaits
{
    public static CompPhobosSling? WaitingOn(VehiclePawn load)
    {
        if (!load.Spawned) return null;
        foreach (Pawn pawn in load.Map.mapPawns.AllPawnsSpawned)
        {
            if (pawn is VehiclePawn helicopter && helicopter.GetComp<CompPhobosSling>() is { } sling && sling.Hooked == load && sling.AwaitingCrew) return sling;
        }
        return null;
    }
}

/// <summary>Hooking a slung load up (Framework 0.92.0): a colonist walks to the vehicle a helicopter waits on and fits the strops.</summary>
public class WorkGiver_PhobosSlingHookUp : VehicleWorkGiver
{
    public override JobDef JobDef => VehicleSystemsJobDefOf.PhobosVehicleSystems_SlingHookUp;

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) =>
        pawn.Map.mapPawns.AllPawnsSpawned.OfType<VehiclePawn>().Where(v => SlingWaits.WaitingOn(v) != null).Cast<Thing>().ToList();

    public override bool CanBeWorkedOn(VehiclePawn vehicle) => SlingWaits.WaitingOn(vehicle) != null;
}

public class JobDriver_PhobosSlingHookUp : JobDriver_WorkVehicle
{
    protected override JobDef JobDef => VehicleSystemsJobDefOf.PhobosVehicleSystems_SlingHookUp;

    protected override float TotalWork => PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>().slingHookUpWork;

    protected override StatDef Stat => StatDefOf.GeneralLaborSpeed;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        // The helicopter gave up the lift, or flew off: stop.
        this.FailOn(() => SlingWaits.WaitingOn(Vehicle) == null);
        foreach (Toil toil in base.MakeNewToils()) yield return toil;
    }

    protected override void WorkComplete(Pawn actor)
    {
        SlingWaits.WaitingOn(Vehicle)?.HookedUp(actor);
        ReadyForNextToil();
    }
}
