using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Vehicles;
using Vehicles.World;
using Verse;
using Verse.AI;

namespace Phobos.VehicleSystems;

/// <summary>
/// On a vehicle's Def: it has a flat deck a tent can stand on (Framework 0.217.0; docs/development/vehicle-accessories.md, "Built: tents for
/// every vehicle"; the owner, 22 Nov 2026). Such a vehicle may be fitted with a deck tent, which is a walk-in room on the game's own pocket
/// maps (the owner's idea: "use pocket rooms that we have already coded"): it stands on the vehicle, so it takes no ground. Other authors
/// mark their own open-bed trucks with it in XML.
/// </summary>
public class PhobosFlatDeckExtension : DefModExtension
{
    /// <summary>The tent's inside, as a cabin layout: <c>B</c> over <c>b</c> a bed, <c>E</c> the flap, <c>.</c> floor. Two beds by default.</summary>
    public List<string> rows = new() { "B.B", "b.b", ".E." };
}

/// <summary>
/// Deck tents (Framework 0.217.0): the parts of a walk-in cabin that differ when the cabin is a tent on a deck. It is canvas, so it is as
/// warm as the air where the vehicle stands and a little more; its people are woken and put outside when danger comes, since nobody on the
/// map can see them; and its sleepers, chosen on the vehicle, walk in at bedtime by themselves.
/// </summary>
public static class DeckTents
{
    /// <summary>The ability the deck tent fitting grants.</summary>
    public const string Ability = "DeckTent";

    /// <summary>
    /// The roomy deck tent's own ability (Framework 0.234.0; the owner's choice of 22 Nov 2026: one kit, two ways to fit it, roomy or light).
    /// Roomy: a little warmer and comfier beds, heavier and slower to fit; light: the tent as it was.
    /// </summary>
    public const string RoomyAbility = "DeckTentRoomy";

    /// <summary>Whether the vehicle's deck tent is the roomy one.</summary>
    public static bool Roomy(VehiclePawn vehicle) => vehicle.GetComp<CompPhobosVehicleSystems>() is { Usable: true } systems && systems.Fittings.Grants(RoomyAbility);

    /// <summary>
    /// The open cabins and deck tents, by their map: read by the temperature answer, which every map asks often. Derived, never saved. Every
    /// cabin since Framework 0.230.0, so a walk-in cabin can take its vehicle's cab temperature.
    /// </summary>
    static readonly Dictionary<Map, CompPhobosVehicleCabin> open = new();
    static Game? game;

    static void Check()
    {
        if (game == Current.Game) return;
        open.Clear();
        game = Current.Game;
    }

    /// <summary>How many cabins and deck tents are open: the temperature answer does nothing while there are none.</summary>
    public static int OpenCount => open.Count;

    internal static void Note(CompPhobosVehicleCabin comp, Map? map, bool isOpen)
    {
        Check();
        if (map == null) return;
        if (isOpen) open[map] = comp;
        else open.Remove(map);
    }

    /// <summary>The cabin or deck tent whose inside this map is, or null.</summary>
    public static CompPhobosVehicleCabin? Of(Map map)
    {
        Check();
        return open.TryGetValue(map, out CompPhobosVehicleCabin? comp) ? comp : null;
    }

    /// <summary>
    /// The temperature inside a deck tent: the air where its vehicle stands (the cell on a map, so a truck in a warm shed has a warm tent; the
    /// tile off one), raised a little towards a comfortable figure by the people and the canvas, never past it. Authored figures, the rules Def's.
    /// </summary>
    public static float Temperature(CompPhobosVehicleCabin comp)
    {
        PhobosVehicleRulesDef rules = PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();
        float warmer = rules.deckTentWarmerBy + (Roomy(comp.Vehicle) ? rules.deckTentRoomyWarmerBy : 0f);
        return Phobos.Framework.Vehicles.Rules.CampLayoutRule.TentTemperature(comp.Vehicle.AmbientTemperature, warmer, rules.deckTentWarmTo + (Roomy(comp.Vehicle) ? rules.deckTentRoomyWarmerBy : 0f));
    }

    /// <summary>Whether danger has come to the vehicle's map: a hostile threat, or the vehicle on fire.</summary>
    public static bool InDanger(VehiclePawn vehicle) =>
        vehicle.Spawned && (Phobos.Framework.Environment.AttachedFires.OnFire(vehicle) || GenHostility.AnyHostileActiveThreatToPlayer(vehicle.Map, countDormantPawnsAsHostile: false));

