using Phobos.Framework.Persistence;
using RimWorld;
using SmashTools;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// No conduit is built over a parked vehicle's own conduits (Framework 0.116.1; owner, 17 Nov 2026: "refuse it under a vehicle"). The game
/// refuses a conduit over the same kind but lets another kind that shares the "Conduit" replace tag replace it, so an ordinary conduit could
/// be built over the hidden conduits laid beneath a vehicle and take their place, while a hidden one was refused. Our plumbing and chemfuel
/// pipes already refuse (the owner checked). Added by an XML patch to every conduit that uses the game's own conduit rule. A line run to the
/// vehicle's edge still joins it. Under our buildings the game's rule stands (owner, 13 Nov 2026).
/// </summary>
public class PlaceWorker_PhobosVehicleConduit : PlaceWorker
{
    public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing? thingToIgnore = null, Thing? thing = null)
    {
        if (map == null || !loc.InBounds(map)) return true;
        foreach (Thing here in loc.GetThingList(map))
        {
            if (!here.def.EverTransmitsPower || !PipeUnderlay.LaidByUs(here)) continue;
            VehiclePawn? vehicle = map.GetDetachedMapComponent<VehiclePositionManager>()?.ClaimedBy(loc);
            if (vehicle == null) continue;
            Seen.Note("vehicles.conduit-refused-under-vehicle", vehicle);
            return new AcceptanceReport("PhobosVehicleSystems_ConduitUnderVehicle".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve());
        }
        return true;
    }
}
