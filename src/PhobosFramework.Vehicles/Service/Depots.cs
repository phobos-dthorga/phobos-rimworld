using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Depots and forward fuel points (Framework 0.199.0; docs/development/vehicle-depots.md, batch 1: DP1, DP1(b), DP2, DP3(b), DP5, DP6).
/// A depot is fuel and gear left on a world-map tile for a later trip: vehicles and trailers with what is in their cargo and tanks, and
/// bare crates of items. It is the lost-convoy place (PhobosSlingWreck) chosen on purpose, with its own Def, and never expires by itself.
/// A convoy standing on the tile leaves and takes items and leaves vehicles through one window; refuels its tanks from what the depot
/// holds; and visits it, on a small map, to drive its vehicles away. An aircraft on a journey that passes over a depot holding its fuel,
/// with its refuel switch on and its tank below half, sets down there and fills up. All the rules are here; the window and buttons only ask.
/// The player's own depots only.
/// </summary>
public static class Depots
{
    /// <summary>An aircraft passing a depot stops for fuel below this share of its tank (authored: below half, so it never stops for a sip).</summary>
    public const float AircraftStopBelow = 0.5f;

    /// <summary>How near the aircraft's track a depot must be for it to stop, in tiles (authored: the tile beneath or one beside it).</summary>
    public const float AircraftStopTiles = 1.5f;

    public static WorldObjectDef DepotDef => PhobosSlingWreckDefOf.PhobosDepot;

    /// <summary>The player's depot on this tile, or null.</summary>
    public static PhobosSlingWreck? At(PlanetTile tile)
    {
        if (!tile.Valid || Find.WorldObjects == null) return null;
        foreach (WorldObject thing in Find.WorldObjects.ObjectsAt(tile))
        {
            if (thing is PhobosSlingWreck { IsDepot: true } depot && !depot.Destroyed) return depot;
        }
        return null;
    }

    /// <summary>Every depot of the player's on the world.</summary>
    public static IEnumerable<PhobosSlingWreck> All()
    {
        if (Find.WorldObjects == null) yield break;
        foreach (WorldObject thing in Find.WorldObjects.AllWorldObjects)
        {
            if (thing is PhobosSlingWreck { IsDepot: true } depot && !depot.Destroyed) yield return depot;
        }
    }

    // ---- Leaving and taking (DP1, DP1(b), DP2's swap) ----

    /// <summary>Why a convoy cannot leave a depot where it stands, or null. A depot shares its tile with nothing but convoys and another
    /// depot of ours; never on an enemy's ground (agent's choice, the design's "hostile ground").</summary>
    public static string? LeaveRefusal(VehicleCaravan caravan)
    {
        if (caravan.Faction != Faction.OfPlayer) return "PhobosDepot_NotOurs".Translate().Resolve();
        if (caravan.vehiclePather is { MovingNow: true }) return "PhobosDepot_Moving".Translate().Resolve();
        if (CargoRefuel.NoLandingReason(caravan.Tile) is { } ground) return ground;
        foreach (WorldObject thing in Find.WorldObjects.ObjectsAt(caravan.Tile))
        {
            if (thing is Caravan || thing is PhobosSlingWreck { IsDepot: true }) continue;
            return "PhobosDepot_TileTaken".Translate(thing.LabelShort.Named("PLACE")).Resolve();
        }
        return null;
    }

    /// <summary>The vehicles a convoy could leave: its own, and the trailers its vehicles tow along.</summary>
    public static List<VehiclePawn> Leavable(VehicleCaravan caravan)
    {
        var list = new List<VehiclePawn>();
        foreach (VehiclePawn vehicle in caravan.VehiclesListForReading)
        {
            list.Add(vehicle);
            // Never one half of an articulated vehicle by itself: the two go, or stay, as one (the fault-read of 21 Nov 2026, H5).
            if (CompPhobosHitch.LeavableTowedAlong(vehicle) is { } towed) list.Add(towed);
        }
        return list;
    }

    /// <summary>Why leaving these vehicles cannot go ahead, or null: the convoy must keep one vehicle that is not towed (Vehicle Framework's
    /// convoy is made of vehicles; to leave every one, its own Stash is the way).</summary>
    public static string? VehiclesRefusal(VehicleCaravan caravan, ICollection<VehiclePawn> leaving)
    {
        if (leaving.Count == 0) return null;
        bool keepsOne = caravan.VehiclesListForReading.Any(v => !leaving.Contains(v));
        return keepsOne ? null : "PhobosDepot_KeepOne".Translate().Resolve();
    }