    /// <summary>The rare look of an open deck tent: with danger outside, everyone in it is woken and sent out, and told once why.</summary>
    internal static void RareLook(CompPhobosVehicleCabin comp, Map cabin, Building_PhobosCabinWayOut wayOut, List<Pawn> inside)
    {
        VehiclePawn vehicle = comp.Vehicle;
        if (!InDanger(vehicle)) return;
        bool anyone = false;
        foreach (Pawn pawn in inside)
        {
            if (pawn.Faction != Faction.OfPlayer || pawn.Downed || pawn.InMentalState) continue;
            if (pawn.CurJobDef == VehicleCabinDefOf.PhobosVehicleSystems_LeaveCabin) continue;
            RestUtility.WakeUp(pawn);
            pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(VehicleCabinDefOf.PhobosVehicleSystems_LeaveCabin, wayOut), JobTag.Misc);
            anyone = true;
        }
        if (!anyone) return;
        Messages.Message("PhobosDeckTent_WokenDanger".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve(), vehicle, MessageTypeDefOf.NegativeEvent, historical: false);
        VehicleStory.Of(vehicle, VehicleStoryDefOf.PhobosLog_TentWoken);
        if (Seen.Wants("vehicles.deck-tent-danger", vehicle)) Seen.Note("vehicles.deck-tent-danger", vehicle, inside.Count + " inside");
    }

    /// <summary>
    /// The vehicle has gone into a caravan with people in its deck tent (Framework 0.219.0): forming a caravan takes the vehicle as it stands,
    /// and a room on the deck is no part of a caravan, so nobody would feed them on the road. Each comes out and rides aboard instead (a
    /// passenger seat, else the cargo, as Vehicle Framework puts the downed), where the caravan counts and feeds them; what was left in the tent
    /// goes into the cargo, and the room is closed.
    /// </summary>
    internal static void IntoCaravan(CompPhobosVehicleCabin comp, Map cabin, VehicleCaravan caravan)
    {
        VehiclePawn vehicle = comp.Vehicle;
        var moved = new List<string>();
        foreach (Pawn pawn in cabin.mapPawns.AllPawnsSpawned.ToList())
        {
            if (pawn.Dead) continue;
            pawn.DeSpawn();
            if (!vehicle.TryAddPawn(pawn) && !vehicle.inventory.innerContainer.TryAdd(pawn))
            {
                // No seat and no room: the caravan takes them as it takes anyone walking with it.
                caravan.AddPawn(pawn, addCarriedPawnToWorldPawnsIfAny: true);
            }
            moved.Add(pawn.LabelShort);
        }
        foreach (Thing thing in cabin.listerThings.AllThings.Where(t => t.def.category == ThingCategory.Item).ToList())
        {
            thing.DeSpawn();
            if (!vehicle.inventory.innerContainer.TryAdd(thing)) thing.Destroy();
        }
        VehicleCabins.CloseMap(cabin);
        comp.SetCabin(null);
        caravan.RecacheVehiclesOrConvertCaravan();
        if (moved.Count == 0) return;
        Messages.Message("PhobosDeckTent_IntoCaravan".Translate(string.Join(", ", moved).Named("NAMES"), vehicle.LabelShort.Named("VEHICLE")).Resolve(),
            caravan, MessageTypeDefOf.NeutralEvent, historical: false);
        if (Seen.Wants("vehicles.deck-tent-caravan", vehicle)) Seen.Note("vehicles.deck-tent-caravan", vehicle, moved.Count + " aboard");
    }

    /// <summary>Why this colonist cannot go to the deck tent to sleep now, or null. Read at bedtime.</summary>
    public static string? BedtimeRefusal(CompPhobosCamp camp, Pawn pawn)
    {
        VehiclePawn vehicle = camp.Vehicle;
        NamedArgument name = vehicle.LabelShort.Named("VEHICLE");
        if (!camp.DeckTentFitted || !camp.IsTentSleeper(pawn)) return "PhobosBunk_NotTheirs".Translate(name).Resolve();
        if (!vehicle.Spawned || vehicle.Map != pawn.MapHeld) return "PhobosCamp_NotHere".Translate(name).Resolve();
        if (vehicle.vehiclePather is { Moving: true } || vehicle.Drafted) return "PhobosBunk_Running".Translate(name).Resolve();
        if (InDanger(vehicle)) return "PhobosBunk_Danger".Translate().Resolve();
        if (!VehicleCabins.Offered(vehicle)) return "PhobosCamp_NotHere".Translate(name).Resolve();
        if (VehicleCabins.EnterRefusal(vehicle) is { } refusal) return refusal;
        if (!pawn.CanReach(vehicle, PathEndMode.Touch, Danger.Some)) return "PhobosCabin_NoPath".Translate().Resolve();
        return null;
    }
}

/// <summary>
/// Sleepers chosen for a vehicle's deck tent, kept by the vehicles as they are assigned, so the bedtime check reads a short list (as the cab
/// bunk's is). Emptied when another game is loaded.
/// </summary>
public static class TentSleepers
{
    static readonly Dictionary<Pawn, CompPhobosCamp> assigned = new();
    static Game? game;

