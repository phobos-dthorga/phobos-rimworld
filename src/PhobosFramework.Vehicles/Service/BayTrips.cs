using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Environment;
using Phobos.Framework.Rules;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Phobos.VehicleSystems;

/// <summary>Whether a vehicle has work that sends it to a dyno bay, whether any of it can be paid for, and what it waits for if not.</summary>
public readonly struct BayTripNeed
{
    public readonly bool Any;
    public readonly bool Payable;
    /// <summary>The first piece of work nothing in the colony can pay for, in the player's words; null when all can be.</summary>
    public readonly string? Missing;

    public BayTripNeed(bool any, bool payable, string? missing)
    {
        Any = any;
        Payable = payable;
        Missing = missing;
    }
}

/// <summary>
/// The service behind bay trips (Framework 0.201.0; docs/development/vehicle-bay-trips.md): what work sends a vehicle and whether it can
/// be paid for, which vehicles are left alone, where at a bay a vehicle may stand, and where it goes afterwards. The Fleet board's switch
/// goes through here. Each vehicle's own trip is <see cref="CompPhobosBayTrip"/>'s; nothing here keeps state.
/// </summary>
public static class BayTrips
{
    /// <summary>How many places may each be tested for a way there in one search: the test is Vehicle Framework's region search.</summary>
    const int MaxWayTests = 16;
    /// <summary>How far past a bay's reach a spot nearby may lie, in cells (authored).</summary>
    const int NearbyRings = 14;

    static VehicleSystemsSettings Settings => VehicleSystemsMod.Settings;

    /// <summary>The player's setting: vehicles go to a dyno bay for repair by themselves.</summary>
    public static bool On => Settings.bayTrips;

    public static IReadOnlyList<CompPhobosBayTrip> Of(Map? map) => MapRegistry<CompPhobosBayTrip>.On(map);

    /// <summary>The Fleet board's switch: keeps a vehicle out of bay trips, or lets it go again, and says so.</summary>
    public static void SetKeptOut(CompPhobosBayTrip trip, bool keptOut)
    {
        if (trip.KeptOut == keptOut) return;
        trip.SetKeptOut(keptOut);
        Messages.Message((keptOut ? "PhobosBayTrip_KeptOutSet" : "PhobosBayTrip_KeptOutCleared").Translate(trip.Vehicle.LabelShort.Named("VEHICLE")).Resolve(),
            trip.Vehicle, MessageTypeDefOf.SilentInput, historical: false);
    }

    // ---- What sends a vehicle ----

    /// <summary>
    /// The work that sends this vehicle to a bay at the player's level (<see cref="BayTripRule.Sends"/>), and whether any of it can be paid
    /// for with what is aboard or lies unforbidden on its map. Reads the lists the systems keep; a field fix is never counted, since the
    /// player asks for those where the vehicle stands.
    /// </summary>
    public static BayTripNeed NeedOf(CompPhobosVehicleSystems systems)
    {
        BayTripTrigger level = Settings.bayTripTrigger;
        bool any = false, payable = false;
        string? missing = null;
        foreach (RepairWork repair in systems.Repairs.Pending)
        {
            if (repair.Kind == RepairKind.FieldFix) continue;
            BayTripWorkKind kind = repair.NeedsRig ? BayTripWorkKind.BayOnly
                : repair.Kind is RepairKind.ReplacePart or RepairKind.ConditionService ? BayTripWorkKind.Broken
                : BayTripWorkKind.Routine;
            if (!BayTripRule.Sends(level, kind)) continue;
            any = true;
            if (BayTripRule.Payable(repair.Options, line => Have(systems, line.def) >= line.count)) payable = true;
            else if (missing == null && repair.Options.Count > 0) missing = MissingLine(repair.Label, repair.Options[0].Select(line => (line.def, line.count)));
        }
        foreach (SystemState state in systems.DueServices())
        {
            if (state.Def.service is not { } service) continue;
            if (!BayTripRule.Sends(level, service.needsRig ? BayTripWorkKind.BayOnly : BayTripWorkKind.Routine)) continue;
            any = true;
            if (service.SuppliesNow.All(supply => Have(systems, supply.thingDef) >= supply.count)) payable = true;
            else missing ??= MissingLine(service.label, service.SuppliesNow.Select(supply => (supply.thingDef, supply.count)));
        }
        // A diagnosis asked for is done only at a bay, and costs nothing.
        if (systems.Diagnostics.Requested) any = payable = true;
        if (BayTripRule.Sends(level, BayTripWorkKind.Routine) && FreeRepairWaits(systems)) any = payable = true;
        return new BayTripNeed(any, payable, payable ? null : missing);
    }