    /// <summary>
    /// Leaves the chosen vehicles and items at the depot on the convoy's tile, making it if there is none, and gives the convoy the chosen
    /// items from the depot. Everyone aboard a vehicle left behind steps out and walks with the convoy. Returns the depot, or null if
    /// nothing was moved. One message says what happened.
    /// </summary>
    public static PhobosSlingWreck? Exchange(VehicleCaravan caravan, ICollection<VehiclePawn> leaveVehicles, List<TransferableOneWay> leaveItems,
        List<TransferableOneWay> takeItems)
    {
        string? refusal = LeaveRefusal(caravan) ?? VehiclesRefusal(caravan, leaveVehicles);
        if (refusal != null)
        {
            Messages.Message(refusal, caravan, MessageTypeDefOf.RejectInput, historical: false);
            return null;
        }
        PhobosSlingWreck? depot = At(caravan.Tile);
        int fuelBefore = depot != null ? TotalFuel(depot) : 0;
        int left = 0, taken = 0;
        bool leaving = leaveVehicles.Count > 0 || leaveItems.Any(t => t.CountToTransfer > 0);
        if (leaving && depot == null) depot = PhobosSlingWreck.MakeDepot(caravan.Tile);
        foreach (VehiclePawn vehicle in leaveVehicles.ToList())
        {
            if (LeaveVehicle(caravan, vehicle, depot!)) left++;
        }
        foreach (TransferableOneWay item in leaveItems)
        {
            if (item.CountToTransfer <= 0) continue;
            left += item.CountToTransfer;
            TransferableUtility.Transfer(item.things, item.CountToTransfer, (thing, _) => depot!.Keep(thing));
        }
        if (depot != null)
        {
            foreach (TransferableOneWay item in takeItems)
            {
                if (item.CountToTransfer <= 0) continue;
                taken += item.CountToTransfer;
                TransferableUtility.Transfer(item.things, item.CountToTransfer, (thing, _) =>
                {
                    thing.holdingOwner?.Remove(thing);
                    CaravanInventoryUtility.GiveThing(caravan, thing);
                });
            }
            caravan.RecacheVehicles();
            // Framework 0.200.0 (DP9): fuel added freshens the depot's fuel in proportion, as fresh fuel mixed with old does.
            int fuelAfter = TotalFuel(depot);
            if (fuelAfter > fuelBefore)
            {
                depot.fuelClock = DepotPressureRule.BlendClock(fuelBefore, depot.fuelClock < 0 ? Find.TickManager.TicksGame : depot.fuelClock,
                    fuelAfter - fuelBefore, Find.TickManager.TicksGame);
            }
            depot.RemoveIfEmpty();
        }
        if (left == 0 && taken == 0) return depot;
        Messages.Message("PhobosDepot_Exchanged".Translate(caravan.LabelShort.Named("CONVOY"), left.Named("LEFT"), taken.Named("TAKEN")).Resolve(),
            caravan, MessageTypeDefOf.TaskCompletion, historical: false);
        if (depot != null && Seen.Wants("vehicles.depot", depot.Vehicles.FirstOrDefault(), "exchanged")) Seen.Note("vehicles.depot", depot.Vehicles.FirstOrDefault(), left + " left, " + taken + " taken", "exchanged");
        return depot;
    }

    /// <summary>One vehicle out of the convoy and into the depot: its riders step out first; a towed trailer is let go of first.</summary>
    static bool LeaveVehicle(VehicleCaravan caravan, VehiclePawn vehicle, PhobosSlingWreck depot)
    {
        foreach (Pawn rider in vehicle.AllPawnsAboard.ToList()) vehicle.DisembarkPawn(rider);
        if (caravan.VehiclesListForReading.Contains(vehicle))
        {
            caravan.RemovePawn(vehicle);
        }
        else
        {
            // A trailer towed along: its tractor lets go of it off the map.
            VehiclePawn? tractor = caravan.VehiclesListForReading.FirstOrDefault(v => CompPhobosHitch.TowedAlong(v) == vehicle);
            if (tractor?.GetComp<CompPhobosHitch>()?.LetGoHeld() != vehicle) return false;
        }
        depot.Keep(vehicle);
        if (vehicle.GetComp<CompPhobosVehicleSystems>() is { Usable: true } comp) VehicleStory.Log(comp, DepotDefOf.PhobosLog_LeftAtDepot);
        return true;
    }

