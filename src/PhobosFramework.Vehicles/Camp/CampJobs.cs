using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Vehicles;
using Verse;
using Verse.AI;

namespace Phobos.VehicleSystems;

/// <summary>
/// The vehicles whose camp waits for a colonist (Framework 0.212.0): kept by the camps themselves as orders are given and done, so the work giver
/// reads a short list. Emptied when another game is loaded.
/// </summary>
public static class CampOrders
{
    static readonly HashSet<CompPhobosCamp> waiting = new();
    static Game? game;

    static void Check()
    {
        if (game == Current.Game) return;
        waiting.Clear();
        game = Current.Game;
    }

    public static void Note(CompPhobosCamp camp, bool isWaiting)
    {
        Check();
        if (isWaiting) waiting.Add(camp);
        else waiting.Remove(camp);
    }

    /// <summary>The camp with work waiting on this vehicle, or null.</summary>
    public static CompPhobosCamp? On(VehiclePawn vehicle)
    {
        Check();
        foreach (CompPhobosCamp camp in waiting)
        {
            if (camp.Vehicle == vehicle && camp.Order != CampOrder.None) return camp;
        }
        return null;
    }

    /// <summary>The vehicles on this map with camp work waiting.</summary>
    public static IEnumerable<Thing> Vehicles(Map map)
    {
        Check();
        foreach (CompPhobosCamp camp in waiting)
        {
            if (camp.Order != CampOrder.None && camp.Vehicle is { Spawned: true } vehicle && vehicle.Map == map) yield return vehicle;
        }
    }
}

/// <summary>A colonist sets up or packs up a vehicle's camp (Construction work: the awning out, the roof tent unfolded, swags rolled out).</summary>
public class WorkGiver_PhobosCamp : VehicleWorkGiver
{
    public override JobDef JobDef => VehicleCampDefOf.PhobosVehicleSystems_CampWork;

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => CampOrders.Vehicles(pawn.Map);

    public override bool ShouldSkip(Pawn pawn, bool forced = false) => !CampOrders.Vehicles(pawn.Map).Any();

    public override bool CanBeWorkedOn(VehiclePawn vehicle) => CampOrders.On(vehicle) is { } camp && camp.OrderRefusal() == null;
}

public class JobDriver_PhobosCamp : PhobosResumableVehicleWork, Phobos.Framework.Interface.IDrawsOwnWorkBar
{
    protected override JobDef JobDef => VehicleCampDefOf.PhobosVehicleSystems_CampWork;

    protected override float TotalWork => CampOrders.On(Vehicle)?.Work ?? 1f;

    /// <summary>Pitching camp and packing it up keep their own work.</summary>
    protected override string ResumeOrder => CampOrders.On(Vehicle)?.Order.ToString() ?? "";

    protected override StatDef Stat => StatDefOf.ConstructionSpeed;

    protected override SkillDef Skill => SkillDefOf.Construction;

    /// <summary>The job's report says which: the JobDef's own words are for setting up.</summary>
    public override string GetReport() => CampOrders.On(Vehicle)?.Order == CampOrder.PackUp
        ? "PhobosCamp_ReportPackingUp".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve()
        : base.GetReport();

    protected override IEnumerable<Toil> MakeNewToils()
    {
        // The order was called off, or the vehicle was moved or taken away: stop.
        this.FailOn(() => CampOrders.On(Vehicle) is not { } camp || camp.OrderRefusal() != null);
        foreach (Toil toil in WorkBars.WithBar(base.MakeNewToils(), EffecterDef != null, GetProgressPct)) yield return toil;
    }

    protected override void WorkComplete(Pawn actor)
    {
        CampOrders.On(Vehicle)?.WorkDone();
        ReadyForNextToil();
    }
}

/// <summary>
/// A sleeper wakes with their bed's own memory (Framework 0.212.0; the record's Y8 and B1): "slept under the stars" from a swag on a fair, mild
/// night with nothing overhead, "slept in a roof tent" from the roof tent. The game gives its bed memories in one private method with no
/// extension point, so this postfix adds ours after its own, where they can answer its "slept outside". It only adds; nothing of the game's is
/// skipped or taken away.
/// </summary>
[HarmonyPatch(typeof(Toils_LayDown), "ApplyBedThoughts")]
public static class CampBedMemories
{
    public static void Postfix(Pawn actor, Building_Bed bed)
    {
        if (bed?.def.GetModExtension<PhobosBedMemoryExtension>() is not { memory: { } memory } extension) return;
        if (actor.needs?.mood?.thoughts?.memories is not { } memories) return;
        memories.RemoveMemoriesOfDef(memory);
        if (actor.IsWildMan()) return;
        Map? map = bed.MapHeld;
        if (map == null) return;
        if (extension.openSkyOnly && bed.OccupiedRect().Cells.Any(c => c.Roofed(map))) return;
        if (extension.fairWeatherOnly && (map.weatherManager.RainRate > 0.01f || map.weatherManager.SnowRate > 0.01f)) return;
        if (extension.comfortableOnly && (memories.GetFirstMemoryOfDef(ThoughtDefOf.SleptInCold) != null || memories.GetFirstMemoryOfDef(ThoughtDefOf.SleptInHeat) != null)) return;
        memories.TryGainMemory(memory);
        if (Seen.Wants("vehicles.camp-bed-memory", actor, memory.defName)) Seen.Note("vehicles.camp-bed-memory", actor, memory.defName, memory.defName);
    }
}
