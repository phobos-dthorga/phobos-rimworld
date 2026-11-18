using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Phobos.Framework.Rules;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using SmashTools.Targeting;
using UnityEngine;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

// Leaving and arriving in flight (Framework 0.149.0; docs/development/vehicle-flight-over-the-map.md, "Leaving the map and arriving", the
// design's slice 3). Every patch here acts only for a vehicle carrying our flight.

/// <summary>Where each aircraft's present leg between places began, so an arrival comes in over the map's edge on that side. Derived, never
/// saved: Vehicle Framework asks for the flight speed as each leg begins, after a load too.</summary>
public static class FlightLegs
{
    static readonly ConditionalWeakTable<VehiclePawn, StrongBox<PlanetTile>> from = new();

    public static void Note(VehiclePawn vehicle, PlanetTile tile) => from.GetOrCreateValue(vehicle).Value = tile;

    /// <summary>The place its present leg began, or an invalid tile when none was seen.</summary>
    public static PlanetTile From(VehiclePawn vehicle) => from.TryGetValue(vehicle, out StrongBox<PlanetTile>? box) ? box.Value : PlanetTile.Invalid;
}

/// <summary>What an aircraft on our flight does as it comes over a map from another place.</summary>
public enum ArrivalIntent
{
    /// <summary>Flies to the spot chosen in the destination picker and lands there.</summary>
    Land,
    /// <summary>Flies to the middle of the map and holds there.</summary>
    Centre,
    /// <summary>Holds just inside the edge it came over.</summary>
    Edge,
    /// <summary>Holds just inside the edge, and the player picks where to land.</summary>
    Targeted,
}

/// <summary>
/// Puts an aircraft on our flight onto a map as it arrives from another place: over the edge on the side it flew in from, already flying,
/// with the order its arrival asked for (Framework 0.149.0). It never lands as Vehicle Framework's own stand-in does.
/// </summary>
public static class FlightArrival
{
    /// <summary>How far in from the edge one told to hold there stops, cells (authored: clear of the edge, still near it).</summary>
    const int HoldInsideCells = 12;

    /// <summary>How far either way along the edge it looks for a clear place to come in (authored).</summary>
    const int SearchAlongEdge = 40;

    public static bool Flies(VehiclePawn? vehicle) => CompPhobosFlight.FliesOnOurs(vehicle);

    static readonly AccessTools.FieldRef<ArrivalAction_LandToCell, IntVec3> LandingCell = AccessTools.FieldRefAccess<ArrivalAction_LandToCell, IntVec3>("landingCell");
    static readonly AccessTools.FieldRef<ArrivalAction_LandToCell, Rot4> LandingRot = AccessTools.FieldRefAccess<ArrivalAction_LandToCell, Rot4>("landingRot");
    static readonly AccessTools.FieldRef<ArrivalAction_LandToCell, MapParent> LandingMapParent = AccessTools.FieldRefAccess<ArrivalAction_LandToCell, MapParent>("mapParent");

    /// <summary>Whether a journey is only to a landing spot on the map the vehicle is on now (fault-read W4), and that spot.</summary>
    public static bool SameMapSpot(VehiclePawn vehicle, List<FlightNode> nodes, IArrivalAction action, out IntVec3 cell, out Rot4 rot)
    {
        cell = IntVec3.Invalid;
        rot = Rot4.North;
        if (action is not ArrivalAction_LandToCell land || action is ArrivalAction_CrashInMap || vehicle.Map is not { } map) return false;
        if (nodes.Count == 0 || nodes[nodes.Count - 1].Tile != map.Tile || LandingMapParent(land)?.Map != map) return false;
        cell = LandingCell(land);
        rot = LandingRot(land);
        return cell.IsValid;
    }

