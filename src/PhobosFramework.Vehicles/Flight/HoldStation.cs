using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// An aircraft holding station over a world tile (Framework 0.357.0; docs/development/electronic-warfare.md, EW7; the owner's card of 27 Nov
/// 2026, "Both a 'hold station' order and in transit"): it flies there, then circles the tile's neighbours for as long as its fuel allows,
/// turning for home in time to make it with the return flight's margin. Vehicle Framework's own circling is not used: it stops after one
/// node unless in recon, and is lost on a load. Instead the ring is plain nodes, kept topped up by the flight's rare look
/// (<see cref="HoldStation.Look"/>), so the path never runs empty and Arrived runs once, on reaching the tile. Saved with the flight's path,
/// as every arrival action is. What it is for: a radio relay from height (the Radio Framework's aircraft relay reads it).
/// </summary>
public class ArrivalAction_PhobosHoldStation : VehicleArrivalAction
{
    PlanetTile centre = PlanetTile.Invalid;
    bool holding;
    MapParent? home;
    IntVec3 homeCell = IntVec3.Invalid;
    Rot4 homeRot = Rot4.North;

    public ArrivalAction_PhobosHoldStation() { }

    public ArrivalAction_PhobosHoldStation(VehiclePawn vehicle, PlanetTile centre, MapParent? home, IntVec3 homeCell, Rot4 homeRot) : base(vehicle)
    {
        this.centre = centre;
        this.home = home;
        this.homeCell = homeCell;
        this.homeRot = homeRot;
    }

    /// <summary>The tile it holds over.</summary>
    public PlanetTile Centre => centre;

    /// <summary>Whether it has reached the tile and is circling it (false on the way there).</summary>
    public bool Holding => holding;

    /// <summary>Where it goes when the hold ends: the colony it was sent from, or the nearest home.</summary>
    public MapParent? Home => home;

    public VehiclePawn Vehicle => vehicle;

    public override bool DestroyOnArrival => false;

    /// <summary>On reaching the tile: it starts circling, the ring put in place before Vehicle Framework looks for the next node.</summary>
    public override void Arrived(GlobalTargetInfo target)
    {
        bool first = !holding;
        holding = true;
        if (AerialVehicle is not { } flight) return;
        HoldStation.TopUp(flight, centre);
        if (!first) return;
        Messages.Message("PhobosHold_MsgHolding".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve(), flight, MessageTypeDefOf.NeutralEvent,
            historical: false);
        if (Seen.Wants("flight.hold-station", vehicle, "holding")) Seen.Note("flight.hold-station", vehicle, "over the tile", "holding");
    }

    /// <summary>Ends the hold: home if the fuel allows, landing where it took off from when that is known; else down where it is.</summary>
    public void EndHold(AerialVehicleInFlight flight)
    {
        holding = false;
        if (home is { HasMap: true } && ReturnFlight.EnoughFuel(vehicle, centre, home.Tile))
        {
            IArrivalAction landing = homeCell.IsValid
                ? new ArrivalAction_LandToCell(vehicle, home, homeCell, homeRot)
                : new ArrivalAction_LandToCaravan(vehicle);
            flight.OrderFlyToTiles(new List<FlightNode> { new(new GlobalTargetInfo(home.Tile)) }, landing);
            return;
        }
        Messages.Message("PhobosAirdrop_MsgNoFuelHome".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve(), MessageTypeDefOf.NegativeEvent);
        flight.SwitchToCaravan();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref centre, "PhobosHold_Centre", PlanetTile.Invalid);
        Scribe_Values.Look(ref holding, "PhobosHold_Holding");
        Scribe_References.Look(ref home, "PhobosHold_Home");
        Scribe_Values.Look(ref homeCell, "PhobosHold_HomeCell", IntVec3.Invalid);
        Scribe_Values.Look(ref homeRot, "PhobosHold_HomeRot", Rot4.North);
    }
}

/// <summary>
/// The hold-station order's service (Framework 0.357.0): ordering it, keeping the ring topped up, and turning for home on fuel. Called from the
/// world-map flight comp's rare look (<see cref="WorldObjectComp_PhobosFlightSystems"/>) and its buttons. Cost: one path count and, while
/// holding, one fuel sum per flight each rare tick; nothing for an aircraft not holding.
/// </summary>
public static class HoldStation
{
    /// <summary>Extra tiles of fuel kept over the way home, so it turns before the margin is gone (a lap's worth; authored).</summary>
    public const float ReserveTiles = 2f;

    /// <summary>Nodes kept ahead of a holding aircraft, a lap's worth, so its path never runs empty between looks (a fast aircraft may pass
    /// several tiles in one).</summary>
    const int KeepAhead = 7;

    static readonly List<PlanetTile> ring = new();

    /// <summary>The hold a flight is on, or null.</summary>
    public static ArrivalAction_PhobosHoldStation? Of(AerialVehicleInFlight flight) =>
        flight.flightPath?.ArrivalAction as ArrivalAction_PhobosHoldStation;

    /// <summary>Whether this aircraft is circling over its tile now (false on the way there, or not on a hold).</summary>
    public static bool Holding(AerialVehicleInFlight flight) => Of(flight) is { Holding: true } && flight.Flying;

    /// <summary>Adds a lap of the tile's neighbours to the path while fewer than a few nodes are left.</summary>
    public static void TopUp(AerialVehicleInFlight flight, PlanetTile centre)
    {
        if (flight.flightPath == null || flight.flightPath.Count >= KeepAhead || !centre.Valid) return;
        ring.Clear();
        Find.WorldGrid.GetTileNeighbors(centre, ring);
        if (ring.Count == 0) ring.Add(centre);
        foreach (PlanetTile tile in ring) flight.flightPath.AddNode(tile);
    }

