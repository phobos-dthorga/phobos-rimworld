using Vehicles;

namespace Phobos.VehicleSystems;

/// <summary>
/// Keeps a crew aboard while one of our stations is engaged (owner, 16 Nov 2026; docs/development/dependency-audit-2026-11-16.md).
/// Vehicle Framework 1.7 sends everyone out of a vehicle that has stood still for five in-game hours (VehiclePawn.TickInterval,
/// HoursIdleToDismount), and resets that count itself while a turret aims, fires or reloads. Our stations do the same, through its
/// public ResetIdleTicks, while they are engaged: a hull under water, a litter with patients and a doctor, a sonar with something
/// hostile in reach, a listening array that hears something. When all is quiet, Vehicle Framework's rule stands. Its own means, no
/// Harmony; it works on any faction's vehicle.
/// </summary>
public static class CrewOnStation
{
    public static void Engaged(VehiclePawn vehicle)
    {
        if (vehicle.Spawned) vehicle.ResetIdleTicks();
    }
}
