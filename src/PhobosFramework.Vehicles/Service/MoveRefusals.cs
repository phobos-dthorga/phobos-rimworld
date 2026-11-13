using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Why the player's vehicle will not move on its own map now, from every refusal of ours: a system stage (worn brakes, a cold boiler), a
/// set-up job holding it, a boat resting on the bottom. The one place this is asked for anything that sends a vehicle somewhere without
/// the player's float menu (Vehicles at Work's trips, the depot bay's "send to bay"), since only the float menu asks the comps' CanMove.
/// Reads cached state only; never recomputes a system.
/// </summary>
public static class MoveRefusals
{
    public static string? For(VehiclePawn vehicle)
    {
        if (vehicle.GetComp<CompPhobosVehicleSystems>() is { Usable: true, MoveRefusal: { } systems }) return systems;
        foreach (ThingComp comp in vehicle.AllComps)
        {
            if (comp is CompPhobosSetUp { HoldRefusal: { } held }) return held;
            if (comp is CompPhobosSubmersible { SettledRefusal: { } settled }) return settled;
        }
        return null;
    }
}
