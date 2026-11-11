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
    /// <summary>The slings waiting for a crew, kept by the slings themselves as they are chosen and hooked up: a hauler looking for work
    /// reads this short list, where it used to walk every pawn on the map for every vehicle on it (the performance pass, Framework 0.94.0).
    /// Emptied when another game is loaded.</summary>
    static readonly HashSet<CompPhobosSling> waiting = new();
    static Game? waitingGame;

    static HashSet<CompPhobosSling> Waiting
    {
        get
        {
            if (waitingGame != Current.Game)
            {
                waiting.Clear();
                waitingGame = Current.Game;
            }
            return waiting;
        }
    }

    public static void Note(CompPhobosSling sling, bool isWaiting)
    {
        if (isWaiting) Waiting.Add(sling);
        else Waiting.Remove(sling);
    }

    static bool StillWaiting(CompPhobosSling sling) => sling.AwaitingCrew && sling.Vehicle is { Spawned: true } && sling.Hooked is { Spawned: true };

    public static CompPhobosSling? WaitingOn(VehiclePawn load)
    {
        foreach (CompPhobosSling sling in Waiting)
        {
            if (sling.Hooked == load && StillWaiting(sling)) return sling;
        }
        return null;
    }

    /// <summary>The loads on this map that wait for someone to hook them up.</summary>
    public static IEnumerable<Thing> LoadsWaiting(Map map)
    {
        foreach (CompPhobosSling sling in Waiting)
        {
            if (StillWaiting(sling) && sling.Hooked!.Map == map) yield return sling.Hooked;
        }
    }
}

/// <summary>Hooking a slung load up (Framework 0.92.0): a colonist walks to the vehicle a helicopter waits on and fits the strops.</summary>
public class WorkGiver_PhobosSlingHookUp : VehicleWorkGiver
{
    public override JobDef JobDef => VehicleSystemsJobDefOf.PhobosVehicleSystems_SlingHookUp;

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => SlingWaits.LoadsWaiting(pawn.Map);

    public override bool ShouldSkip(Pawn pawn, bool forced = false) => !SlingWaits.LoadsWaiting(pawn.Map).Any();

    public override bool CanBeWorkedOn(VehiclePawn vehicle) => SlingWaits.WaitingOn(vehicle) != null;
}

public class JobDriver_PhobosSlingHookUp : JobDriver_WorkVehicle, Phobos.Framework.Interface.IDrawsOwnWorkBar
{
    protected override JobDef JobDef => VehicleSystemsJobDefOf.PhobosVehicleSystems_SlingHookUp;

    protected override float TotalWork => PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>().slingHookUpWork;

    protected override StatDef Stat => StatDefOf.GeneralLaborSpeed;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        // The helicopter gave up the lift, or flew off: stop.
        this.FailOn(() => SlingWaits.WaitingOn(Vehicle) == null);
        foreach (Toil toil in WorkBars.WithBar(base.MakeNewToils(), EffecterDef != null, GetProgressPct)) yield return toil;
    }

    protected override void WorkComplete(Pawn actor)
    {
        SlingWaits.WaitingOn(Vehicle)?.HookedUp(actor);
        ReadyForNextToil();
    }
}