    /// <summary>Why this aircraft cannot hold over that tile now, or null: the fuel to get there, hold a little and get home.</summary>
    public static string? Refusal(AerialVehicleInFlight flight, VehiclePawn vehicle, PlanetTile tile)
    {
        if (!tile.Valid) return "PhobosHold_NoTile".Translate().Resolve();
        PlanetTile here = WorldHelper.GetNearestTile(flight.DrawPos);
        MapParent? home = NearestHome(tile);
        if (vehicle.CompFueledTravel is not { } fuel || vehicle.CompVehicleLauncher is not { } launcher) return null;
        float there = Find.WorldGrid.ApproxDistanceInTiles(here, tile);
        float back = home != null ? Find.WorldGrid.ApproxDistanceInTiles(tile, home.Tile) : 0f;
        float needed = launcher.FuelNeededToLaunchAtDist(there + back + ReserveTiles) * ReturnFlight.FuelMargin;
        return fuel.Fuel >= needed ? null : "PhobosHold_NoFuel".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve();
    }

    /// <summary>Sends the aircraft to hold over a tile, telling the player what it did or why not.</summary>
    public static void Order(AerialVehicleInFlight flight, VehiclePawn vehicle, PlanetTile tile)
    {
        if (Refusal(flight, vehicle, tile) is { } refused)
        {
            Messages.Message(refused, flight, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        MapParent? home = NearestHome(tile);
        var hold = new ArrivalAction_PhobosHoldStation(vehicle, tile, home, IntVec3.Invalid, Rot4.North);
        flight.OrderFlyToTiles(new List<FlightNode> { new(tile) }, hold);
        Messages.Message("PhobosHold_MsgOrdered".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve(), flight, MessageTypeDefOf.SilentInput,
            historical: false);
        if (Seen.Wants("flight.hold-station", vehicle, "ordered")) Seen.Note("flight.hold-station", vehicle, "ordered", "ordered");
    }

    /// <summary>The player's End hold: home now.</summary>
    public static void End(AerialVehicleInFlight flight)
    {
        if (Of(flight) is not { } hold) return;
        Messages.Message("PhobosHold_MsgEnded".Translate(hold.Vehicle.LabelShort.Named("VEHICLE")).Resolve(), flight, MessageTypeDefOf.SilentInput,
            historical: false);
        hold.EndHold(flight);
    }

    /// <summary>
    /// The flight's rare look: while holding, the ring kept topped up, and the turn for home once the fuel left is what the way home needs with
    /// the return margin and a lap in hand. With no home to go to, it comes down where it is at the same reserve.
    /// </summary>
    public static void Look(AerialVehicleInFlight flight, VehiclePawn vehicle)
    {
        if (Of(flight) is not { Holding: true } hold || !flight.Flying) return;
        TopUp(flight, hold.Centre);
        if (vehicle.CompFueledTravel is not { } fuel || vehicle.CompVehicleLauncher is not { } launcher) return;
        float back = hold.Home is { HasMap: true } home ? Find.WorldGrid.ApproxDistanceInTiles(hold.Centre, home.Tile) : 0f;
        if (fuel.Fuel > launcher.FuelNeededToLaunchAtDist(back + ReserveTiles) * ReturnFlight.FuelMargin) return;
        Messages.Message("PhobosHold_MsgFuel".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve(), flight, MessageTypeDefOf.CautionInput,
            historical: false);
        if (Seen.Wants("flight.hold-station", vehicle, "fuel")) Seen.Note("flight.hold-station", vehicle, "turned for home on fuel", "fuel");
        hold.EndHold(flight);
    }

    /// <summary>The player's home colony nearest a tile, or null.</summary>
    static MapParent? NearestHome(PlanetTile tile) => Find.Maps
        .Where(map => map.IsPlayerHome && map.Parent != null)
        .Select(map => map.Parent)
        .OrderBy(parent => Find.WorldGrid.ApproxDistanceInTiles(tile, parent.Tile))
        .FirstOrDefault();

    // ---- Buttons on the flight (presentation only) ----

    static Texture2D HoldIcon => SystemIcons.For("Phobos/VehicleSystems/Flight/Circle");
    static Texture2D EndIcon => SystemIcons.For("Phobos/VehicleSystems/Flight/RoundStop");

    /// <summary>"Hold station over…" while flying and not on a hold; "End hold" while on one.</summary>
    public static IEnumerable<Gizmo> Gizmos(AerialVehicleInFlight flight, VehiclePawn vehicle)
    {
        if (!flight.Flying) yield break;
        if (Of(flight) != null)
        {
            yield return new Command_Action
            {
                defaultLabel = "PhobosHold_GizmoEnd".Translate().Resolve(),
                defaultDesc = "PhobosHold_GizmoEndDesc".Translate().Resolve(),
                icon = EndIcon,
                action = () => End(flight),
            };
            yield break;
        }
        yield return new Command_Action
        {
            defaultLabel = "PhobosHold_Gizmo".Translate().Resolve(),
            defaultDesc = "PhobosHold_GizmoDesc".Translate().Resolve(),
            icon = HoldIcon,
            action = () => Find.WorldTargeter.BeginTargeting(target =>
            {
                Order(flight, vehicle, target.Tile);
                return true;
            }, canTargetTiles: true, mouseAttachment: HoldIcon),
        };
    }
}
