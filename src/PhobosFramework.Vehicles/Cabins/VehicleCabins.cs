using System.Collections.Generic;
using System.Linq;
using Phobos.Framework;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Vehicles.World;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Phobos.VehicleSystems;

/// <summary>
/// Walk-in cabins (Framework, vehicle cabins, 22 Nov 2026; the owner's request after the UH-60M Black Hawk's; docs/development/vehicle-cabins.md).
/// A vehicle with a cabin comp has a small pocket map, the game's own, made when the first person goes in and removed once nobody is inside and
/// the vehicle stands on a map. Its way out always leads to wherever the vehicle now is; the game will not close the map the vehicle left while
/// people are in the cabin. Written from the game's own portal code. Behind the "Walk-in cabins" setting (on). The player's people only.
/// </summary>
public static class VehicleCabins
{
    /// <summary>How long going in or out takes, as through the game's own portals (90 ticks there; a vehicle's door is quicker).</summary>
    public const int PassTicks = 60;

    public static bool Enabled => VehicleSystemsMod.Settings.vehicleCabins;

    public static CompPhobosVehicleCabin? Of(VehiclePawn? vehicle) => vehicle?.GetComp<CompPhobosVehicleCabin>();

    /// <summary>Whether to offer the cabin at all on this vehicle: it has one, the setting is on, and it is the player's.</summary>
    public static bool Offered(VehiclePawn vehicle) => Enabled && Of(vehicle) is { HasLayout: true } && vehicle.Faction == Faction.OfPlayer && !vehicle.Dead;

    /// <summary>Why nobody can go in now, or null. <paramref name="people"/>: how many would go in together (two when one carries another).</summary>
    public static string? EnterRefusal(VehiclePawn vehicle, int people = 1)
    {
        NamedArgument name = vehicle.LabelShort.Named("VEHICLE");
        if (!vehicle.Spawned) return "PhobosCabin_NotHere".Translate(name).Resolve();
        if (OnMapFlight.Airborne(vehicle)) return "PhobosCabin_InTheAir".Translate(name).Resolve();
        if (Room(vehicle) < people) return "PhobosCabin_Full".Translate(name).Resolve();
        return null;
    }

    // ---- Who is in the cabin, and how many more it takes (Framework 0.211.1; owner, 22 Nov 2026: seats and cabin share one limit) ----

    static readonly List<Pawn> none = new();

    /// <summary>Everyone in the vehicle's cabin now: aboard as much as anyone seated, for its weight and its oxygen.</summary>
    public static IReadOnlyList<Pawn> Occupants(VehiclePawn? vehicle) => Of(vehicle)?.Cabin is { } cabin ? cabin.mapPawns.AllPawnsSpawned : none;

    /// <summary>The vehicle's passenger seats (seats that neither drive nor man a weapon) and how many are taken.</summary>
    public static void PassengerSeats(VehiclePawn vehicle, out int seats, out int seated)
    {
        seats = seated = 0;
        if (vehicle.handlers == null) return;
        foreach (VehicleRoleHandler handler in vehicle.handlers)
        {
            if (handler.role == null || handler.role.handlingTypes != HandlingType.None) continue;
            seats += handler.role.Slots;
            seated += handler.thingOwner.Count;
        }
    }

    /// <summary>How many more people may go into the cabin: its own places, and the passenger seats it shares with those seated.</summary>
    public static int Room(VehiclePawn vehicle)
    {
        if (Of(vehicle) is not { } comp) return 0;
        PassengerSeats(vehicle, out int seats, out int seated);
        Phobos.Framework.Vehicles.Rules.CabinPlan plan = comp.Layout.Plan;
        int places = Phobos.Framework.Vehicles.Rules.CabinLayoutRule.Places(plan);
        // A tent on the deck is no part of the cab: it holds its own places, whatever the seats hold (Framework 0.217.0).
        if (comp.Props.deckTent) return System.Math.Max(0, places - Occupants(vehicle).Count);
        return Phobos.Framework.Vehicles.Rules.CabinRoomRule.CabinRoom(seats, seated, places, Occupants(vehicle).Count);
    }

    /// <summary>Why nobody can leave now, or null: the vehicle must stand on a map, on the ground.</summary>
    public static string? LeaveRefusal(VehiclePawn? vehicle)
    {
        if (vehicle == null || vehicle.Destroyed) return "PhobosCabin_NoVehicle".Translate().Resolve();
        NamedArgument name = vehicle.LabelShort.Named("VEHICLE");
        if (!vehicle.Spawned) return "PhobosCabin_LeaveInFlight".Translate(name).Resolve();
        if (OnMapFlight.Airborne(vehicle)) return "PhobosCabin_LeaveInTheAir".Translate(name).Resolve();
        return null;
    }

