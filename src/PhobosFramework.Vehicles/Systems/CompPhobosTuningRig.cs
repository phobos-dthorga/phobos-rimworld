using Phobos.Framework.Environment;
using Phobos.Framework.Interface;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using Vehicles;

namespace Phobos.VehicleSystems;

/// <summary>Makes a building a dyno bay: engines are tuned, and parts changed for another grade, only on a vehicle standing within
/// <c>reach</c> of it while it is powered. It is also where bay trips take vehicles, and what makes a service yard work. Goes on a
/// building's ThingDef, with a <c>CompPowerTrader</c> (without one it always counts as running) and, to count as a service yard's
/// dyno, <c>PhobosYardEquipmentExtension</c>.</summary>
public sealed class CompProperties_PhobosTuningRig : CompProperties
{
    /// <summary>How many cells from the rig's own edge a vehicle may stand. Authored.</summary>
    public int reach = 3;

    public CompProperties_PhobosTuningRig() => compClass = typeof(CompPhobosTuningRig);
}

/// <summary>What a tuning rig knows: whether it is running and whether a vehicle is close enough. The rule that tunes need one is in <see cref="VehicleFittings"/>.</summary>
public sealed class CompPhobosTuningRig : ThingComp
{
    CompPowerTrader? power;

    public CompProperties_PhobosTuningRig Props => (CompProperties_PhobosTuningRig)props;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        power = parent.GetComp<CompPowerTrader>();
        MapRegistry<CompPhobosTuningRig>.Add(parent.Map, this);
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        base.PostDeSpawn(map, mode);
        MapRegistry<CompPhobosTuningRig>.Remove(map, this);
    }

    /// <summary>The player's tuning rigs on a map (Framework 0.130.0, step S8: from the map's own list, not a walk of every building).</summary>
    public static IEnumerable<CompPhobosTuningRig> PlayersOn(Map map)
    {
        IReadOnlyList<CompPhobosTuningRig> rigs = MapRegistry<CompPhobosTuningRig>.On(map);
        for (int i = 0; i < rigs.Count; i++)
        {
            if (rigs[i].parent.Faction == Faction.OfPlayer) yield return rigs[i];
        }
    }

    public bool Working => power == null || power.PowerOn;

    public bool Reaches(CellRect area) => area.ExpandedBy(Props.reach).Overlaps(parent.OccupiedRect());

    /// <summary>The player's vehicles standing in reach, with something to fit or tune (Framework 0.90.0): the same reach the vehicles'
    /// own fittings check, measured from the cells their pictures cover.</summary>
    Phobos.Framework.Rules.Expiring<List<CompPhobosVehicleSystems>> inReach;

    /// <summary>Kept for a moment: the inspect text and the button both ask every frame while the bay is selected, and the answer walks
    /// every pawn on the map (the performance pass, Framework 0.94.0).</summary>
    public List<CompPhobosVehicleSystems> VehiclesInReach() => inReach.TryGet(RealTime.LastRealTime, out List<CompPhobosVehicleSystems> held) ? held
        : inReach.Set(FindVehiclesInReach(), RealTime.LastRealTime, VehicleFittings.DisplayCacheSeconds);

    List<CompPhobosVehicleSystems> FindVehiclesInReach()
    {
        var found = new List<CompPhobosVehicleSystems>();
        if (!parent.Spawned) return found;
        foreach (Pawn pawn in parent.Map.mapPawns.AllPawnsSpawned)
        {
            if (pawn is not VehiclePawn vehicle || vehicle.Faction != Faction.OfPlayer) continue;
            if (vehicle.GetComp<CompPhobosVehicleSystems>() is not { Usable: true } comp || !comp.Fittings.AnythingToFit) continue;
            if (Reaches(VehicleFittings.DrawnArea(vehicle))) found.Add(comp);
        }
        return found;
    }

    public override string? CompInspectStringExtra()
    {
        // Guarded: the game asks for this every frame while the thing is selected (UiGuard).
        if (UiGuard.Resting(GuardKey)) return UiGuard.FallbackText;
        try { return InspectLine(); }
        catch (System.Exception e) { UiGuard.Report(GuardKey, e); return null; }
    }

    const string GuardKey = "A dyno bay's line";

    string InspectLine()
    {
        string state = (Working
            ? "PhobosVehicleSystems_RigInspectReady".Translate(Props.reach.Named("CELLS"))
            : "PhobosVehicleSystems_RigInspectOff".Translate()).Resolve();
        // A yard's dyno bay says how its line stands (Framework 0.210.0): who is on it, how many wait.
        if (ServiceYards.For(parent.Map)?.OwnerOf(parent) is { Works: true } yard)
        {
            state += "\n" + "PhobosServiceYard_RigLine".Translate(yard.Yard.label.Named("YARD"), yard.OnDyno.Named("ON"), yard.Waiting.Named("WAITING"),
                ServiceYards.TurnsOf(this).Named("TURNS")).Resolve();
        }
        List<CompPhobosVehicleSystems> inReach = VehiclesInReach();
        if (inReach.Count == 0) return state;
        string names = inReach.Select(c => c.Vehicle.LabelShort).ToCommaList(useAnd: true);
        int waiting = inReach.Sum(c => c.Fittings.Orders.Count);
        return state + "\n" + (waiting > 0
            ? "PhobosVehicleSystems_RigInspectInReachWaiting".Translate(names.Named("VEHICLES"), waiting.Named("COUNT"))
            : "PhobosVehicleSystems_RigInspectInReach".Translate(names.Named("VEHICLES"))).Resolve();
    }

    /// <summary>The bay's own button: every vehicle in reach, each opening its fittings and tunes. Presentation only.</summary>
    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        if (parent.Faction != Faction.OfPlayer) yield break;
        List<CompPhobosVehicleSystems> inReach = VehiclesInReach();
        var command = new Command_Action
        {
            defaultLabel = "PhobosVehicleSystems_RigVehiclesGizmo".Translate(),
            defaultDesc = "PhobosVehicleSystems_RigVehiclesGizmoDesc".Translate(Props.reach.Named("CELLS")),
            icon = SystemIcons.Fittings,
            action = () => Find.WindowStack.Add(new FloatMenu(inReach.Select(comp =>
            {
                int waiting = comp.Fittings.Orders.Count;
                string label = (waiting > 0
                    ? "PhobosVehicleSystems_RigVehicleOptionWaiting".Translate(comp.Vehicle.LabelShortCap.Named("VEHICLE"), waiting.Named("COUNT"))
                    : "PhobosVehicleSystems_RigVehicleOption".Translate(comp.Vehicle.LabelShortCap.Named("VEHICLE"))).Resolve();
                return new FloatMenuOption(label, () => Find.WindowStack.Add(new Dialog_PhobosVehicleFittings(comp)));
            }).ToList())),
        };
        if (inReach.Count == 0) command.Disable("PhobosVehicleSystems_RigNoVehicles".Translate(Props.reach.Named("CELLS")).Resolve());
        yield return command;
    }
}
