using System.Collections.Generic;
using HarmonyLib;
using RimWorld.Planet;
using UnityEngine;
using Vehicles;
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

/// <summary>
/// A hitched trailer is drawn where its tow puts it, not at the middle of its cells (Framework 0.265.1; the owner's report of 23 Nov 2026,
/// a trailer standing about a tile off the L-ATV, half a cell of it from the snap to whole cells). Vehicle Framework's glide aims every
/// vehicle at the middle of its cells, and the one nudge it offers, <c>Transform.position</c>, is drawn twice (its <c>DrawPos</c> adds it and
/// <c>DynamicDrawPhaseAt</c> adds it again), so an offset there would put the picture and everything asking <c>DrawPos</c> apart. Adding the
/// rest of the place to the glide's aim keeps them together, and the glide carries it smoothly when the trailer's cells change. Vehicle
/// Framework has no extension point for it. A postfix: a vehicle not on a hitch gets nought added.
/// </summary>
[HarmonyPatch(typeof(VehicleTweener), "TweenedPosRoot")]
public static class TowedDrawnInPlace
{
    public static void Postfix(VehiclePawn ___vehicle, ref Vector3 __result)
    {
        Vector3 offset = Hitches.DrawOffset(___vehicle);
        if (offset == Vector3.zero) return;
        __result += offset;
        // Noted here rather than where the offset is worked out: it says the glide took it (the patch applied), not only that we had one.
        if (Seen.Wants("vehicles.hitch.drawn-in-place", ___vehicle))
        {
            Seen.Note("vehicles.hitch.drawn-in-place", ___vehicle, offset.x.ToString("0.00") + ", " + offset.z.ToString("0.00"),
                values: new List<KeyValuePair<string, double>> { new("offset", offset.magnitude) });
        }
    }
}
