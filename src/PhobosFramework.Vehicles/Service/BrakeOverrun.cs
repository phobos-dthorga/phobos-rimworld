using System.Collections.Generic;
using HarmonyLib;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using SmashTools;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A land vehicle of the player's with no brakes, rolling on past where it meant to stop (Framework 0.188.0;
/// docs/development/vehicle-emergent-play.md, EM29(b); the owner's choice of 20 Nov 2026: "it overruns and hits things"). Begun when
/// Vehicle Framework says the vehicle has arrived; it then moves on a cell at a time, straight ahead as it was travelling, for as many
/// cells as its speed and weight carry it (OverrunRule). People in its way are struck and it goes on; a wall, a solid piece of cover or
/// another vehicle stops it, with damage to both. The map's edge and ground it cannot stand on stop it quietly. Derived, never saved: a
/// game loaded while one rolls finds it stopped where it was. Other factions' vehicles never overrun: they run no stages.
/// </summary>
public sealed class BrakeOverrun
{
    /// <summary>Cover this solid or more stops a rolling vehicle; lower things are driven over. Authored.</summary>
    const float StoppingFill = 0.4f;

    readonly CompPhobosVehicleSystems comp;
    readonly IntVec3 step;
    readonly double cellsPerSecond, kilograms;
    int left, wait;
    bool struck;

    BrakeOverrun(CompPhobosVehicleSystems comp, IntVec3 step, double cellsPerSecond, double kilograms, int cells)
    {
        this.comp = comp;
        this.step = step;
        this.cellsPerSecond = cellsPerSecond;
        this.kilograms = kilograms;
        left = cells;
        wait = OverrunRule.TicksPerCell(cellsPerSecond);
    }

    /// <summary>Vehicle Framework says this vehicle has arrived, facing as it travelled: with no brakes, it starts to roll on.</summary>
    public static void OnArrived(VehiclePawn vehicle, Rot4 travelling)
    {
        if (!vehicle.Spawned || vehicle.Faction != Faction.OfPlayer || vehicle.VehicleDef.type != VehicleType.Land || OnMapFlight.Airborne(vehicle)) return;
        if (CompPhobosVehicleSystems.Of(vehicle) is not { Usable: true } comp || comp.Overrun != null || !comp.NoBrakes()) return;
        double speed = vehicle.GetStatValue(VehicleStatDefOf.MoveSpeed) * comp.SpeedCap;
        double kilograms = CompPhobosSling.LoadKilograms(vehicle);
        int cells = OverrunRule.Cells(speed, kilograms);
        if (cells <= 0) return;
        comp.Overrun = new BrakeOverrun(comp, travelling.FacingCell, speed, kilograms, cells);
        Messages.Message("PhobosVehicleSystems_OverrunStart".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve(), vehicle, MessageTypeDefOf.ThreatSmall, historical: false);
    }

    /// <summary>Each tick while it rolls: every so often it moves on one cell, or stops against what is in the way.</summary>
    public void Tick()
    {
        VehiclePawn vehicle = comp.Vehicle;
        // An order to move again, or leaving the map, ends it: the driver has it going somewhere.
        if (!vehicle.Spawned || vehicle.Destroyed || vehicle.vehiclePather is { Moving: true })
        {
            comp.Overrun = null;
            return;
        }
        if (--wait > 0) return;
        wait = OverrunRule.TicksPerCell(cellsPerSecond);
        Map map = vehicle.Map;
        IntVec3 cell = vehicle.Position + step;
        CellRect next = vehicle.VehicleRect(cell, vehicle.Rotation);
        if (!next.InBounds(map))
        {
            End();
            return;
        }
        if (Blocker(vehicle, next, map) is { } blocker)
        {
            Strike(vehicle, blocker);
            End();
            return;
        }
        foreach (IntVec3 c in next)
        {
            if (c.Walkable(map)) continue;
            End();
            return;
        }
        foreach (Pawn pawn in InTheWay(next, map)) Strike(vehicle, pawn);
        if (vehicle.Destroyed || !vehicle.Spawned)
        {
            comp.Overrun = null;
            return;
        }
        VehicleShift.MoveOneCell(vehicle, cell);
        if (--left <= 0) End();
    }

