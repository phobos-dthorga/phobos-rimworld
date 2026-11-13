using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Vehicles;
using Verse;
using Verse.AI;

namespace Phobos.VehicleSystems;

/// <summary>
/// The vehicles waiting for a colonist to set them up or pack them up by hand (Framework 0.111.0; docs/development/vehicle-trailers.md, B0.1): kept by the
/// vehicles themselves as orders are given and done, so the work giver reads a short list. Emptied when another game is loaded.
/// </summary>
public static class HandSetUps
{
    static readonly HashSet<CompPhobosSetUp> waiting = new();
    static Game? game;

    static void Check()
    {
        if (game == Current.Game) return;
        waiting.Clear();
        game = Current.Game;
    }

    public static void Note(CompPhobosSetUp setUp, bool isWaiting)
    {
        Check();
        if (isWaiting) waiting.Add(setUp);
        else waiting.Remove(setUp);
    }

    /// <summary>The waiting order's comp on this vehicle, or null.</summary>
    public static CompPhobosSetUp? On(VehiclePawn vehicle)
    {
        Check();
        foreach (CompPhobosSetUp setUp in waiting)
        {
            if (setUp.Vehicle == vehicle && setUp.HandOrder != HandSetUpOrder.None) return setUp;
        }
        return null;
    }

    /// <summary>The vehicles on this map with work waiting.</summary>
    public static IEnumerable<Thing> Vehicles(Map map)
    {
        Check();
        foreach (CompPhobosSetUp setUp in waiting)
        {
            if (setUp.HandOrder != HandSetUpOrder.None && setUp.Vehicle is { Spawned: true } vehicle && vehicle.Map == map) yield return vehicle;
        }
    }
}

/// <summary>A colonist sets up or packs up a trailer that waits for it (Construction work: legs down, chocks in, hoses out).</summary>
public class WorkGiver_PhobosTrailerSetUp : VehicleWorkGiver
{
    public override JobDef JobDef => VehicleSystemsJobDefOf.PhobosVehicleSystems_TrailerSetUp;

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => HandSetUps.Vehicles(pawn.Map);

    public override bool ShouldSkip(Pawn pawn, bool forced = false) => !HandSetUps.Vehicles(pawn.Map).Any();

    public override bool CanBeWorkedOn(VehiclePawn vehicle) => HandSetUps.On(vehicle) is { } setUp && setUp.HandOrderRefusal() == null;
}

public class JobDriver_PhobosTrailerSetUp : JobDriver_WorkVehicle, Phobos.Framework.Interface.IDrawsOwnWorkBar
{
    protected override JobDef JobDef => VehicleSystemsJobDefOf.PhobosVehicleSystems_TrailerSetUp;

    protected override float TotalWork => HandSetUps.On(Vehicle)?.HandWork ?? 1f;

    protected override StatDef Stat => StatDefOf.ConstructionSpeed;

    protected override SkillDef Skill => SkillDefOf.Construction;

    /// <summary>The job's report says which: the JobDef's own words are for setting up.</summary>
    public override string GetReport() => HandSetUps.On(Vehicle)?.HandOrder == HandSetUpOrder.PackUp
        ? "PhobosTrailer_ReportPackingUp".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve()
        : base.GetReport();

    protected override IEnumerable<Toil> MakeNewToils()
    {
        // The order was called off, or the trailer was hitched or taken away: stop.
        this.FailOn(() => HandSetUps.On(Vehicle) is not { } setUp || setUp.HandOrderRefusal() != null);
        foreach (Toil toil in WorkBars.WithBar(base.MakeNewToils(), EffecterDef != null, GetProgressPct)) yield return toil;
    }

    protected override void WorkComplete(Pawn actor)
    {
        HandSetUps.On(Vehicle)?.HandWorkDone(actor);
        ReadyForNextToil();
    }
}
