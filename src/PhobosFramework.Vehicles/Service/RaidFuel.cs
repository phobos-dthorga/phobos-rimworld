using System.Collections.Generic;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Raiders arrive with the journey behind them (Framework 0.192.0; docs/development/vehicle-emergent-play.md, EM5; the owner's choice of
/// 20 Nov 2026, "fuel floor", and their reason to build it: "especially if the chemfuel tank gets a puncture and starts leaking"). When
/// another faction's ground vehicle first appears on a map that is not one of its own faction's settlements, its tank is set once to a
/// full tank less the fuel for the trip from that faction's nearest settlement, never below a fifth (ArrivalFuelRule), in place of
/// Vehicle Framework's random 45-85%. A raid burns little fuel on the map, so this shows when a tank is holed: Vehicle Framework drains a
/// leaking tank at 1 to 10 units a second, and a raid from far away runs dry sooner, leaves less fuel on the ground and is sooner
/// stranded (its crew then blow it up: Vehicle Framework's sabotage order). Aircraft, a faction's vehicles at its own settlements, the
/// player's and a vehicle with figures only switched on are left as Vehicle Framework makes them.
/// </summary>
public static class RaidFuel
{
    /// <summary>Called once, the first time the vehicle spawns (not after a load).</summary>
    public static void SetOnArrival(CompPhobosVehicleSystems comp)
    {
        VehiclePawn vehicle = comp.Vehicle;
        if (comp.ArrivalFuelSet) return;
        comp.ArrivalFuelSet = true;
        Faction? faction = vehicle.Faction;
        CompFueledTravel? fuel = vehicle.CompFueledTravel;
        Map? map = vehicle.Map;
        if (faction == null || faction == Faction.OfPlayer || fuel == null || map == null || VehicleSystemsMod.Settings.showOnly) return;
        if (vehicle.VehicleDef.type != VehicleType.Land || vehicle.CompVehicleLauncher != null || fuel.FuelCapacity <= 0f) return;
        if (map.Parent is Settlement home && home.Faction == faction) return;
        if (!NearestSettlement(faction, map.Tile, out float tiles)) return;
        double speed = vehicle.GetStatValue(VehicleStatDefOf.MoveSpeed) * vehicle.WorldSpeedMultiplier;
        double rate = fuel.ConsumptionRatePerTickRaw / ArrivalFuelRule.FuelPerTickPerRate;
        double trip = ArrivalFuelRule.TripFuel(tiles, rate, speed);
        float target = (float)ArrivalFuelRule.Arrival(fuel.FuelCapacity, trip);
        float now = fuel.Fuel;
        if (target > now) fuel.Refuel(target - now);
        else if (target < now) fuel.ConsumeFuel(now - target);
        if (Seen.Wants("vehicles.raid-fuel", vehicle))
        {
            Seen.Note("vehicles.raid-fuel", vehicle, tiles.ToString("0") + " tiles, " + (target / fuel.FuelCapacity).ToStringPercent(), null,
                new[] { new KeyValuePair<string, double>("tiles", tiles), new KeyValuePair<string, double>("share", target / fuel.FuelCapacity) });
        }
    }

    /// <summary>How far the faction's nearest settlement is from this tile, on the same layer of the world.</summary>
    static bool NearestSettlement(Faction faction, PlanetTile tile, out float tiles)
    {
        tiles = float.MaxValue;
        if (!tile.Valid || Find.WorldGrid == null) return false;
        foreach (Settlement settlement in Find.WorldObjects.Settlements)
        {
            if (settlement.Faction != faction || !settlement.Tile.Valid || settlement.Tile.Layer != tile.Layer) continue;
            float distance = Find.WorldGrid.ApproxDistanceInTiles(tile, settlement.Tile);
            if (distance < tiles) tiles = distance;
        }
        return tiles < float.MaxValue;
    }
}
