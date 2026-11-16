using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A trailer on the tow bar never becomes a world pawn (Framework 0.120.5, fault-read H4). When a caravan forms with both the tractor and
/// its hitched trailer ticked, Vehicle Framework adds the tractor first; the tractor's hitch takes the trailer into its own holder as it
/// leaves the map (TowTravel.TakeAlong), and Vehicle Framework then fails to add the trailer to the caravan but still sends it off the map,
/// whose last step hands it to the world's pawns. Held and listed there both, it was saved twice, and after a load mid-journey the hitch
/// could let go of one copy while the other kept its cargo. The game has no extension point for this.
///
/// A prefix that skips the original, recorded as AGENTS.md asks: it skips only for a pawn whose holder is our hitch, which travels in that
/// holder and must not be anywhere else. Every other pawn passes untouched.
/// </summary>
[HarmonyPatch(typeof(WorldPawns), nameof(WorldPawns.PassToWorld))]
public static class HeldTrailerStaysHeld
{
    public static bool Prefix(Pawn pawn) => pawn?.ParentHolder is not CompPhobosHitch;
}