    /// <summary>Spawns the vehicle on the map's edge, in the air, and gives it its first order. False when it could not, so Vehicle
    /// Framework's own landing goes ahead instead.</summary>
    public static bool Enter(VehiclePawn vehicle, Map map, ArrivalIntent intent, IntVec3 landCell, Rot4 landRot)
    {
        if (vehicle.GetComp<CompPhobosFlight>() is not { } flight || map == null) return false;
        PlanetTile cameFrom = FlightLegs.From(vehicle);
        double bearing = cameFrom.Valid && cameFrom != map.Tile ? Find.WorldGrid.GetHeadingFromTo(map.Tile, cameFrom) : Rand.Range(0f, 360f);
        if (!EnterAt(vehicle, map, bearing, out float px, out float pz, out double inward)) return false;
        if (Seen.On)
        {
            string variant = intent.ToString();
            if (Seen.Wants("aircraft.flight-arrived", vehicle, variant)) Seen.Note("aircraft.flight-arrived", vehicle, variant: variant);
        }
        Order(vehicle, flight, map, intent, landCell, landRot, px, pz, inward);
        return true;
    }

    /// <summary>Spawns the vehicle in the air on the edge on this bearing from the map's middle, heading inward, with no order yet
    /// (Framework 0.157.0: shared by arrivals and raids). False when it could not.</summary>
    public static bool EnterAt(VehiclePawn vehicle, Map map, double bearing) => EnterAt(vehicle, map, bearing, out _, out _, out _);

    static bool EnterAt(VehiclePawn vehicle, Map map, double bearing, out float px, out float pz, out double inward)
    {
        px = pz = 0f;
        inward = FlightPathRule.Normalise(bearing + 180);
        if (vehicle.GetComp<CompPhobosFlight>() is not { } flight || map == null) return false;
        IntVec2 size = vehicle.VehicleDef.Size;
        float edge = Mathf.Max(size.x, size.z) / 2f + 1f;
        Vector3 centre = map.Center.ToVector3Shifted();
        (double ex, double ez) = FlightPathRule.EdgePoint(centre.x, centre.z, bearing, edge, map.Size.x - edge, edge, map.Size.z - edge);
        Rot4 rot = FlightRules.Rot4For(inward);
        (px, pz) = ClearPlace(vehicle, map, (float)ex, (float)ez, rot, edge);
        IntVec3 cell = FlightRules.CellFor(vehicle, px, pz, new Rot8(rot));
        if (!cell.InBounds(map)) return false;
        GenSpawn.Spawn(vehicle, cell, map, rot, WipeMode.Vanish);
        if (!vehicle.Spawned) return false;
        flight.EnterFlying(px, pz, (float)inward);
        return true;
    }

    static void Order(VehiclePawn vehicle, CompPhobosFlight flight, Map map, ArrivalIntent intent, IntVec3 landCell, Rot4 landRot, float px, float pz,
        double inward)
    {
        double rad = inward * Math.PI / 180;
        IntVec3 inside = new IntVec3(Mathf.RoundToInt(px + (float)Math.Sin(rad) * HoldInsideCells), 0, Mathf.RoundToInt(pz + (float)Math.Cos(rad) * HoldInsideCells))
            .ClampInsideMap(map);
        switch (intent)
        {
            case ArrivalIntent.Land when flight.LandRefusal(landCell, landRot) is { } refused:
                flight.FlyTo(inside);
                if (vehicle.Faction == Faction.OfPlayer)
                {
                    Messages.Message("PhobosFlight_MsgArrivedSpotRefused".Translate(vehicle.LabelShort.Named("VEHICLE"), refused.Named("WHY")).Resolve(), vehicle,
                        MessageTypeDefOf.CautionInput);
                }
                break;
            case ArrivalIntent.Land:
                flight.Land(landCell, landRot);
                break;
            case ArrivalIntent.Centre:
                flight.FlyTo(map.Center);
                break;
            case ArrivalIntent.Targeted:
                flight.FlyTo(inside);
                if (vehicle.Faction == Faction.OfPlayer)
                {
                    LandingTargeter.Instance.BeginTargetingAndFocusMap(vehicle, map, (target, rot) => flight.Land(target.Cell, rot),
                        target => flight.LandRefusal(target.Cell, vehicle.Rotation) == null, allowRotating: vehicle.VehicleDef.rotatable);
                }
                break;
            default:
                flight.FlyTo(inside);
                break;
        }
    }