    /// <summary>The cabin's map, made now if it is not open.</summary>
    public static Map? Open(CompPhobosVehicleCabin comp)
    {
        if (comp.Cabin is { } open) return open;
        VehiclePawn vehicle = comp.Vehicle;
        if (!vehicle.Spawned) return null;
        CabinLayoutSpec layout = comp.Layout;
        if (!layout.Plan.Valid) return null;
        CabinGeneration.Generating = comp;
        CabinGeneration.Plan = layout.Plan;
        try
        {
            Map map = PocketMapUtility.GeneratePocketMap(new IntVec3(layout.Plan.Width, 1, layout.Plan.Length), VehicleCabinDefOf.PhobosVehicleCabinMap,
                null, vehicle.Map);
            comp.SetCabin(map);
            // A deck tent is noted apart, so its opening is never taken for a cabin's (Framework 0.217.0).
            if (comp.Props.deckTent)
            {
                if (Seen.Wants("vehicles.deck-tent-opened", vehicle)) Seen.Note("vehicles.deck-tent-opened", vehicle, $"{layout.Plan.Width} x {layout.Plan.Length}");
            }
            else if (Seen.Wants("vehicles.cabin-opened", vehicle)) Seen.Note("vehicles.cabin-opened", vehicle, $"{layout.Plan.Width} x {layout.Plan.Length}",
                layout.whenFitted.Count == 0 ? "as built" : string.Join(" + ", layout.whenFitted.Select(f => f.defName)));
            return map;
        }
        finally
        {
            CabinGeneration.Generating = null;
            CabinGeneration.Plan = null;
        }
    }

    /// <summary>The way out inside an open cabin, or null.</summary>
    public static Building_PhobosCabinWayOut? WayOut(Map? cabin) =>
        cabin?.listerThings.ThingsOfDef(VehicleCabinDefOf.PhobosCabinWayOut).OfType<Building_PhobosCabinWayOut>().FirstOrDefault();

    /// <summary>Where someone leaving stands: a free cell beside the vehicle.</summary>
    public static IntVec3 BesideVehicle(VehiclePawn vehicle)
    {
        Map map = vehicle.Map;
        foreach (IntVec3 cell in vehicle.OccupiedRect().ExpandedBy(1).EdgeCells)
        {
            if (cell.InBounds(map) && cell.Standable(map)) return cell;
        }
        return CellFinder.StandableCellNear(vehicle.Position, map, 8f);
    }

    /// <summary>Puts a person through the door, as the game's own portals do: drafted stays drafted, ordered work is cleared, a lord is told
    /// they left. Whatever they carry comes with them. False, with a message, where there is nowhere to stand.</summary>
    public static bool Move(Pawn pawn, Map to, IntVec3 cell, Thing near)
    {
        if (!cell.IsValid || !cell.InBounds(to) || !cell.Standable(to)) cell = CellFinder.StandableCellNear(cell.IsValid ? cell : to.Center, to, 6f);
        if (!cell.IsValid)
        {
            Messages.Message("PhobosCabin_NoRoom".Translate(pawn.LabelShort.Named("PAWN")), near, MessageTypeDefOf.RejectInput, historical: false);
            return false;
        }
        bool drafted = pawn.IsPlayerControlled && pawn.Drafted;
        bool fireAtWill = pawn.drafter?.FireAtWill ?? true;
        pawn.DeSpawnOrDeselect();
        GenSpawn.Spawn(pawn, cell, to, Rot4.Random);
        if (drafted && pawn.drafter != null)
        {
            pawn.drafter.Drafted = true;
            pawn.drafter.FireAtWill = fireAtWill;
        }
        pawn.mindState.priorityWork.ClearPrioritizedWorkAndJobQueue();
        pawn.GetLord()?.Notify_PawnLost(pawn, PawnLostCondition.ExitedMap);
        return true;
    }

    /// <summary>Someone carrying a downed person has come through: they lay them on a free bed there (a litter in the cabin, a bed at home),
    /// or set them down where they stand.</summary>
    public static void SettleCarried(Pawn carrier)
    {
        if (carrier.carryTracker?.CarriedThing is not Pawn patient) return;
        Building_Bed? bed = RestUtility.FindBedFor(patient, carrier, checkSocialProperness: false);
        carrier.carryTracker.TryDropCarriedThing(carrier.Position, ThingPlaceMode.Near, out Thing _);
        if (bed == null || carrier.Drafted) return;
        Job rescue = JobMaker.MakeJob(JobDefOf.Rescue, patient, bed);
        rescue.count = 1;
        carrier.jobs.TryTakeOrderedJob(rescue, JobTag.Misc);
    }