    /// <summary>Whether a damaged part waits for the free repair any mechanic does: damaged, and not waiting for a replacement.</summary>
    public static bool FreeRepairWaits(CompPhobosVehicleSystems systems)
    {
        List<VehicleComponent>? parts = systems.Vehicle.statHandler?.components;
        if (parts == null) return false;
        for (int i = 0; i < parts.Count; i++)
        {
            if (parts[i].HealthPercent < 1f && !systems.Repairs.PartBlocked(parts[i])) return true;
        }
        return false;
    }

    static string MissingLine(string work, IEnumerable<(ThingDef def, int count)> supplies) =>
        "PhobosBayTrip_WaitsForParts".Translate(work.CapitalizeFirst().Named("WORK"),
            supplies.Select(line => line.def.label + " x" + line.count).ToCommaList(useAnd: true).Named("SUPPLIES")).Resolve();

    /// <summary>How many of a thing the vehicle could have: aboard, and lying unforbidden on its map.</summary>
    static int Have(CompPhobosVehicleSystems systems, ThingDef def)
    {
        int count = systems.CountInCargo(def);
        Map? map = systems.Vehicle.Map;
        if (map == null) return count;
        // A building named as a supply is counted packed (PackedThings, Framework 0.265.0).
        foreach (Thing thing in Phobos.Framework.Items.PackedThings.OnMap(map, def))
        {
            if (!thing.IsForbidden(Faction.OfPlayer)) count += thing.stackCount;
        }
        return count;
    }

    // ---- Which vehicles are left alone ----

    /// <summary>
    /// Whether the vehicle is in use, so no trip of ours may take it: anyone aboard, its engine on, moving, forbidden, burning, part of a
    /// caravan forming, towing or towed or waiting to be coupled, set up for work, or on another errand of ours.
    /// </summary>
    public static bool Busy(VehiclePawn vehicle, CompPhobosBayTrip self)
    {
        if (vehicle.Drafted || vehicle.vehiclePather is { Moving: true } || vehicle.AllPawnsAboard.Count > 0) return true;
        if (vehicle.IsForbidden(Faction.OfPlayer) || AttachedFires.OnFire(vehicle) || vehicle.GetLord() != null) return true;
        if (Hitches.Towing(vehicle) != null || Hitches.WaitingOn(vehicle) != null) return true;
        // Its own rear half is part of an articulated vehicle, not a tow (the fault-read of 21 Nov 2026, H12: such a vehicle never went).
        if (vehicle.GetComp<CompPhobosHitch>() is { } hitch
            && ((hitch.Trailer != null && !Couplings.Permanent(hitch.Trailer)) || hitch.Pending != null || hitch.Winching != null)) return true;
        if (CompPhobosHitch.SetUpRefusal(vehicle) != null) return true;
        foreach (ThingComp comp in vehicle.AllComps)
        {
            if (comp != self && comp is CompPhobosVehicleRun run && (run.Running || run.WantsDriver)) return true;
        }
        return false;
    }

    // ---- A place at a bay ----

    /// <summary>
    /// Whether the vehicle stands where the mechanics have it: in reach of a dyno bay, or in a working service yard, where it takes its turn on
    /// the dyno where it stands (Framework 0.210.0).
    /// </summary>
    public static bool AtBayOrYard(CompPhobosVehicleSystems systems) => systems.Fittings.NearRig() || ServiceYards.InWorkingYard(systems.Vehicle);

    /// <summary>The cells other trips on this map keep for where their drives end.</summary>
    internal static List<CellRect> KeptBy(Map map, VehiclePawn except)
    {
        var kept = new List<CellRect>();
        IReadOnlyList<CompPhobosBayTrip> trips = Of(map);
        for (int i = 0; i < trips.Count; i++)
        {
            if (trips[i].Vehicle != except && trips[i].KeptCells is { } cells) kept.Add(cells);
        }
        return kept;
    }

