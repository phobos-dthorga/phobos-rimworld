using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// An aircraft's way home to the spot it took off from, after a job on a tile with no map (Framework 0.345.0, taken out of the airdrop so the
/// medevac shares it): the flight there, landing on the cell and facing it left, when home is another place it has the fuel for; else it
/// comes down as a caravan where it is.
/// </summary>
public static class ReturnFlight
{
    /// <summary>Fuel kept in hand over a leg's need: the airdrop's 15% (authored, Framework 0.288.0).</summary>
    public const float FuelMargin = 1.15f;

    /// <summary>Whether the aircraft has the fuel for <paramref name="legs"/> legs between two tiles, with the margin.</summary>
    public static bool EnoughFuel(VehiclePawn vehicle, PlanetTile from, PlanetTile to, int legs = 1)
    {
        if (vehicle.CompFueledTravel is not { } fuel || vehicle.CompVehicleLauncher is not { } launcher) return true;
        return fuel.Fuel >= launcher.FuelNeededToLaunchAtDist(Find.WorldGrid.ApproxDistanceInTiles(from, to)) * FuelMargin * legs;
    }

    /// <summary>The journey home and how it lands there, when home is another place it has the fuel for; else nothing (it stays).</summary>
    public static (List<FlightNode>?, IArrivalAction?) Home(VehiclePawn vehicle, MapParent? home, IntVec3 cell, Rot4 rot, PlanetTile from)
    {
        if (home is not { HasMap: true } || home.Tile == from || !EnoughFuel(vehicle, from, home.Tile)) return (null, null);
        return (new List<FlightNode> { new(new GlobalTargetInfo(home.Tile)) }, new ArrivalAction_LandToCell(vehicle, home, cell, rot));
    }

    /// <summary>After a job on a tile: on home, or down where it is as a caravan when it lacks the fuel (as the C-17 does).</summary>
    public static void FlyHome(VehiclePawn vehicle, AerialVehicleInFlight? flight, PlanetTile from, MapParent? home, IntVec3 cell, Rot4 rot)
    {
        if (flight == null) return;
        (List<FlightNode>? nodes, IArrivalAction? action) = Home(vehicle, home, cell, rot, from);
        if (nodes != null && action != null)
        {
            flight.OrderFlyToTiles(nodes, action);
            return;
        }
        Messages.Message("PhobosAirdrop_MsgNoFuelHome".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve(), MessageTypeDefOf.NegativeEvent);
        flight.SwitchToCaravan();
    }
}