    /// <summary>
    /// A place on the edge near the one wanted where spawning would destroy nothing: a vehicle spawned over plants or buildings would wipe
    /// them, and an aircraft coming in overhead must not. Looks along the edge either way; the wanted place when none is clear.
    /// </summary>
    static (float X, float Z) ClearPlace(VehiclePawn vehicle, Map map, float x, float z, Rot4 rot, float edge)
    {
        bool alongX = Mathf.Abs(z - edge) < 0.5f || Mathf.Abs(z - (map.Size.z - edge)) < 0.5f;
        // Fault-read W12 (Framework 0.167.0): the nearest place along the whole edge where nothing would be wiped, else the one wiping least.
        int reach = Mathf.Max(SearchAlongEdge, Mathf.Max(map.Size.x, map.Size.z));
        (float X, float Z) least = (x, z);
        int leastWiped = int.MaxValue;
        for (int step = 0; step <= reach; step++)
        {
            foreach (int sign in step == 0 ? new[] { 1 } : new[] { 1, -1 })
            {
                float px = alongX ? Mathf.Clamp(x + sign * step, edge, map.Size.x - edge) : x;
                float pz = alongX ? z : Mathf.Clamp(z + sign * step, edge, map.Size.z - edge);
                int wiped = Wiped(vehicle, map, FlightRules.CellFor(vehicle, px, pz, new Rot8(rot)), rot);
                if (wiped == 0) return (px, pz);
                if (wiped < leastWiped)
                {
                    leastWiped = wiped;
                    least = (px, pz);
                }
            }
        }
        return least;
    }

    /// <summary>How many things spawning it here would wipe (filth aside); a cell off the map counts as a great many.</summary>
    static int Wiped(VehiclePawn vehicle, Map map, IntVec3 cell, Rot4 rot)
    {
        int wiped = 0;
        foreach (IntVec3 c in vehicle.VehicleRect(cell, rot))
        {
            if (!c.InBounds(map)) return 100000;
            foreach (Thing thing in c.GetThingList(map))
            {
                if (thing.def.category != ThingCategory.Filth && GenSpawn.SpawningWipes(vehicle.def, thing.def)) wiped++;
            }
        }
        return wiped;
    }
}

/// <summary>Notes where each leg between places begins: Vehicle Framework reads the flight speed as each leg starts (SetSpeed), with the
/// flight's tile the place it is leaving. A postfix that changes nothing.</summary>
[HarmonyPatch(typeof(CompVehicleLauncher), nameof(CompVehicleLauncher.FlightSpeed), MethodType.Getter)]
public static class Patch_FlightSpeed_FlightLegs
{
    public static void Postfix(CompVehicleLauncher __instance)
    {
        if (__instance.Vehicle is { ParentHolder: AerialVehicleInFlight flight } vehicle) FlightLegs.Note(vehicle, flight.Tile);
    }
}

/// <summary>
/// Vehicle Framework's launch, for an aircraft on our flight: it keeps the destination and arrival the picker chose and skips Vehicle
/// Framework's lift-off animation; our flight takes off (if on the ground), flies to the map's edge on the side of the journey, and hands
/// over there. A prefix that skips the original, and must: Vehicle Framework offers no launch without its animation. It runs last, after the
/// other flight frameworks' prefixes and our air-defence prompt, and does nothing when one of them has already held the launch back.
/// </summary>
[HarmonyPatch(typeof(CompVehicleLauncher), nameof(CompVehicleLauncher.Launch))]
[HarmonyPriority(Priority.Last)]
public static class Patch_Launch_Flight
{
    public static bool Prefix(CompVehicleLauncher __instance, TargetData<GlobalTargetInfo> targetData, IArrivalAction arrivalAction, bool __runOriginal)
    {
        if (!__runOriginal) return false;
        VehiclePawn vehicle = __instance.Vehicle;
        if (vehicle == null || !vehicle.Spawned || vehicle.GetComp<CompPhobosFlight>() is not { Active: true } flight) return true;
        List<FlightNode> nodes = targetData.targets.Select(target => new FlightNode(target)).ToList();
        flight.Depart(nodes, arrivalAction);
        return false;
    }
}