    /// <summary>
    /// A place for this vehicle in reach of a working dyno bay of the player's, nearest bay first: where its centre stands and the way it
    /// faces. False with the reason in the player's words: no bay, none with power, or none with room and a way there.
    /// </summary>
    public static bool TryFindPlace(VehiclePawn vehicle, ICollection<IntVec3>? avoid, Rot4? onlyFacing, out Thing? bay, out IntVec3 cell, out Rot4 facing,
        out string? why)
    {
        bay = null;
        cell = IntVec3.Invalid;
        facing = Rot4.Invalid;
        why = null;
        Map map = vehicle.Map;
        bool anyBay = false, anyWorking = false;
        List<CellRect> kept = KeptBy(map, vehicle);
        int wayTests = 0;
        // A service yard first (Framework 0.210.0; owner, 20 Nov 2026: "Bay trips aim at a yard first"): a place wholly in a working yard
        // with a working dyno bay. A yard full of vehicles with nothing left to do sends the one idle longest out to make room, for a later look.
        if (ServiceYards.TryFindYardPlace(vehicle, kept, avoid, onlyFacing, MaxWayTests, ref wayTests, out bay, out cell, out facing)) return true;
        foreach (CompPhobosTuningRig rig in CompPhobosTuningRig.PlayersOn(map).OrderBy(r => r.parent.Position.DistanceToSquared(vehicle.Position)))
        {
            anyBay = true;
            if (!rig.Working) continue;
            anyWorking = true;
            if (!TryFindSpotAt(vehicle, rig, kept, avoid, onlyFacing, ref wayTests, out cell, out facing)) continue;
            bay = rig.parent;
            return true;
        }
        why = (!anyBay ? "PhobosBayTrip_NoBay" : !anyWorking ? "PhobosBayTrip_BayOff" : "PhobosBayTrip_NoRoom").Translate().Resolve();
        return false;
    }

    /// <summary>
    /// A place in one bay's reach: every cell near enough that the vehicle's body, standing there, lies within the bay's reach, nearest the
    /// vehicle first, facing the bay if it can. The body must fit on ground it can drive, clear of the bay, of doors, of other vehicles and
    /// of the places other trips keep; and Vehicle Framework must find a way there.
    /// </summary>
    static bool TryFindSpotAt(VehiclePawn vehicle, CompPhobosTuningRig rig, List<CellRect> kept, ICollection<IntVec3>? avoid, Rot4? onlyFacing,
        ref int wayTests, out IntVec3 cell, out Rot4 facing)
    {
        cell = IntVec3.Invalid;
        facing = Rot4.Invalid;
        Map map = vehicle.Map;
        IntVec3 from = vehicle.Position;
        CellRect rigRect = rig.parent.OccupiedRect();
        int reach = rig.Props.reach;
        int half = Mathf.Max(vehicle.def.size.x, vehicle.def.size.z) / 2 + 1;
        List<IntVec3> candidates = rigRect.ExpandedBy(reach + half).Cells
            .Where(c => c.InBounds(map) && !rigRect.Contains(c) && (avoid == null || !avoid.Contains(c)))
            .OrderBy(c => c.DistanceToSquared(from))
            .ToList();
        foreach (IntVec3 candidate in candidates)
        {
            if (!candidate.Standable(vehicle, map)) continue;
            foreach (Rot4 rot in FacingsTowards(candidate, rigRect.CenterCell))
            {
                // A vehicle carried as it stands (towed, pushed) takes only a place it fits facing as it faces.
                if (onlyFacing is { } only && only != rot) continue;
                CellRect body = vehicle.VehicleRect(candidate, rot);
                if (!body.ExpandedBy(reach).Overlaps(rigRect) || !BodyFits(vehicle, body, map, kept, rig.parent)) continue;
                // A carried vehicle is not asked for a way of its own: it will not drive, and what tows or pushes it finds the way.
                if (candidate != from && onlyFacing == null)
                {
                    if (wayTests++ >= MaxWayTests) return false;
                    if (!vehicle.CanReachVehicle(candidate, PathEndMode.OnCell, Danger.Deadly, TraverseMode.ByPawn)) break;
                }
                cell = candidate;
                facing = rot;
                return true;
            }
        }
        return false;
    }

    /// <summary>The four facings, the one that points most nearly at a cell first.</summary>
    static IEnumerable<Rot4> FacingsTowards(IntVec3 from, IntVec3 to)
    {
        IntVec3 d = to - from;
        Rot4 first = Mathf.Abs(d.x) > Mathf.Abs(d.z) ? (d.x > 0 ? Rot4.East : Rot4.West) : (d.z >= 0 ? Rot4.North : Rot4.South);
        yield return first;
        yield return first.Opposite;
        yield return first.Rotated(RotationDirection.Clockwise);
        yield return first.Rotated(RotationDirection.Counterclockwise);
    }

