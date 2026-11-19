namespace Phobos.VehicleSystems;

/// <summary>
/// While the stage lasts the vehicle has no brakes worth the name (Framework 0.188.0; docs/development/vehicle-emergent-play.md, EM29(b)):
/// it still drives, and overruns where it meant to stop (BrakeOverrun). Like every consequence, only the player's vehicles carry it, and
/// the "show only" setting turns it off.
/// </summary>
public class Effect_NoBrakes : VehicleSystemEffect
{
}
