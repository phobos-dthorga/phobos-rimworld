using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// What time does to a depot (Framework 0.200.0; docs/development/vehicle-depots.md, batch 2). Once a game day, staggered by depot, each
/// pressure the player has switched on runs, each with a cause they can see coming and no roll (DepotPressureRule):
/// DP7 hostile neighbours: the nearest hostile settlement within six tiles finds the depot after days set by its distance, said on the
/// depot's line from the first day, and strips it of its cargo and fuel; its vehicles are left, emptied. With RimPacts, a war that hands a
/// nearby settlement to an enemy makes it a hostile neighbour, so DP10 comes through here (RimPacts offers no call that says who holds a
/// tile). DP8 weather: in frost or a wet climate its vehicles' outer parts wear and its loose fuel cans leak, a little a day. DP9 fuel
/// ageing: fuel kept past three months starts to go stale. The owner's rule on settings by risk (20 Nov 2026): the slow pressures start on,
/// the one that takes a whole depot starts off.
/// </summary>
public static class DepotPressures
{
    static VehicleSystemsSettings Settings => VehicleSystemsMod.Settings;

    /// <summary>The daily look, from the depot's own tick.</summary>
    public static void Daily(PhobosSlingWreck depot)
    {
        if (depot.Destroyed || depot.HasMap) return;
        if (Settings.depotHostileNeighbours) Neighbours(depot);
        else depot.foundAtTick = -1;
        if (Settings.depotWeather) Weather(depot);
        if (Settings.depotFuelAgeing) Ageing(depot);
    }

    /// <summary>The nearest settlement hostile to the player within finding distance, and its distance.</summary>
    static Settlement? HostileNeighbour(PlanetTile tile, out float tiles)
    {
        tiles = float.MaxValue;
        Settlement? nearest = null;
        foreach (Settlement settlement in Find.WorldObjects.Settlements)
        {
            if (settlement.Faction == null || !settlement.Faction.HostileTo(Faction.OfPlayer) || settlement.Tile.Layer != tile.Layer) continue;
            float distance = Find.WorldGrid.ApproxDistanceInTiles(tile, settlement.Tile);
            if (distance > DepotPressureRule.FoundWithinTiles || distance >= tiles) continue;
            tiles = distance;
            nearest = settlement;
        }
        return nearest;
    }

    static void Neighbours(PhobosSlingWreck depot)
    {
        Settlement? neighbour = HostileNeighbour(depot.Tile, out float tiles);
        if (neighbour == null || DepotPressureRule.DaysToBeFound(tiles) is not { } days)
        {
            depot.foundAtTick = -1;
            depot.foundBy = null;
            return;
        }
        int now = Find.TickManager.TicksGame;
        if (depot.foundAtTick < 0 || depot.foundBy != neighbour.Faction)
        {
            depot.foundAtTick = now + (int)(days * GenDate.TicksPerDay);
            depot.foundBy = neighbour.Faction;
            return;
        }
        if (now < depot.foundAtTick || !Holds(depot)) return;
        Strip(depot);
        Find.LetterStack.ReceiveLetter("PhobosDepot_StrippedLabel".Translate(), "PhobosDepot_Stripped".Translate(depot.LabelCap.Named("DEPOT"),
            neighbour.Faction.Name.Named("FACTION")), LetterDefOf.NegativeEvent, depot);
        if (Seen.Wants("vehicles.depot-pressure", null, "stripped")) Seen.Note("vehicles.depot-pressure", null, depot.Label, "stripped");
        depot.foundAtTick = -1;
    }

    /// <summary>Whether there is anything to strip: loose cargo, cargo in its vehicles, or fuel in their tanks.</summary>
    static bool Holds(PhobosSlingWreck depot) =>
        depot.Items.Any() || depot.Vehicles.Any(v => v.inventory?.innerContainer.Any(t => t is not Pawn) == true
                                                    || v.GetComp<CompPhobosTank>()?.Compartments.Any(c => c.HasLoad) == true);

    /// <summary>Takes everything but the vehicles themselves: loose cargo, their cargo, their tanks' loads. Nobody is aboard a depot's vehicles.</summary>
    static void Strip(PhobosSlingWreck depot)
    {
        foreach (Thing item in depot.Items.ToList()) item.Destroy(DestroyMode.Vanish);
        foreach (VehiclePawn vehicle in depot.Vehicles)
        {
            if (vehicle.inventory?.innerContainer is { } hold)
            {
                foreach (Thing thing in hold.ToList()) if (thing is not Pawn) thing.Destroy(DestroyMode.Vanish);
            }
            if (vehicle.GetComp<CompPhobosTank>() is { } tank) foreach (TankCompartment compartment in tank.Compartments) if (compartment.HasLoad) compartment.Clear();
        }
    }

