using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Vehicles;
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

    /// <summary>The open deck tents, by their map: read by the temperature answer, which every map asks often. Derived, never saved.</summary>
    static readonly Dictionary<Map, CompPhobosVehicleCabin> open = new();
    static Game? game;

    static void Check()
    {
        if (game == Current.Game) return;
        open.Clear();
        game = Current.Game;
    }

    /// <summary>How many deck tents are open: the temperature answer does nothing while there are none.</summary>
    public static int OpenCount => open.Count;

    internal static void Note(CompPhobosVehicleCabin comp, Map? map, bool isOpen)
    {
        Check();
        if (map == null || !comp.Props.deckTent) return;
        if (isOpen) open[map] = comp;
        else open.Remove(map);
    }

    /// <summary>The deck tent whose inside this map is, or null.</summary>
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
        return Phobos.Framework.Vehicles.Rules.CampLayoutRule.TentTemperature(comp.Vehicle.AmbientTemperature, rules.deckTentWarmerBy, rules.deckTentWarmTo);
    }

    /// <summary>Whether danger has come to the vehicle's map: a hostile threat, or the vehicle on fire.</summary>
    public static bool InDanger(VehiclePawn vehicle) =>
        vehicle.Spawned && (vehicle.IsBurning() || GenHostility.AnyHostileActiveThreatToPlayer(vehicle.Map, countDormantPawnsAsHostile: false));

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
        if (Seen.Wants("vehicles.deck-tent-danger", vehicle)) Seen.Note("vehicles.deck-tent-danger", vehicle, inside.Count + " inside");
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
/// The air in a deck tent. The game gives a pocket map one fixed temperature from its Def, which suits a vehicle's cabin (held at 21 degrees
/// C) and not a canvas tent on a deck, and it has no per-map figure to set instead. This postfix on the map's own outdoor temperature answers
/// for an open deck tent's map only: the air where its vehicle stands, a little warmer. With no deck tent open it returns at once.
/// </summary>
[HarmonyPatch(typeof(MapTemperature), nameof(MapTemperature.OutdoorTemp), MethodType.Getter)]
public static class DeckTentAir
{
    public static void Postfix(Map ___map, ref float __result)
    {
        if (DeckTents.OpenCount == 0 || ___map == null) return;
        if (DeckTents.Of(___map) is { } comp) __result = DeckTents.Temperature(comp);
    }
}
