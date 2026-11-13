using System.Collections.Generic;
using System.Linq;
using DubsBadHygiene;
using Phobos.Framework.Hygiene;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems.Hygiene;

/// <summary>
/// Vehicles take their water from Dubs Bad Hygiene's plumbing under or beside them, through the Framework's one plumbing reader
/// (PlumbingFeed), so a coolant top-up or an injection-water refill needs a pipe with a water tower behind it. In Dubs' lite mode
/// there is no plumbing, so water stays free. Registered once at startup.
/// </summary>
[StaticConstructorOnStartup]
public static class PlumbingWaterRegistration
{
    static PlumbingWaterRegistration()
    {
        if (PlumbingFeed.LiteMode) return;
        VehicleWater.Register(new PlumbingWater(), "Dubs Bad Hygiene plumbing under or beside the vehicle");
        VehiclePipes.Register(new PlumbingLink());
    }
}

/// <summary>
/// A vehicle that takes on water lays Dubs' visible plumbing across its footprint while it stands with its engine off (Framework 0.108.0; owner, 13 Nov 2026:
/// always, "so players are not confused", not only where a run touches it), so a pipe touching any side joins it. Taken up when the engine starts or it moves.
/// </summary>
public sealed class PlumbingLink : VehiclePipes.ILink
{
    const string Key = "DubsPlumbing";

    static bool TakesWater(VehiclePipes.IHost comp) =>
        VehiclePipes.SystemsTakeWater(comp)
        || (VehiclePipes.Tank(comp) is { } tank && tank.Compartments.Any(c => c.contents != null && c.contents.ports.Any(port => port is Port_DubsPlumbing or Port_DubsSeptic)));

    public void Update(VehiclePipes.IHost comp)
    {
        var underlay = comp.Underlay(Key);
        int before = underlay.Changes;
        if (!comp.ParkedForPipes || !TakesWater(comp))
        {
            Leave(comp);
            return;
        }
        Map map = comp.Vehicle.Map;
        CellRect footprint = comp.Vehicle.OccupiedRect();
        underlay.Renew(DubsPipes.Def, DubsPipes.Make);
        // Plumbing stands on any ground a vehicle can park on but water: the game will not build a pipe in water.
        underlay.Fill(footprint, map, cell => DubsPipes.IsPiped(cell, map), DubsPipes.Make, cell => !cell.GetTerrain(map).IsWater);
        if (underlay.Changes != before) DubsPipes.Redraw(map, footprint);
    }

    public void Leave(VehiclePipes.IHost comp)
    {
        var underlay = comp.Underlay(Key);
        int before = underlay.Changes;
        // Read the map and footprint before the pieces go: a vehicle leaving the map may already be off it.
        Map? map = comp.Vehicle.Spawned ? comp.Vehicle.Map : null;
        CellRect footprint = comp.Vehicle.Spawned ? comp.Vehicle.OccupiedRect() : CellRect.Empty;
        underlay.Clear();
        if (map != null && underlay.Changes != before) DubsPipes.Redraw(map, footprint);
    }

    public void AfterLoad(VehiclePipes.IHost comp)
    {
        if (!comp.Vehicle.Spawned) return;
        var underlay = comp.Underlay(Key);
        int before = underlay.Changes;
        underlay.AdoptOnce(comp.Vehicle.OccupiedRect(), comp.Vehicle.Map, DubsPipes.IsOldHidden);
        underlay.Renew(DubsPipes.Def, DubsPipes.Make);
        if (underlay.Changes != before) DubsPipes.Redraw(comp.Vehicle.Map, comp.Vehicle.OccupiedRect());
    }
}

public sealed class PlumbingWater : VehicleWater.ISource
{
    readonly Dictionary<VehiclePawn, PlumbingFeed> feeds = new();

    PlumbingFeed Feed(VehiclePawn vehicle)
    {
        if (!feeds.TryGetValue(vehicle, out PlumbingFeed? feed))
        {
            feed = new PlumbingFeed();
            feeds[vehicle] = feed;
        }
        return feed;
    }

    public bool CanDraw(VehiclePawn vehicle, float litres, out string? reason)
    {
        reason = null;
        if (!vehicle.Spawned)
        {
            reason = "PhobosVehicleSystems_WaterNoPlumbing".Translate();
            return false;
        }
        switch (Feed(vehicle).Check(vehicle, litres))
        {
            case WaterDraw.Flowing:
                return true;
            case WaterDraw.TowersEmpty:
                reason = "PhobosVehicleSystems_WaterTowersEmpty".Translate();
                return false;
            default:
                reason = "PhobosVehicleSystems_WaterNoPlumbing".Translate();
                return false;
        }
    }

    public bool TryDraw(VehiclePawn vehicle, float litres) =>
        vehicle.Spawned && Feed(vehicle).TryPull(vehicle, litres, out ContaminationLevel _) == WaterDraw.Flowing;
}
