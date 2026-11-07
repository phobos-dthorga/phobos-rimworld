using HarmonyLib;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehiclePatches;

/// <summary>
/// Keeps a map open while a player vehicle with a colonist aboard is spawned on it, in the air or on the ground.
///
/// The game removes a site's map once nothing blocks removal (MapPawns.AnyPawnBlockingMapRemoval). That check can't see crew
/// inside a Vehicle Framework vehicle: VehiclePawn keeps them in its role handlers, which the game's held-thing scan doesn't
/// reach. Vehicle Framework fills the gap for vehicles that claim ground cells, and the Harrier's flight framework for aircraft
/// in the air, but the framework patches only the game's own map parent types: its scan is of Assembly-CSharp alone. A sortie
/// over a Real Ruins site, a RimMarket convoy battle or any other mod's map parent with its own ShouldRemoveMapNow could close
/// the map under the jet, pilot and all. Read in the 1.7 game, Vehicle Framework's 1.6 build and framework build 19o, 3 Nov 2026.
///
/// A postfix on the game's own check is the one place every such rule reads, so it covers them all without naming any mod, and
/// the VTOL Vehicle framework's helicopters with them.
/// </summary>
public static class AirborneCrewKeepsMap
{
    public static void Apply(Harmony harmony)
    {
        var check = AccessTools.PropertyGetter(typeof(MapPawns), nameof(MapPawns.AnyPawnBlockingMapRemoval));
        harmony.Patch(check, postfix: new HarmonyMethod(AccessTools.Method(typeof(AirborneCrewKeepsMap), nameof(Postfix))));
    }

    /// <summary>Adds one rule to the game's: a spawned player vehicle with a free colonist aboard blocks removal.</summary>
    public static void Postfix(ref bool __result, Map ___map)
    {
        if (__result || ___map?.mapPawns == null) return;
        foreach (Pawn pawn in ___map.mapPawns.AllPawnsSpawned)
        {
            if (pawn is not VehiclePawn vehicle || vehicle.Destroyed || vehicle.Faction != Faction.OfPlayer) continue;
            foreach (Pawn aboard in vehicle.AllPawnsAboard)
            {
                if (aboard.IsFreeColonist)
                {
                    __result = true;
                    return;
                }
            }
        }
    }
}