    /// <summary>Whether the body can stand on these cells: on the map, on ground it can drive, clear of doors, of other vehicles, of the
    /// places other trips keep, and of a building's own cells and the cell a mechanic works it from.</summary>
    internal static bool BodyFits(VehiclePawn vehicle, CellRect body, Map map, List<CellRect> kept, Thing? clearOf)
    {
        if (clearOf != null && (body.Overlaps(clearOf.OccupiedRect()) || (clearOf.def.hasInteractionCell && body.Contains(clearOf.InteractionCell)))) return false;
        foreach (IntVec3 c in body)
        {
            if (!c.InBounds(map) || !vehicle.Drivable(c) || c.GetDoor(map) != null) return false;
        }
        return ClearOfVehicles(vehicle, body, map, kept);
    }

    /// <summary>Whether these cells hold no other vehicle and none of the places other trips keep.</summary>
    internal static bool ClearOfVehicles(VehiclePawn vehicle, CellRect cells, Map map, List<CellRect> kept)
    {
        for (int i = 0; i < kept.Count; i++)
        {
            if (kept[i].Overlaps(cells)) return false;
        }
        foreach (IntVec3 c in cells)
        {
            if (!c.InBounds(map)) continue;
            List<Thing> things = c.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i] is VehiclePawn other && other != vehicle) return false;
            }
        }
        return true;
    }

    // ---- Carrying a vehicle that cannot drive ----

    /// <summary>
    /// The nearest vehicle that could tow this wreck to the bay now, or null: the player's, on no trip and not kept out, fitted with the
    /// recovery winch, free, able to drive once a driver is aboard, and heavy enough that the tow needs no warning (the tow ratio: an
    /// over-heavy tow is the player's own choice, never made for them).
    /// </summary>
    public static CompPhobosBayTrip? FindTug(VehiclePawn wreck)
    {
        CompPhobosBayTrip? best = null;
        int bestDistance = int.MaxValue;
        IReadOnlyList<CompPhobosBayTrip> trips = Of(wreck.Map);
        for (int i = 0; i < trips.Count; i++)
        {
            CompPhobosBayTrip trip = trips[i];
            VehiclePawn tug = trip.Vehicle;
            if (tug == wreck || trip.Stage != BayTripStage.None || trip.KeptOut || !trip.TakesPart || tug.Faction != Faction.OfPlayer) continue;
            if (tug.GetComp<CompPhobosHitch>() is not { CanWinch: true } hitch || Busy(tug, trip)) continue;
            if (hitch.Refusal() != null || hitch.RefusalFor(wreck, checkReach: false) != null || hitch.RecoveryTowWarning(wreck) != null) continue;
            if (VehicleAutoDrive.CannotDrive(tug, needsDriver: false) != null) continue;
            int distance = tug.Position.DistanceToSquared(wreck.Position);
            if (distance >= bestDistance) continue;
            best = trip;
            bestDistance = distance;
        }
        return best;
    }

    /// <summary>Turns a standing vehicle where it stands, as Vehicle Framework does on arrival, and has the map take note of the cells it
    /// left and took, as a move of ours does (VehicleShift).</summary>
    internal static void Turn(VehiclePawn vehicle, Rot4 rot)
    {
        Map map = vehicle.Map;
        CellRect before = vehicle.OccupiedRect();
        Hitches.TakeUpPipes(vehicle);
        VehicleAutoDrive.Face(vehicle, rot);
        foreach (IntVec3 c in before.Cells)
        {
            if (c.InBounds(map)) map.pathing.RecalculatePerceivedPathCostAt(c);
        }
        foreach (IntVec3 c in vehicle.OccupiedRect().Cells)
        {
            if (c.InBounds(map)) map.pathing.RecalculatePerceivedPathCostAt(c);
        }
    }

    /// <summary>Whether anyone stands on cells a moved vehicle is about to take: it waits for them, and never moves onto a person or an animal.</summary>
    internal static bool SomeoneOn(VehiclePawn vehicle, CellRect cells, Map map)
    {
        CellRect own = vehicle.OccupiedRect();
        foreach (IntVec3 c in cells)
        {
            if (own.Contains(c) || !c.InBounds(map)) continue;
            List<Thing> things = c.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i] is Pawn pawn && pawn != vehicle) return true;
            }
        }
        return false;
    }

    // ---- Where it goes afterwards ----

    /// <summary>
    /// Where a vehicle goes once its work at the bay is done, in the owner's order (20 Nov 2026): its depot bay if it has one and the pad
    /// is free; else a place in a vehicle park; else a spot nearby, clear of every bay's reach, of every depot pad, of doors and of
    /// zones (nobody parks on a crop). False when nowhere can be found, or it stands in a park already: it then stays where it is.
    /// </summary>
    public static bool TryFindHome(VehiclePawn vehicle, out IntVec3 cell, out Rot4 facing, out BayTripHome kind, bool leavingYard = false)
    {
        Map map = vehicle.Map;
        List<CellRect> kept = KeptBy(map, vehicle);
        bool depotBay = TryFindDepotBay(vehicle, kept, out cell, out facing);
        // A vehicle worked in a service yard stays there unless it has a depot bay of its own (owner, 20 Nov 2026), and is moved out only to
        // make room (leavingYard).
        if (!depotBay && !leavingYard && ServiceYards.InWorkingYard(vehicle))
        {
            kind = BayTripHome.Yard;
            return false;
        }
        // With no depot bay to go to, a vehicle standing in a park is home already.
        if (!depotBay && map.zoneManager.ZoneAt(vehicle.Position) is Zone_PhobosVehiclePark)
        {
            kind = BayTripHome.Park;
            return false;
        }
        bool park = !depotBay && VehicleParks.TryFindPlace(vehicle, cells => ClearOfVehicles(vehicle, cells, map, kept),
            body => BodyFits(vehicle, body, map, kept, null),
            c => vehicle.CanReachVehicle(c, PathEndMode.OnCell, Danger.Deadly, TraverseMode.ByPawn), MaxWayTests, out cell, out facing);
        kind = BayTripRule.Home(depotBay, park);
        if (kind != BayTripHome.Nearby) return true;
        return TryFindNearby(vehicle, kept, out cell, out facing);
    }

    /// <summary>The vehicle's own depot bay, if it has one, is not on it already, its pad is free and there is a way: nose to the bay.</summary>
    static bool TryFindDepotBay(VehiclePawn vehicle, List<CellRect> kept, out IntVec3 cell, out Rot4 facing)
    {
        cell = IntVec3.Invalid;
        facing = Rot4.Invalid;
        if (DepotBays.Of(vehicle) is not { } depot || depot.Holds(vehicle)) return false;
        IntVec3 park = depot.ParkingCell(vehicle);
        Rot4 nose = depot.parent.Rotation.Opposite;
        if (!park.InBounds(vehicle.Map) || !BodyFits(vehicle, vehicle.VehicleRect(park, nose), vehicle.Map, kept, depot.parent)) return false;
        if (!vehicle.CanReachVehicle(park, PathEndMode.OnCell, Danger.Deadly, TraverseMode.ByPawn)) return false;
        cell = park;
        facing = nose;
        return true;
    }

    /// <summary>A spot a short way off, nearest first, where the body stands clear of every bay's reach and every depot pad.</summary>
    static bool TryFindNearby(VehiclePawn vehicle, List<CellRect> kept, out IntVec3 cell, out Rot4 facing)
    {
        cell = IntVec3.Invalid;
        facing = Rot4.Invalid;
        Map map = vehicle.Map;
        var keepClear = new List<CellRect>();
        foreach (CompPhobosTuningRig rig in CompPhobosTuningRig.PlayersOn(map)) keepClear.Add(rig.parent.OccupiedRect().ExpandedBy(rig.Props.reach));
        foreach (CompPhobosDepotBay depot in DepotBays.On(map)) keepClear.Add(depot.Pad);
        int length = Mathf.Max(vehicle.def.size.x, vehicle.def.size.z);
        int wayTests = 0;
        Rot4[] facings = { vehicle.Rotation, vehicle.Rotation.Rotated(RotationDirection.Clockwise) };
        for (int ring = length; ring <= length + NearbyRings; ring++)
        {
            foreach (IntVec3 candidate in CellRect.CenteredOn(vehicle.Position, ring).EdgeCells.OrderBy(c => c.DistanceToSquared(vehicle.Position)))
            {
                if (!candidate.InBounds(map) || !candidate.Standable(vehicle, map)) continue;
                foreach (Rot4 rot in facings)
                {
                    CellRect body = vehicle.VehicleRect(candidate, rot);
                    if (keepClear.Any(clear => clear.Overlaps(body)) || !BodyFits(vehicle, body, map, kept, null) || body.Cells.Any(c => map.zoneManager.ZoneAt(c) != null)) continue;
                    if (wayTests++ >= MaxWayTests) return false;
                    if (!vehicle.CanReachVehicle(candidate, PathEndMode.OnCell, Danger.Deadly, TraverseMode.ByPawn)) break;
                    cell = candidate;
                    facing = rot;
                    return true;
                }
            }
        }
        return false;
    }
}