    // ---- Hooking a trailer up again (Framework 0.324.0, EM41) ----

    /// <summary>Why this vehicle of the convoy cannot hook up this trailer from the depot here (any trailer, when null), or null. The convoy
    /// must stand on the depot's tile; the hitch says the rest.</summary>
    public static string? HookUpRefusal(VehicleCaravan caravan, VehiclePawn tractor, VehiclePawn? trailer)
    {
        if (caravan.vehiclePather is { MovingNow: true }) return "PhobosDepot_Moving".Translate().Resolve();
        if (tractor.GetComp<CompPhobosHitch>() is not { } hitch) return "PhobosHitch_NotFitted".Translate(tractor.LabelShort.Named("TRACTOR")).Resolve();
        if (trailer == null) return hitch.Refusal();
        if (At(caravan.Tile) is not { } depot || !depot.Vehicles.Contains(trailer)) return "PhobosDepot_NoDepotHere".Translate().Resolve();
        return hitch.HookUpRefusal(trailer);
    }

    /// <summary>
    /// The trailer out of the depot and onto the vehicle's hitch, off the map, as if it had been towed there (the owner's "drop and hook",
    /// EM41): no visit to the depot's map. A trailer on wrecked gear is said plainly first, as coupling one on a map is (EM42).
    /// </summary>
    public static void HookUp(VehicleCaravan caravan, VehiclePawn tractor, VehiclePawn trailer)
    {
        if (HookUpRefusal(caravan, tractor, trailer) is { } refusal)
        {
            Messages.Message(refusal, caravan, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        PhobosSlingWreck depot = At(caravan.Tile)!;
        if (TrailerFaults.CouplingWarning(trailer) is { } wrecked) Messages.Message(wrecked, caravan, MessageTypeDefOf.CautionInput, historical: false);
        if (!tractor.GetComp<CompPhobosHitch>()!.TakeHeld(trailer))
        {
            Messages.Message("PhobosDepot_HookFailed".Translate(trailer.LabelShort.Named("TRAILER")).Resolve(), caravan, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        caravan.RecacheVehicles();
        depot.RemoveIfEmpty();
        VehicleStory.Hitched(tractor, trailer);
        Messages.Message("PhobosDepot_Hooked".Translate(tractor.LabelShort.Named("TRACTOR"), trailer.LabelShort.Named("TRAILER")).Resolve(), caravan,
            MessageTypeDefOf.TaskCompletion, historical: false);
        if (Seen.Wants("vehicles.depot-hook", trailer)) Seen.Note("vehicles.depot-hook", trailer, tractor.LabelShort);
    }

    // ---- Fuel (DP2, DP3(b)) ----

    /// <summary>The tank compartments of a depot's vehicles that hold this fuel, and how a tank unit pours into a vehicle's tank (one for
    /// one, as the tanker's hose pours: Port_VehicleFuel).</summary>
    static IEnumerable<TankCompartment> TanksOf(PhobosSlingWreck depot, ThingDef fuel)
    {
        foreach (VehiclePawn vehicle in depot.Vehicles)
        {
            if (vehicle.GetComp<CompPhobosTank>() is not { } tank) continue;
            foreach (TankCompartment compartment in tank.Compartments)
            {
                if (compartment.HasLoad && compartment.contents?.ports.Any(p => p is Port_VehicleFuel port && port.thing == fuel) == true) yield return compartment;
            }
        }
    }

    /// <summary>The items of this fuel a depot holds: in crates on the ground and in its vehicles' cargo.</summary>
    static IEnumerable<Thing> FuelItems(PhobosSlingWreck depot, ThingDef fuel)
    {
        foreach (Thing thing in depot.Items) if (thing.def == fuel) yield return thing;
        foreach (VehiclePawn vehicle in depot.Vehicles)
        {
            if (vehicle.inventory?.innerContainer is not { } hold) continue;
            foreach (Thing thing in hold) if (thing.def == fuel) yield return thing;
        }
    }

    /// <summary>How much of this fuel a depot holds, in fuel units: items and tanks together.</summary>
    public static int FuelUnits(PhobosSlingWreck depot, ThingDef fuel) =>
        FuelItems(depot, fuel).Sum(t => t.stackCount) + (int)TanksOf(depot, fuel).Sum(c => c.amount);

    /// <summary>Takes up to this much fuel out of a depot, items first then tanks; returns what it took. What is taken is used up.</summary>
    public static int Draw(PhobosSlingWreck depot, ThingDef fuel, int most)
    {
        int taken = 0;
        foreach (Thing thing in FuelItems(depot, fuel).ToList())
        {
            if (taken >= most) break;
            int take = System.Math.Min(most - taken, thing.stackCount);
            thing.SplitOff(take).Destroy(DestroyMode.Vanish);
            taken += take;
        }
        foreach (TankCompartment compartment in TanksOf(depot, fuel).ToList())
        {
            if (taken >= most) break;
            int take = System.Math.Min(most - taken, (int)compartment.amount);
            if (take <= 0) continue;
            compartment.amount -= take;
            if (!compartment.HasLoad) compartment.Clear();
            taken += take;
        }
        return taken;
    }

    /// <summary>The fuel a vehicle could take from a depot now: the room in its tank, and what the depot holds of its fuel.</summary>
    static int WantedFrom(PhobosSlingWreck depot, VehiclePawn vehicle)
    {
        if (!CargoRefuel.Burns(vehicle)) return 0;
        CompFueledTravel fuel = vehicle.CompFueledTravel;
        int room = (int)(fuel.FuelCapacity - fuel.Fuel);
        return room <= 0 ? 0 : System.Math.Min(room, FuelUnits(depot, fuel.Props.fuelType));
    }

    /// <summary>Why the convoy cannot refuel from a depot now, or null.</summary>
    public static string? RefuelRefusal(VehicleCaravan caravan, PhobosSlingWreck? depot)
    {
        if (depot == null) return "PhobosDepot_NoDepotHere".Translate().Resolve();
        if (!caravan.VehiclesListForReading.Any(v => WantedFrom(depot, v) > 0)) return "PhobosDepot_NothingToPour".Translate().Resolve();
        return null;
    }

    /// <summary>Fills every vehicle of the convoy it can from the depot, the emptiest first. A moving convoy is held up by the pouring, as
    /// refuelling from its own cargo holds it up; a standing one pays no time (a simplification).</summary>
    public static void RefuelConvoy(VehicleCaravan caravan)
    {
        PhobosSlingWreck? depot = At(caravan.Tile);
        if (RefuelRefusal(caravan, depot) is { } refusal)
        {
            Messages.Message(refusal, caravan, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        PhobosVehicleRulesDef rules = PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();
        double stopTicks = 0;
        int total = 0;
        foreach (VehiclePawn vehicle in caravan.VehiclesListForReading.OrderBy(v => v.CompFueledTravel?.FuelPercent ?? 1f))
        {
            int want = WantedFrom(depot!, vehicle);
            if (want <= 0) continue;
            CompFueledTravel fuel = vehicle.CompFueledTravel;
            int got = Draw(depot!, fuel.Props.fuelType, want);
            if (got <= 0) continue;
            fuel.Refuel(got);
            total += got;
            stopTicks += CargoFuelRule.StopTicks(got, fuel.FuelCapacity, rules.refuelStopHoursPerTank, GenDate.TicksPerHour);
        }
        if (caravan.vehiclePather is { Moving: true } pather && stopTicks > 0) pather.nextTileCostLeft += (float)stopTicks;
        Messages.Message("PhobosDepot_Refuelled".Translate(caravan.LabelShort.Named("CONVOY"), total.Named("AMOUNT")).Resolve(), caravan,
            MessageTypeDefOf.TaskCompletion, historical: false);
        if (Seen.Wants("vehicles.depot", caravan.VehiclesListForReading.FirstOrDefault(), "refuel-convoy"))
        {
            Seen.Note("vehicles.depot", caravan.VehiclesListForReading.FirstOrDefault(), total + " units", "refuel-convoy");
        }
    }

    /// <summary>A depot of the player's within the aircraft's stopping distance of this tile that holds its fuel and can be landed at, or null.</summary>
    public static PhobosSlingWreck? StopFor(VehiclePawn aircraft, PlanetTile below)
    {
        if (!below.Valid || !CargoRefuel.Burns(aircraft)) return null;
        ThingDef fuel = aircraft.CompFueledTravel.Props.fuelType;
        PhobosSlingWreck? best = null;
        float bestDistance = float.MaxValue;
        foreach (PhobosSlingWreck depot in All())
        {
            if (depot.Tile.Layer != below.Layer) continue;
            float distance = Find.WorldGrid.ApproxDistanceInTiles(below, depot.Tile);
            if (distance > AircraftStopTiles || distance >= bestDistance || FuelUnits(depot, fuel) <= 0) continue;
            if (CargoRefuel.NoLandingReason(depot.Tile) != null) continue;
            best = depot;
            bestDistance = distance;
        }
        return best;
    }

    /// <summary>Whether any depot of the player's holds this fuel: an aircraft's refuel switch is offered for it.</summary>
    public static bool AnyHolding(ThingDef fuel) => All().Any(d => FuelUnits(d, fuel) > 0);

    // ---- Reading it (DP5) ----

    /// <summary>The kinds of fuel a depot holds anything of: in crates, in its vehicles' cargo, in their tanks.</summary>
    public static IEnumerable<ThingDef> FuelKinds(PhobosSlingWreck depot)
    {
        var kinds = new HashSet<ThingDef>();
        foreach (Thing thing in depot.Items) if (IsFuel(thing.def)) kinds.Add(thing.def);
        foreach (VehiclePawn vehicle in depot.Vehicles)
        {
            if (vehicle.inventory?.innerContainer is { } hold) foreach (Thing thing in hold) if (IsFuel(thing.def)) kinds.Add(thing.def);
            if (vehicle.GetComp<CompPhobosTank>() is { } tank)
            {
                foreach (TankCompartment compartment in tank.Compartments)
                {
                    if (!compartment.HasLoad || compartment.contents == null) continue;
                    foreach (TankPort port in compartment.contents.ports) if (port is Port_VehicleFuel { thing: { } made }) kinds.Add(made);
                }
            }
        }
        return kinds.OrderBy(d => d.label);
    }

    /// <summary>Every unit of fuel a depot holds, of every kind.</summary>
    public static int TotalFuel(PhobosSlingWreck depot) => FuelKinds(depot).Sum(fuel => FuelUnits(depot, fuel));

    /// <summary>The depot's line on the world map: its fuel by kind, how many vehicles and crates, and what time is doing to it.</summary>
    public static string Summary(PhobosSlingWreck depot)
    {
        var lines = new List<string>();
        foreach (ThingDef fuel in FuelKinds(depot))
        {
            int units = FuelUnits(depot, fuel);
            if (units > 0) lines.Add("PhobosDepot_FuelLine".Translate(units.Named("AMOUNT"), fuel.label.Named("FUEL")).Resolve());
        }
        if (lines.Count == 0) lines.Add("PhobosDepot_NoFuel".Translate().Resolve());
        lines.Add("PhobosDepot_HeldLine".Translate(depot.Vehicles.Count().Named("VEHICLES"), depot.Items.Sum(t => t.stackCount).Named("ITEMS")).Resolve());
        lines.AddRange(DepotPressures.Lines(depot));
        return string.Join("\n", lines);
    }

    /// <summary>Whether this is a fuel a vehicle of the game burns, read from the vehicles' own Defs (never by name).</summary>
    public static bool IsFuel(ThingDef def) => FuelDefs.Contains(def);

    static HashSet<ThingDef>? fuelDefs;
    static HashSet<ThingDef> FuelDefs => fuelDefs ??= new HashSet<ThingDef>(DefDatabase<VehicleDef>.AllDefsListForReading
        .Select(d => d.GetCompProperties<CompProperties_FueledTravel>()?.fuelType).Where(f => f != null)!);
}

[DefOf]
public static class DepotDefOf
{
    public static Phobos.Framework.Persistence.PhobosLogEntryDef PhobosLog_LeftAtDepot = null!;

    static DepotDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(DepotDefOf));
}
