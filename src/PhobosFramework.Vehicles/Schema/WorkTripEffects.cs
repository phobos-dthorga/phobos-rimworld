using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// While the stage lasts, colonists do not take the vehicle out by themselves: a stage in which driving it would wreck it (an engine boiling
/// over, a cooling system nearly empty, no oil pressure). The player's own orders still go through, and so do the errands the player sets
/// (Deliver, Gather); only work done on a colonist's own initiative leaves it alone, which today means Vehicles at Work's trips and hauls
/// (Framework 0.183.0; docs/development/vehicles-at-work-tie-ins.md, VW8; owner, 20 Nov 2026: "implement both"). The reason is a translation
/// key with {VEHICLE}, read through <c>CompPhobosVehicleSystems.WorkTripRefusal</c>. Like every refusal, only the player's vehicles
/// carry it, and the "show only" setting turns it off.
/// </summary>
public class Effect_KeepFromWorkTrips : VehicleSystemEffect
{
    public string reason = "";

    public string WorkTripRefusal(SystemContext context) => reason.Translate(context.Vehicle.LabelShort.Named("VEHICLE")).Resolve();
}
