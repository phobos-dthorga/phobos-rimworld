using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A convoy whose last colonist dies is not deleted with its vehicles (Framework 0.187.0; docs/development/vehicle-emergent-play.md, EM2:
/// what is lost stays in the world). Vehicle Framework clears and destroys a vehicle caravan once nobody who owns it is left aboard any of
/// its vehicles, vehicles and cargo with it. With the setting on, just before that, the vehicles (with their cargo and anyone else aboard)
/// are taken out to a place on the world map where the convoy stopped, the same kind of place a dropped sling load becomes, and a caravan
/// can come back for them. A prefix that never skips Vehicle Framework's own ending: it only takes the vehicles out first.
/// </summary>
[HarmonyPatch(typeof(VehicleCaravan), nameof(VehicleCaravan.Notify_MemberDied))]
public static class LostConvoyStays
{
    public static void Prefix(VehicleCaravan __instance, Pawn member)
    {
        if (!VehicleSystemsMod.Settings.FlagOn("lostConvoysStay") || __instance.Faction != Faction.OfPlayer || !__instance.Spawned) return;
        // Vehicle Framework's own test for the end: no vehicle left with a living owner aboard besides the one who died.
        bool ownerLeft = __instance.PawnsListForReading.Any(p => p is VehiclePawn v && !v.Dead && v.AllPawnsAboard.Any(o => o != member && __instance.IsOwner(o)));
        if (ownerLeft) return;
        List<VehiclePawn> vehicles = __instance.VehiclesListForReading.Where(v => v != null && !v.Dead && !v.Destroyed).ToList();
        if (vehicles.Count == 0) return;
        PlanetTile tile = __instance.Tile;
        foreach (VehiclePawn vehicle in vehicles)
        {
            __instance.RemovePawn(vehicle);
            if (vehicle.GetComp<CompPhobosVehicleSystems>() is { Usable: true } comp) VehicleStory.Log(comp, VehicleStoryDefOf.PhobosLog_LeftBehind);
        }
        PhobosSlingWreck site = PhobosSlingWreck.Make(tile, vehicles, 0f, PhobosSlingWreckDefOf.PhobosLostConvoy);
        Messages.Message("PhobosVehicleSystems_LostConvoyLeft".Translate(vehicles.Count.Named("COUNT")).Resolve(), site, MessageTypeDefOf.NegativeEvent);
        if (Seen.Wants("vehicles.lost-convoy-left", vehicles[0])) Seen.Note("vehicles.lost-convoy-left", vehicles[0], vehicles.Count.ToString());
    }
}