    /// <summary>What stops it dead in these cells: another vehicle, or a wall or solid piece of cover.</summary>
    static Thing? Blocker(VehiclePawn vehicle, CellRect cells, Map map)
    {
        foreach (IntVec3 c in cells)
        {
            List<Thing> things = c.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i] is VehiclePawn other && other != vehicle) return other;
            }
            if (c.GetEdifice(map) is { } edifice && (edifice.def.passability == Traversability.Impassable || edifice.def.fillPercent >= StoppingFill)) return edifice;
        }
        return null;
    }

    /// <summary>People and animals standing in these cells, not aboard anything.</summary>
    static List<Pawn> InTheWay(CellRect cells, Map map)
    {
        var found = new List<Pawn>();
        foreach (IntVec3 c in cells)
        {
            List<Thing> things = c.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i] is Pawn pawn && pawn is not VehiclePawn && !pawn.Dead && !found.Contains(pawn)) found.Add(pawn);
            }
        }
        return found;
    }

    void Strike(VehiclePawn vehicle, Thing hit)
    {
        struck = true;
        float damage = (float)OverrunRule.ImpactDamage(cellsPerSecond, kilograms);
        string name = hit.LabelShort;
        if (!hit.Destroyed) hit.TakeDamage(new DamageInfo(DamageDefOf.Blunt, damage, instigator: vehicle));
        // The vehicle takes half the blow, on the end that struck: the cell of its body furthest ahead.
        CellRect body = vehicle.OccupiedRect();
        var front = new IntVec2(step.x * (body.Width - 1) / 2, step.z * (body.Height - 1) / 2);
        if (!vehicle.Destroyed) vehicle.TakeDamage(new DamageInfo(DamageDefOf.Blunt, damage * 0.5f), front);
        Messages.Message("PhobosVehicleSystems_OverrunStruck".Translate(vehicle.LabelShort.Named("VEHICLE"), name.Named("THING")).Resolve(),
            new LookTargets(vehicle), MessageTypeDefOf.NegativeEvent);
        VehicleStory.Log(comp, VehicleStoryDefOf.PhobosLog_Struck, subject: name);
        if (Seen.Wants("vehicles.brake-overrun", vehicle, "struck")) Seen.Note("vehicles.brake-overrun", vehicle, name, "struck");
    }

    void End()
    {
        comp.Overrun = null;
        if (struck) return;
        VehicleStory.Log(comp, VehicleStoryDefOf.PhobosLog_RanOn);
        Seen.Note("vehicles.brake-overrun", comp.Vehicle, variant: "rolled");
    }
}

/// <summary>Moving a standing vehicle one cell without its pather, as Vehicle Framework moves it a cell: the winch's drag and a brakeless
/// overrun share it (Framework 0.188.0).</summary>
public static class VehicleShift
{
    public static void MoveOneCell(VehiclePawn vehicle, IntVec3 cell)
    {
        Map map = vehicle.Map;
        CellRect before = vehicle.OccupiedRect();
        CellRect next = vehicle.VehicleRect(cell, vehicle.Rotation);
        // Vehicle Framework's own patches on Position reclaim the cells and its regions.
        vehicle.Position = cell;
        // The cell it was heading for goes with it, or it turns back to face it at the next load; what it laid where it stood comes up.
        vehicle.vehiclePather?.ResetToCurrentPosition();
        Hitches.TakeUpPipes(vehicle);
        foreach (IntVec3 c in before.Cells)
        {
            if (c.InBounds(map)) map.pathing.RecalculatePerceivedPathCostAt(c);
        }
        foreach (IntVec3 c in next.Cells)
        {
            if (c.InBounds(map)) map.pathing.RecalculatePerceivedPathCostAt(c);
        }
    }
}

/// <summary>Vehicle Framework's arrival starts a brakeless vehicle rolling on (Framework 0.188.0, EM29(b)). The prefix only notes which
/// way it was travelling, before the arrival turns it to face as ordered; neither skips nor changes the arrival.</summary>
[HarmonyPatch(typeof(VehiclePathFollower), "PatherArrived")]
public static class BrakeOverrunOnArrival
{
    public static void Prefix(VehiclePawn ___vehicle, out Rot4 __state) => __state = ___vehicle != null ? ___vehicle.Rotation : Rot4.North;

    public static void Postfix(VehiclePawn ___vehicle, Rot4 __state)
    {
        if (___vehicle != null) BrakeOverrun.OnArrived(___vehicle, __state);
    }
}
