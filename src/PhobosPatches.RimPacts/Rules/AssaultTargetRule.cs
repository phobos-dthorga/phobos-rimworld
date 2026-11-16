namespace Phobos.RimPactsPatches.Rules;

/// <summary>
/// Whether a thing is still worth an assault's attention. Game-free. RimPacts counts a target as live while it stands on the map and,
/// for a pawn, is neither dead nor downed; a vehicle is a pawn that never goes down, so one with nobody aboard stayed live for the
/// whole battle. RimPacts' own count of fighters already leaves an unmanned vehicle out (its IsLiveFighter); this makes the assault's
/// targets agree with it.
/// </summary>
public static class AssaultTargetRule
{
    public static bool StillLive(bool liveByRimPacts, bool unmannedVehicle) => liveByRimPacts && !unmannedVehicle;
}