/// <summary>
/// The launch button for an aircraft of ours in the air, answered afresh (finding 6): Vehicle Framework judged it as standing on the ground,
/// refusing it for a roof over the cell beneath or a diagonal heading, neither of which matters in the air. It runs first, so our other
/// refusals (damaged controls, a closed sky, a sling) still follow. A postfix.
/// Framework 0.151.0: on the ground too, after Vehicle Framework's own refusals, our flight answers in place of the hover-limit check: a
/// straight lift-off, a roll along a clear strip ahead, or why neither will do (the design's slice 4).
/// </summary>
[HarmonyPatch(typeof(CompVehicleLauncher), nameof(CompVehicleLauncher.CanLaunchWithCargoCapacity))]
[HarmonyPriority(Priority.First)]
public static class Patch_CanLaunch_Flight
{
    public static void Postfix(CompVehicleLauncher __instance, ref bool __result, ref string disableReason)
    {
        CompPhobosFlight? flight = FlightRegistry.Get(__instance.Vehicle);
        if (flight == null)
        {
            // On the ground: Vehicle Framework's refusal stands; ours is asked only where it would allow.
            if (!__result || __instance.Vehicle is not { Spawned: true } vehicle || vehicle.GetComp<CompPhobosFlight>() is not { Active: true } grounded) return;
            flight = grounded;
        }
        string? refused = flight.DepartRefusal();
        __result = refused == null;
        disableReason = refused!;
    }
}

/// <summary>Landing on a spot chosen in the destination picker: an aircraft on our flight comes in over the edge and flies to it, rather than
/// Vehicle Framework's stand-in dropping onto it. A prefix that skips the original for ours only; a crash keeps Vehicle Framework's own.</summary>
[HarmonyPatch(typeof(ArrivalAction_LandToCell), "SpawnSkyfaller")]
public static class Patch_LandToCell_Flight
{
    public static bool Prefix(ArrivalAction_LandToCell __instance, VehiclePawn ___vehicle, MapParent ___mapParent, IntVec3 ___landingCell, Rot4 ___landingRot)
    {
        if (__instance is ArrivalAction_CrashInMap || !FlightArrival.Flies(___vehicle) || ___mapParent?.Map is not { } map) return true;
        return !FlightArrival.Enter(___vehicle, map, ArrivalIntent.Land, ___landingCell, ___landingRot);
    }
}

/// <summary>Arriving on a map loaded or made for the journey (a settlement attacked, a site), dropped in its middle: ours flies in over the
/// edge to the middle and holds. A prefix that skips the original for ours only.</summary>
[HarmonyPatch(typeof(AerialVehicleArrivalModeWorker_CenterDrop), nameof(AerialVehicleArrivalModeWorker_CenterDrop.VehicleArrived))]
public static class Patch_CenterDrop_Flight
{
    public static bool Prefix(VehiclePawn vehicle, Map map) =>
        !FlightArrival.Flies(vehicle) || !FlightArrival.Enter(vehicle, map, ArrivalIntent.Centre, IntVec3.Invalid, Rot4.North);
}

/// <summary>The same, dropped at the edge: ours holds just inside the edge it flew in over. A prefix that skips the original for ours only.</summary>
[HarmonyPatch(typeof(AerialVehicleArrivalModeWorker_EdgeDrop), nameof(AerialVehicleArrivalModeWorker_EdgeDrop.VehicleArrived))]
public static class Patch_EdgeDrop_Flight
{
    public static bool Prefix(VehiclePawn vehicle, Map map) =>
        !FlightArrival.Flies(vehicle) || !FlightArrival.Enter(vehicle, map, ArrivalIntent.Edge, IntVec3.Invalid, Rot4.North);
}

/// <summary>The same, landing where the player picks: ours holds inside the edge and the landing outline opens for the spot. A prefix that skips
/// the original for ours only.</summary>
[HarmonyPatch(typeof(AerialVehicleArrivalModeWorker_TargetedDrop), nameof(AerialVehicleArrivalModeWorker_TargetedDrop.VehicleArrived))]
public static class Patch_TargetedDrop_Flight
{
    public static bool Prefix(VehiclePawn vehicle, Map map) =>
        !FlightArrival.Flies(vehicle) || !FlightArrival.Enter(vehicle, map, ArrivalIntent.Targeted, IntVec3.Invalid, Rot4.North);
}
