using System.Collections.Generic;
using HarmonyLib;
using Phobos.Framework.Persistence;
using Phobos.Framework.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The hours each of the player's people has spent driving or flying this one vehicle, and whether it is lost (Framework 0.190.0;
/// docs/development/vehicle-emergent-play.md, EM12). Saved with the vehicle, keyed by each person's load ID, so a person who has died or
/// left keeps their place in its history without the save holding a reference to them.
/// </summary>
public sealed class VehicleDriverHours
{
    public Dictionary<string, float> hours = new();

    /// <summary>When it was lost on the world map (a dropped sling load, a lost convoy), or -1. Cleared when it is brought home.</summary>
    public int lostSince = -1;

    public void Add(Pawn pawn, float more)
    {
        string key = pawn.GetUniqueLoadID();
        hours[key] = (hours.TryGetValue(key, out float value) ? value : 0f) + more;
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref hours, "PhobosVehicleSystems_DriverHours", LookMode.Value, LookMode.Value);
        Scribe_Values.Look(ref lostSince, "PhobosVehicleSystems_LostSince", -1);
        hours ??= new Dictionary<string, float>();
    }
}

/// <summary>
/// Hours on type (Framework 0.190.0; docs/development/vehicle-emergent-play.md, EM11). While one of the player's vehicles moves, everyone
/// in a driving seat gains practice at that kind of vehicle (the core's <see cref="Practice"/>, by its defName) and hours at this one,
/// counted on the rare look the vehicle already gets, on a map or away. A practised driver is easier on the brakes and tyres (the driver
/// system's driverExperience quantity, read by their wear) and a practised pilot puts an aircraft down more gently. Never faster: the
/// owner's driver rule keeps speed to fitness. Other factions' drivers gain nothing (the light tier keeps no history).
/// </summary>
public static class VehiclePractice
{
    /// <summary>A hard or rolling landing's wear at full practice, as a share of a new pilot's (authored).</summary>
    public const float LandingBestFactor = 0.6f;

    static readonly float HoursPerLook = GenTicks.TickRareInterval / (float)GenDate.TicksPerHour;

    /// <summary>The kind of thing practice is kept for: one kind of vehicle.</summary>
    public static string KindOf(VehiclePawn vehicle) => "vehicle/" + vehicle.def.defName;

    /// <summary>The rare look: a moving vehicle's drivers gain an interval's practice.</summary>
    public static void Look(CompPhobosVehicleSystems comp)
    {
        VehiclePawn vehicle = comp.Vehicle;
        if (vehicle.Faction != Faction.OfPlayer || !comp.MovingNow) return;
        string kind = KindOf(vehicle);
        foreach (Pawn driver in VehicleStory.Drivers(vehicle))
        {
            if (driver.Faction != Faction.OfPlayer) continue;
            Practice.Add(driver, kind, HoursPerLook);
            comp.Practised.Add(driver, HoursPerLook);
            driver.records?.AddTo(VehiclePracticeDefOf.PhobosRecord_HoursDriving, HoursPerLook);
            if (Seen.Wants("vehicles.practice", vehicle, "hours"))
            {
                Seen.Note("vehicles.practice", vehicle, driver.LabelShort + ", " + Practice.Hours(driver, kind).ToString("0.#") + " h on type", "hours",
                    new[] { new KeyValuePair<string, double>("hours", Practice.Hours(driver, kind)) });
            }
        }
    }

    /// <summary>The best practice share among those in a driving seat now, 0 with nobody there or for another faction's vehicle.</summary>
    public static float Share(VehiclePawn vehicle)
    {
        if (vehicle.Faction != Faction.OfPlayer) return 0f;
        string kind = KindOf(vehicle);
        float best = 0f;
        foreach (Pawn driver in VehicleStory.Drivers(vehicle))
        {
            if (driver.Faction == Faction.OfPlayer) best = UnityEngine.Mathf.Max(best, Practice.Share(driver, kind));
        }
        return best;
    }

    /// <summary>What a landing's wear is multiplied by for the pilot at the controls now.</summary>
    public static float LandingFactor(VehiclePawn vehicle)
    {
        float share = Share(vehicle);
        if (share <= 0f) return 1f;
        float factor = (float)PracticeRule.Factor(share, LandingBestFactor);
        if (Seen.Wants("vehicles.practice", vehicle, "landing"))
        {
            Seen.Note("vehicles.practice", vehicle, "landing wear x" + factor.ToString("0.00"), "landing", new[] { new KeyValuePair<string, double>("factor", factor) });
        }
        return factor;
    }

    /// <summary>This vehicle's regular driver, alive and one of the player's free colonists, or null (EM12).</summary>
    public static Pawn? RegularDriver(CompPhobosVehicleSystems comp)
    {
        string? id = PracticeRule.Regular(comp.Practised.hours);
        if (id == null) return null;
        foreach (Pawn pawn in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists)
        {
            if (pawn.GetUniqueLoadID() == id) return pawn;
        }
        return null;
    }
}

