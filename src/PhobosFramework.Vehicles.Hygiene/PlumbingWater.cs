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

/// <summary>A parked vehicle that takes on water joins any plumbing it stands on or against.</summary>
public sealed class PlumbingLink : VehiclePipes.ILink
{
    const string Key = "DubsPlumbing";

    static bool TakesWater(CompPhobosVehicleSystems comp) =>
        comp.Props.systems.Any(system => system.def.service is { waterLitres: > 0f })
        || (comp.Vehicle.GetComp<CompPhobosTank>() is { } tank && tank.Compartments.Any(c => c.contents != null && c.contents.ports.Any(port => port is Port_DubsPlumbing or Port_DubsSeptic)));

    public void Update(CompPhobosVehicleSystems comp)
    {
        var underlay = comp.Underlay(Key);
        if (!comp.ParkedForPipes || !TakesWater(comp))
        {
            underlay.Clear();
            return;
        }
        Map map = comp.Vehicle.Map;
        CellRect footprint = comp.Vehicle.OccupiedRect();
        if (underlay.TouchesRun(footprint, map, cell => DubsPipes.IsPiped(cell, map))) underlay.Fill(footprint, map, cell => DubsPipes.IsPiped(cell, map), DubsPipes.Make);
        else underlay.Clear();
    }

    public void Leave(CompPhobosVehicleSystems comp) => comp.Underlay(Key).Clear();
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
