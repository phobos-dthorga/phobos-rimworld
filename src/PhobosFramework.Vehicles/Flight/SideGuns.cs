using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Marks a turret as a side-firing gun (Framework 0.329.0; the owner's card of 26 Nov 2026 for the C-27J's MC-27J gun kit: "Circle left, gun
/// on the centre"). An aircraft with one fitted circles toward the gun's side, as a real gunship flies its pylon turn, so the gun faces the
/// circle's centre and fires on what is inside it; its arc is the turret's own (Vehicle Framework's angleRestricted). Another author may put
/// it on a turret of theirs in XML.
/// </summary>
public class PhobosSideGunExtension : DefModExtension
{
    /// <summary>The gun fires out of the left side (the default; the MC-27J's left jump door), else the right.</summary>
    public bool left = true;
}

/// <summary>Which way an aircraft's fitted side gun makes it circle.</summary>
public static class SideGuns
{
    /// <summary>
    /// Whether the aircraft must circle clockwise (seen from above: turning right) for its side gun to face the centre: false for a gun on the
    /// left, true for one on the right; null with no side gun fitted, when the player's own choice stands. A list of one to three turrets,
    /// read when it circles.
    /// </summary>
    public static bool? CirclesClockwise(VehiclePawn vehicle)
    {
        if (vehicle.CompVehicleTurrets?.Turrets is not { Count: > 0 } turrets) return null;
        foreach (VehicleTurret turret in turrets)
        {
            if (turret.def?.GetModExtension<PhobosSideGunExtension>() is { } side) return !side.left;
        }
        return null;
    }
}