    static void Weather(PhobosSlingWreck depot)
    {
        int now = GenTicks.TicksAbs;
        float season = GenTemperature.GetTemperatureFromSeasonAtTile(now, depot.Tile);
        float rain = Find.WorldGrid[depot.Tile]?.rainfall ?? 0f;
        if (!DepotPressureRule.Weathers(season, rain)) return;
        foreach (VehiclePawn vehicle in depot.Vehicles)
        {
            if (vehicle.statHandler == null) continue;
            foreach (VehicleComponent part in vehicle.statHandler.components)
            {
                if (part.props.depth != VehicleComponent.VehiclePartDepth.External || part.Health <= 1f) continue;
                part.SetHealth(UnityEngine.Mathf.Max(1f, part.Health - (float)(DepotPressureRule.WearSharePerDay * part.MaxHealth)));
            }
            vehicle.GetComp<CompPhobosVehicleSystems>()?.RememberAllPartHealths();
        }
        // Loose cans only: what is inside a vehicle is under cover.
        int cans = depot.Items.Where(t => Depots.IsFuel(t.def)).Sum(t => t.stackCount);
        (int leaked, double carry) = DepotPressureRule.WholeUnits(cans, DepotPressureRule.CanLeakSharePerDay, depot.leakCarry);
        depot.leakCarry = carry;
        if (leaked > 0) TakeLoose(depot, leaked);
        if (Seen.Wants("vehicles.depot-pressure", null, "weathered")) Seen.Note("vehicles.depot-pressure", null, depot.Label + ", " + leaked + " cans", "weathered");
    }

    static void TakeLoose(PhobosSlingWreck depot, int count)
    {
        foreach (Thing thing in depot.Items.Where(t => Depots.IsFuel(t.def)).ToList())
        {
            if (count <= 0) break;
            int take = System.Math.Min(count, thing.stackCount);
            thing.SplitOff(take).Destroy(DestroyMode.Vanish);
            count -= take;
        }
    }

    static void Ageing(PhobosSlingWreck depot)
    {
        if (depot.fuelClock < 0) return;
        double ageDays = (Find.TickManager.TicksGame - depot.fuelClock) / GenDate.TicksPerDay;
        double share = DepotPressureRule.StaleShare(ageDays);
        if (share <= 0) return;
        int stale = 0;
        foreach (ThingDef fuel in Depots.FuelKinds(depot).ToList())
        {
            depot.staleCarry.TryGetValue(fuel.defName, out double carry);
            (int units, double left) = DepotPressureRule.WholeUnits(Depots.FuelUnits(depot, fuel), share, carry);
            depot.staleCarry[fuel.defName] = left;
            if (units > 0) stale += Depots.Draw(depot, fuel, units);
        }
        if (stale > 0 && Seen.Wants("vehicles.depot-pressure", null, "stale")) Seen.Note("vehicles.depot-pressure", null, depot.Label + ", " + stale + " units", "stale");
    }

    /// <summary>The depot's lines about what time is doing to it, for its world-map line: when it will be found, and how fresh its fuel is.</summary>
    public static IEnumerable<string> Lines(PhobosSlingWreck depot)
    {
        if (Settings.depotHostileNeighbours && depot.foundAtTick >= 0 && depot.foundBy != null)
        {
            int left = System.Math.Max(0, depot.foundAtTick - Find.TickManager.TicksGame);
            yield return "PhobosDepot_FoundIn".Translate(depot.foundBy.Name.Named("FACTION"), left.ToStringTicksToPeriod().Named("TIME")).Resolve();
        }
        if (Settings.depotFuelAgeing && depot.fuelClock >= 0 && Depots.FuelKinds(depot).Any())
        {
            int age = (int)((Find.TickManager.TicksGame - depot.fuelClock) / GenDate.TicksPerDay);
            yield return (age > DepotPressureRule.FreshDays ? "PhobosDepot_FuelStale" : "PhobosDepot_FuelAge").Translate(age.Named("DAYS"),
                ((int)DepotPressureRule.FreshDays).Named("FRESH")).Resolve();
        }
    }
}
