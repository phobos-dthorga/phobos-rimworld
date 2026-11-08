using System.Collections.Generic;
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
        VehicleWater.Register(new PlumbingWater(), "Dubs Bad Hygiene plumbing beside the vehicle");
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