    /// <summary>
    /// The vehicle's rare look: an open cabin nobody is in is removed once the vehicle stands on the ground, what was left in it set down beside
    /// the vehicle; and on the ground, an undrafted colonist inside with nothing to do walks out by themselves, so nobody idles there forgotten.
    /// A patient in a litter, and anyone at work, stays.
    /// </summary>
    public static void RareLook(CompPhobosVehicleCabin comp)
    {
        if (comp.Cabin is not { } cabin) return;
        VehiclePawn vehicle = comp.Vehicle;
        // A deck tent's people ride aboard once the vehicle is in a caravan (Framework 0.219.0).
        if (comp.Props.deckTent && !vehicle.Spawned && vehicle.GetVehicleCaravan() is { } caravan)
        {
            DeckTents.IntoCaravan(comp, cabin, caravan);
            return;
        }
        if (!vehicle.Spawned || OnMapFlight.Airborne(vehicle)) return;
        List<Pawn> inside = cabin.mapPawns.AllPawnsSpawned.ToList();
        if (inside.Count == 0)
        {
            Close(comp, cabin);
            return;
        }
        Building_PhobosCabinWayOut? wayOut = WayOut(cabin);
        if (wayOut == null) return;
        // A deck tent: danger outside wakes everyone in it and sends them out (nobody on the map can see them).
        if (comp.Props.deckTent) DeckTents.RareLook(comp, cabin, wayOut, inside);
        foreach (Pawn pawn in inside)
        {
            if (pawn.Faction != Faction.OfPlayer || pawn.Drafted || pawn.Downed || pawn.InBed() || pawn.InMentalState) continue;
            JobDef? doing = pawn.CurJobDef;
            if (doing != null && doing != JobDefOf.Wait_Wander && doing != JobDefOf.GotoWander && doing != JobDefOf.Wait) continue;
            Job leave = JobMaker.MakeJob(VehicleCabinDefOf.PhobosVehicleSystems_LeaveCabin, wayOut);
            pawn.jobs.TryTakeOrderedJob(leave, JobTag.Misc);
            if (Seen.Wants("vehicles.cabin-left-idle", vehicle)) Seen.Note("vehicles.cabin-left-idle", vehicle, pawn.LabelShort);
        }
    }

    static void Close(CompPhobosVehicleCabin comp, Map cabin)
    {
        VehiclePawn vehicle = comp.Vehicle;
        SetDownLeftovers(cabin, vehicle.Map, vehicle.Position);
        PocketMapUtility.DestroyPocketMap(cabin);
        comp.SetCabin(null);
        if (Seen.Wants("vehicles.cabin-closed", vehicle)) Seen.Note("vehicles.cabin-closed", vehicle);
    }

    /// <summary>Everything movable left in the cabin is set down near a cell on another map.</summary>
    static void SetDownLeftovers(Map cabin, Map to, IntVec3 near)
    {
        foreach (Thing thing in cabin.listerThings.AllThings.Where(t => t.def.category == ThingCategory.Item).ToList())
        {
            thing.DeSpawn();
            GenPlace.TryPlaceThing(thing, near, to, ThingPlaceMode.Near);
        }
    }

    /// <summary>
    /// The vehicle is gone: everyone in its cabin, and what they left there, is set down beside the wreck, or, if it was lost off any map (in
    /// flight between places), on the map the cabin was last pointed at, else the player's home. Nobody is lost with a cabin.
    /// </summary>
    public static void Evacuate(CompPhobosVehicleCabin comp, Map? previousMap, IntVec3 at)
    {
        if (comp.Cabin is not { } cabin) return;
        Map? to = previousMap is { Disposed: false } ? previousMap : (cabin.Parent as PocketMapParent)?.sourceMap;
        if (to == null || to.Disposed) to = Find.AnyPlayerHomeMap;
        if (to != null)
        {
            IntVec3 near = previousMap == to && at.IsValid ? at : to.Center;
            foreach (Pawn pawn in cabin.mapPawns.AllPawnsSpawned.ToList())
            {
                IntVec3 cell = CellFinder.StandableCellNear(near, to, 8f);
                if (!cell.IsValid) cell = CellFinder.RandomCell(to);
                pawn.DeSpawnOrDeselect();
                GenSpawn.Spawn(pawn, cell, to);
            }
            SetDownLeftovers(cabin, to, near);
            Messages.Message("PhobosCabin_Evacuated".Translate(comp.Vehicle.LabelShort.Named("VEHICLE")), new TargetInfo(near, to), MessageTypeDefOf.NegativeEvent);
        }
        PocketMapUtility.DestroyPocketMap(cabin);
        comp.SetCabin(null);
    }
}