/// <summary>
/// "Their" vehicle (Framework 0.190.0; docs/development/vehicle-emergent-play.md, EM12): the colonist who has driven a vehicle most, with
/// enough hours at it to call it theirs, feels its loss and its return. Destroyed, lost on the world map (a dropped sling load, a lost
/// convoy) and brought home again are memories, each naming the vehicle; nobody else minds. Mood figures are authored, small beside the
/// game's own (a good meal +5, sleeping on the ground -4). A vehicle taken by another faction would count as lost, but nothing in the
/// game or Vehicle Framework takes one today.
/// </summary>
public static class OwnVehicles
{
    /// <summary>The vehicle is gone from the player's hands: destroyed, or lost on the world map.</summary>
    public static void Lost(VehiclePawn vehicle, bool destroyed)
    {
        if (vehicle.Faction != Faction.OfPlayer || vehicle.GetComp<CompPhobosVehicleSystems>() is not { Usable: true } comp) return;
        if (!destroyed) comp.Practised.lostSince = Find.TickManager.TicksGame;
        if (VehiclePractice.RegularDriver(comp) is not { } regular) return;
        Give(regular, destroyed ? VehiclePracticeDefOf.PhobosVehicleSystems_OwnVehicleDestroyed : VehiclePracticeDefOf.PhobosVehicleSystems_OwnVehicleLost,
            vehicle.LabelShort);
        string variant = destroyed ? "destroyed" : "lost";
        if (Seen.Wants("vehicles.own-vehicle", vehicle, variant)) Seen.Note("vehicles.own-vehicle", vehicle, regular.LabelShort, variant);
    }

    /// <summary>The rare look: a lost vehicle standing on one of the player's home maps again is home.</summary>
    public static void LookHome(CompPhobosVehicleSystems comp)
    {
        if (comp.Practised.lostSince < 0) return;
        VehiclePawn vehicle = comp.Vehicle;
        if (!vehicle.Spawned || vehicle.Map is not { IsPlayerHome: true } || vehicle.Faction != Faction.OfPlayer) return;
        comp.Practised.lostSince = -1;
        VehicleStory.Log(comp, VehiclePracticeDefOf.PhobosLog_BroughtHome);
        Pawn? regular = VehiclePractice.RegularDriver(comp);
        if (regular != null) Give(regular, VehiclePracticeDefOf.PhobosVehicleSystems_OwnVehicleHome, vehicle.LabelShort);
        if (Seen.Wants("vehicles.own-vehicle", vehicle, "home")) Seen.Note("vehicles.own-vehicle", vehicle, regular?.LabelShort, "home");
    }

    static void Give(Pawn pawn, ThoughtDef def, string vehicle)
    {
        if (pawn.needs?.mood?.thoughts?.memories == null) return;
        if (ThoughtMaker.MakeThought(def) is not Thought_PhobosOwnVehicle thought) return;
        thought.vehicle = vehicle;
        pawn.needs.mood.thoughts.memories.TryGainMemory(thought);
    }
}

/// <summary>A memory about one vehicle, naming it: its stage's label and description take {VEHICLE}. Memories of different vehicles stay
/// apart.</summary>
public class Thought_PhobosOwnVehicle : Thought_Memory
{
    public string vehicle = "";

    public override string LabelCap => CurStage.label.Formatted(vehicle.Named("VEHICLE")).Resolve().CapitalizeFirst();

    public override string Description => CurStage.description.Formatted(pawn.Named("PAWN"), vehicle.Named("VEHICLE")).Resolve();

    public override bool GroupsWith(Thought other) => other is Thought_PhobosOwnVehicle same && same.def == def && same.vehicle == vehicle;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref vehicle, "PhobosVehicleSystems_Vehicle", "");
    }
}

/// <summary>A player's vehicle being destroyed is felt by its regular driver (Framework 0.190.0, EM12). A prefix that only reads and
/// never skips Vehicle Framework's destruction; it runs while the vehicle still knows who drove it.</summary>
[HarmonyPatch(typeof(VehiclePawn), nameof(VehiclePawn.Kill), new[] { typeof(DamageInfo?), typeof(DestroyMode), typeof(bool) })]
public static class OwnVehicleOnKill
{
    public static void Prefix(VehiclePawn __instance, DestroyMode destroyMode)
    {
        if (destroyMode != DestroyMode.KillFinalize || __instance.Dead || __instance.Faction != Faction.OfPlayer) return;
        OwnVehicles.Lost(__instance, destroyed: true);
    }
}

[DefOf]
public static class VehiclePracticeDefOf
{
    public static RecordDef PhobosRecord_HoursDriving = null!;
    public static ThoughtDef PhobosVehicleSystems_OwnVehicleDestroyed = null!;
    public static ThoughtDef PhobosVehicleSystems_OwnVehicleLost = null!;
    public static ThoughtDef PhobosVehicleSystems_OwnVehicleHome = null!;
    public static Phobos.Framework.Persistence.PhobosLogEntryDef PhobosLog_BroughtHome = null!;

    static VehiclePracticeDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(VehiclePracticeDefOf));
}
