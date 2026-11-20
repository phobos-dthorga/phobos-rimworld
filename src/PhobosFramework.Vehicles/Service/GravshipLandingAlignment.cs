using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Puts a vehicle carried on the gravship back on its own deck cells after a turned landing (Framework, 20 Nov 2026;
/// docs/development/vehicle-emergent-play.md, "Read: EM18"; the owner: "Please fix the gravship landing thingy"). The game lands each
/// carried thing at its launch offset turned with the ship, by its own footprint rule, which shifts an even-sided footprint one cell when
/// it faces east, south or west (GenAdj.AdjustForRotation). Vehicle Framework draws a vehicle facing south as if north and west as if east
/// (AdjustForVehicleOccupiedRect), so after a turned landing a vehicle with an even length or width covers cells one along from the deck
/// cells it stood on: an end over the edge or in a wall. A postfix that runs after the game has placed everything: for each carried
/// vehicle it works out where the launch footprint lands (the game's rule applied to the footprint Vehicle Framework drew at launch) and,
/// where Vehicle Framework now draws it elsewhere, moves it back by that difference. Temporary (docs/development/temporary-patches.md,
/// row 21): if Vehicle Framework's footprint comes to follow the game's rule, the difference is always nothing and this does nothing.
/// </summary>
[HarmonyPatch(typeof(GravshipPlacementUtility), nameof(GravshipPlacementUtility.PlaceGravshipInMap))]
public static class GravshipLandingAlignment
{
    public static void Postfix(Gravship gravship, IntVec3 root, Map map)
    {
        if (gravship == null || map == null) return;
        Rot4 turn = gravship.Rotation;
        if (turn == Rot4.North) return;    // landed as it left: every footprint matches
        foreach (KeyValuePair<Pawn, PositionData.Data> placed in gravship.PawnPlacements)
        {
            if (placed.Key is not VehiclePawn vehicle || !vehicle.Spawned || vehicle.Map != map) continue;
            IntVec3 shift = Shift(vehicle, root + placed.Value.local, placed.Value.rotation, turn);
            if (shift == IntVec3.Zero) continue;
            VehicleShift.MoveOneCell(vehicle, vehicle.Position + shift);
            if (Seen.Wants("vehicles.gravship-landing-aligned", vehicle))
            {
                Seen.Note("vehicles.gravship-landing-aligned", vehicle, "moved " + shift.x + "," + shift.z + " after a turn to " + turn.ToStringHuman());
            }
        }
    }

    /// <summary>How far the vehicle must move so it covers the cells its launch footprint lands on.</summary>
    static IntVec3 Shift(VehiclePawn vehicle, IntVec3 centre, Rot4 landed, Rot4 turn)
    {
        IntVec2 size = vehicle.VehicleDef.size;
        if (size.x % 2 == 1 && size.z % 2 == 1) return IntVec3.Zero;    // odd sides: every rule agrees
        Rot4 atLaunch = new Rot4((landed.AsInt - turn.AsInt + 4) % 4);
        Rot4 drawnAtLaunch = atLaunch == Rot4.South ? Rot4.North : atLaunch == Rot4.West ? Rot4.East : atLaunch;
        Rot4 imageFacing = new Rot4((drawnAtLaunch.AsInt + turn.AsInt) % 4);
        CellRect intended = GenAdj.OccupiedRect(centre, imageFacing, size);
        CellRect drawn = vehicle.VehicleRect();
        if (intended.Width != drawn.Width || intended.Height != drawn.Height) return IntVec3.Zero;
        return new IntVec3(intended.minX - drawn.minX, 0, intended.minZ - drawn.minZ);
    }
}