    static void Check()
    {
        if (game == Current.Game) return;
        assigned.Clear();
        game = Current.Game;
    }

    internal static void Note(CompPhobosCamp camp, Pawn pawn, bool sleeps)
    {
        Check();
        if (sleeps) assigned[pawn] = camp;
        else if (assigned.TryGetValue(pawn, out CompPhobosCamp? was) && was == camp) assigned.Remove(pawn);
    }

    /// <summary>The vehicle whose deck tent this colonist sleeps in, or null.</summary>
    public static CompPhobosCamp? TentOf(Pawn pawn)
    {
        Check();
        return assigned.TryGetValue(pawn, out CompPhobosCamp? camp) && camp.IsTentSleeper(pawn) ? camp : null;
    }
}

/// <summary>
/// A bed in a deck tent or a walk-in cabin is slept in, never owned (fault-read 25 Nov 2026, C3). The game's lie-down job claims any
/// non-medical bed it uses, and claiming one gives up the bed the colonist owns: a tent sleeper lost their own bed at home on the first
/// night, and owned nothing once the tent closed and its bed went. A prefix that skips the original, recorded here as the rules ask:
/// there is no extension point between the job and the claim, and the game's own toil ignores the answer, so skipping changes nothing
/// else. The colonist still sleeps in the tent's bed, as anyone sleeps in a bed they do not own.
/// </summary>
[HarmonyPatch(typeof(Pawn_Ownership), nameof(Pawn_Ownership.ClaimBedIfNonMedical))]
public static class CabinBedsUnclaimed
{
    public static bool Prefix(Building_Bed newBed, ref bool __result)
    {
        if (newBed?.Map is not { } map || DeckTents.Of(map) == null) return true;
        __result = false;
        // Seen in play (Framework 0.298.0, row 510): a tent or cabin bed slept in without being claimed, so the bed at home stays theirs.
        Seen.Note("vehicles.tent-bed-kept", newBed);
        return false;
    }
}

/// <summary>
/// At bedtime, a colonist chosen for a deck tent walks to its vehicle and goes in, instead of to the bed the game chose on the map; inside,
/// the game's own rest sends them to one of the tent's beds. The game decides when to sleep, as for the cab bunk; this postfix only changes
/// where, only when the tent can take them now, and never for someone who should rest for their health. Someone already inside is left to
/// the bed the game found there.
/// </summary>
[HarmonyPatch(typeof(JobGiver_GetRest), "TryGiveJob")]
public static class TentBedtime
{
    public static void Postfix(Pawn pawn, ref Job? __result)
    {
        if (__result == null || pawn.Faction != Faction.OfPlayer || TentSleepers.TentOf(pawn) is not { } camp) return;
        if (pawn.MapHeld != camp.Vehicle.MapHeld || HealthAIUtility.ShouldSeekMedicalRest(pawn)) return;
        if (DeckTents.BedtimeRefusal(camp, pawn) != null) return;
        __result = JobMaker.MakeJob(VehicleCabinDefOf.PhobosVehicleSystems_EnterCabin, camp.Vehicle);
        if (Seen.Wants("vehicles.deck-tent-bedtime", camp.Vehicle)) Seen.Note("vehicles.deck-tent-bedtime", camp.Vehicle, pawn.LabelShort);
    }
}

/// <summary>
/// The air in a deck tent and a walk-in cabin. The game gives a pocket map one fixed temperature from its Def (21 degrees C for our cabins),
/// and it has no per-map figure to set instead. This postfix on the map's own outdoor temperature answers for an open deck tent's map: the
/// air where its vehicle stands, a little warmer; and since Framework 0.230.0 for a walk-in cabin's: the temperature of its vehicle's cab, as
/// the vehicle's Cabin system works it out (heater, air conditioning, the engine off, the weather), where the vehicle has one. A vehicle with
/// no cab climate of ours keeps the fixed figure. With no cabin open it returns at once.
/// </summary>
[HarmonyPatch(typeof(MapTemperature), nameof(MapTemperature.OutdoorTemp), MethodType.Getter)]
public static class DeckTentAir
{
    public static void Postfix(Map ___map, ref float __result)
    {
        if (DeckTents.OpenCount == 0 || ___map == null) return;
        if (DeckTents.Of(___map) is not { } comp) return;
        if (comp.Props.deckTent)
        {
            __result = DeckTents.Temperature(comp);
            return;
        }
        if (CabClimate.TryGet(comp.Vehicle, out float cab))
        {
            if (Seen.Wants("vehicles.cabin-air", comp.Vehicle)) Seen.Note("vehicles.cabin-air", comp.Vehicle, cab.ToString("0") + " C");
            __result = cab;
        }
    }
}
