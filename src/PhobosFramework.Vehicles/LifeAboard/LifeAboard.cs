using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Life aboard (Framework 0.233.0; docs/development/vehicle-accessories.md, Y4 and "Built: life aboard"; hotboxing,
/// docs/development/vehicle-hotboxing.md; the owner, 13 Nov 2026: "life aboard and hotboxing as one piece", and 22 Nov 2026: build both).
/// One look, for a vehicle of the player's with people aboard, that does for riders what the game does not: nobody aboard a vehicle on a
/// map can start a job, so nobody there drank, read or ate a treat, and a caravan's recreation is abstract. Each look, everyone aboard
/// hears the stereo if one is fitted and playing; a rider who is not driving drinks from the crew water tank when thirsty, reads a book
/// from the cargo (by a reading light after dark), or has a treat such as a cup of tea; and a rider whose drug policy allows it smokes,
/// filling a closed cab with smoke the others share. A driver neither reads, drinks, eats nor smokes. Every figure is the rules Def's.
/// Cheap: an empty vehicle costs one count; the look runs every 250 ticks.
/// </summary>
public static class LifeAboard
{
    public const int LookTicks = 250;
    const float HoursPerLook = LookTicks / (float)GenDate.TicksPerHour;

    /// <summary>The stereo's and the reading light's loads, by name, on their fittings.</summary>
    public const string StereoLoad = "cabStereo";
    public const string ReadingLightLoad = "readingLight";

    /// <summary>
    /// A thirsty rider drinks from the vehicle's water (the crew water tank), registered by the mod that gives water a meaning (Dubs Bad
    /// Hygiene's tie-in). True when they drank.
    /// </summary>
    public static Func<VehiclePawn, Pawn, bool>? Drink;

    sealed class Rider
    {
        public int treatAt = -1;
        public int smokeAt = -1;
    }

    sealed class Cab
    {
        public float smoke;
        public bool rolled;
    }

    static readonly ConditionalWeakTable<Pawn, Rider> riders = new();
    static readonly ConditionalWeakTable<VehiclePawn, Cab> cabs = new();

    static PhobosVehicleRulesDef Rules => PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();

    /// <summary>The smoke still in a vehicle's cab, 0 to 1. Not saved: it clears within the hour anyway.</summary>
    public static float SmokeAboard(VehiclePawn vehicle) => cabs.TryGetValue(vehicle, out Cab? cab) ? cab.smoke : 0f;

    /// <summary>One look at a vehicle and the people aboard it, on a map or in a caravan.</summary>
    public static void Look(CompPhobosVehicleSystems comp, Caravan? caravan)
    {
        VehiclePawn vehicle = comp.Vehicle;
        Cab cab = cabs.GetOrCreateValue(vehicle);
        if (cab.smoke > 0f)
        {
            cab.smoke = (float)LifeAboardRule.SmokeLeft(cab.smoke, HoursPerLook, comp.WindowsDown || !comp.ClosedCab || Glass.Gone(comp.Vehicle) ? Rules.smokeClearHoursOpen : Rules.smokeClearHours);
            Puff(vehicle);
        }
        List<Pawn> aboard = vehicle.AllPawnsAboard;
        if (aboard.Count == 0) return;
        if (vehicle.Faction != Faction.OfPlayer)
        {
            RaiderRoll(comp, cab, aboard);
            return;
        }
        HashSet<Pawn> driving = new(VehicleStory.Drivers(vehicle));
        bool music = comp.Fittings.Grants("CabStereo") && comp.LoadRunning(StereoLoad);
        bool dark = When_Dark.IsDark(vehicle);
        bool light = !dark || (comp.Fittings.Grants("ReadingLight") && comp.LoadRunning(ReadingLightLoad));
        int now = Find.TickManager.TicksGame;
        foreach (Pawn pawn in aboard.ToList())
        {
            if (pawn.Dead || pawn.needs == null || !pawn.RaceProps.Humanlike) continue;
            Need_Joy? joy = pawn.needs.joy;
            if (music && joy != null)
            {
                joy.GainJoy((float)LifeAboardRule.JoyGained(Rules.stereoJoyPerHour, HoursPerLook), LifeAboardDefOf.HighCulture);
                if (Seen.Wants("vehicles.life-aboard-music", vehicle)) Seen.Note("vehicles.life-aboard-music", vehicle, pawn.LabelShort);
            }
            if (driving.Contains(pawn) || pawn.Downed || pawn.InMentalState) continue;
            Rider rider = riders.GetOrCreateValue(pawn);
            // On a map only: in a caravan the game, and Dubs' tie-in, already give its people water.
            if (caravan == null && Drink != null && Drink(vehicle, pawn) && Seen.Wants("vehicles.life-aboard-drink", vehicle)) Seen.Note("vehicles.life-aboard-drink", vehicle, pawn.LabelShort);
            if (VehicleSystemsMod.Settings.hotboxing && Smoke(comp, caravan, pawn, rider, now)) continue;
            if (joy == null || joy.CurLevel >= Rules.lifeAboardJoyBelow) continue;
            if (light && Book(vehicle, caravan, pawn) is { } book)
            {
                joy.GainJoy((float)LifeAboardRule.JoyGained(Rules.readingJoyPerHour, HoursPerLook), JoyKindDefOf.Reading);
                Lessons(book, pawn);
                if (Seen.Wants("vehicles.life-aboard-read", vehicle, dark ? "light" : "day")) Seen.Note("vehicles.life-aboard-read", vehicle, pawn.LabelShort + ": " + book.LabelShort, dark ? "light" : "day");
                continue;
            }
            if (LifeAboardRule.Rested(rider.treatAt, now, Rules.treatGapHours * GenDate.TicksPerHour) && Treat(vehicle, caravan, pawn))
            {
                rider.treatAt = now;
            }
        }
    }

    // ---- A book, a treat ----

    /// <summary>Everything a rider can reach: their own inventory, then the vehicle's cargo, then (in a caravan) the caravan's packs.</summary>
    static IEnumerable<Thing> Within(VehiclePawn vehicle, Caravan? caravan, Pawn pawn)
    {
        if (pawn.inventory?.innerContainer is { } own)
        {
            for (int i = 0; i < own.Count; i++) yield return own[i];
        }
        if (vehicle.inventory?.innerContainer is { } cargo)
        {
            for (int i = 0; i < cargo.Count; i++) yield return cargo[i];
        }
        if (caravan == null) yield break;
        foreach (Thing thing in CaravanInventoryUtility.AllInventoryItems(caravan)) yield return thing;
    }

    static Thing? Book(VehiclePawn vehicle, Caravan? caravan, Pawn pawn) => Within(vehicle, caravan, pawn).FirstOrDefault(t => t is Book);

    /// <summary>
    /// A book's own lessons for the reading done since the last look (Framework 0.236.0): each of its outcomes as the game's reading gives
    /// them (skill, research), at the reader's reading speed. Not a quest from a book, which looks for a map, nor the room's reading bonus:
    /// a cab is no room (the game gives 1 without one).
    /// </summary>
    static void Lessons(Thing thing, Pawn pawn)
    {
        if (thing is not Book book || book.BookComp == null) return;
        float factor = pawn.GetStatValue(StatDefOf.ReadingSpeed) * LookTicks;
        foreach (BookOutcomeDoer doer in book.BookComp.Doers)
        {
            if (doer is BookOutcomeDoer_GiveQuest) continue;
            doer.OnReadingTick(pawn, factor);
        }
        if (Seen.Wants("vehicles.life-aboard-lessons", pawn)) Seen.Note("vehicles.life-aboard-lessons", pawn, book.LabelShort);
    }

    /// <summary>A treat: something eaten or drunk for pleasure that is no drug (a tea, a coffee, a sweet), the most enjoyable first.</summary>
    static bool Treat(VehiclePawn vehicle, Caravan? caravan, Pawn pawn)
    {
        Thing? best = null;
        foreach (Thing thing in Within(vehicle, caravan, pawn))
        {
            IngestibleProperties? food = thing.def.ingestible;
            if (food == null || food.joy <= 0f || thing.def.IsDrug || food.joyKind == LifeAboardDefOf.Chemical || thing.def.IsCorpse) continue;
            if (!pawn.WillEat(thing, null, careIfNotAcceptableForTitle: true)) continue;
            if (best == null || food.joy > best.def.ingestible!.joy) best = thing;
        }
        if (best == null) return false;
        string label = best.LabelNoCount;
        float nutrition = best.Ingested(pawn, pawn.needs.food?.NutritionWanted ?? 0f);
        if (pawn.needs.food != null) pawn.needs.food.CurLevel += nutrition;
        caravan?.RecacheInventory();
        if (Seen.Wants("vehicles.life-aboard-treat", vehicle)) Seen.Note("vehicles.life-aboard-treat", vehicle, pawn.LabelShort + ": " + label);
        return true;
    }

    // ---- Hotboxing ----

    /// <summary>A drug smoked, read from its ingestion: the game's smoking sound (smokeleaf and flake in the base game; other mods' alike).</summary>
    public static bool Smoked(ThingDef def) => def.IsDrug && def.ingestible?.ingestSound?.defName == "Ingest_Smoke";

    /// <summary>
    /// On a map, a rider lights up when their drug policy schedules it now; anywhere, for recreation when their policy allows it for joy and
    /// their recreation is low, at most once in the rules' gap. In a caravan the game itself already takes scheduled drugs.
    /// </summary>
    static bool Smoke(CompPhobosVehicleSystems comp, Caravan? caravan, Pawn pawn, Rider rider, int now)
    {
        if (pawn.drugs == null || pawn.IsTeetotaler()) return false;
        Need_Joy? joy = pawn.needs.joy;
        bool mayForJoy = joy != null && joy.CurLevel < Rules.smokeJoyBelow && LifeAboardRule.Rested(rider.smokeAt, now, Rules.smokeGapHours * GenDate.TicksPerHour);
        foreach (Thing thing in Within(comp.Vehicle, caravan, pawn))
        {
            if (!Smoked(thing.def)) continue;
            DrugPolicyEntry? entry = pawn.drugs.CurrentPolicy?[thing.def];
            if (entry == null) continue;
            bool scheduled = caravan == null && pawn.drugs.ShouldTryToTakeScheduledNow(thing.def);
            bool forJoy = mayForJoy && entry.allowedForJoy && joy!.CurLevel < entry.onlyIfJoyBelow;
            if (!scheduled && !forJoy) continue;
            ThingDef def = thing.def;
            float nutrition = thing.Ingested(pawn, 0f);
            if (pawn.needs.food != null) pawn.needs.food.CurLevel += nutrition;
            caravan?.RecacheInventory();
            rider.smokeAt = now;
            if (Seen.Wants("vehicles.hotbox-smoke", comp.Vehicle, scheduled ? "scheduled" : "joy")) Seen.Note("vehicles.hotbox-smoke", comp.Vehicle, pawn.LabelShort + ": " + def.label, scheduled ? "scheduled" : "joy");
            Hotbox(comp, pawn, def);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Someone aboard smoked this drug (by our look, or the game's own caravan drug-taking): the cab fills, and each other person aboard takes
    /// a share of the drug's own high, as the rules' bystander share says (none of its tolerance or addiction), with a memory of it.
    /// </summary>
    public static void Hotbox(CompPhobosVehicleSystems comp, Pawn smoker, ThingDef drug)
    {
        if (!VehicleSystemsMod.Settings.hotboxing) return;
        VehiclePawn vehicle = comp.Vehicle;
        cabs.GetOrCreateValue(vehicle).smoke = 1f;
        IngestionOutcomeDoer_GiveHediff? high = drug.ingestible?.outcomeDoers?.OfType<IngestionOutcomeDoer_GiveHediff>().FirstOrDefault(d => d.hediffDef != null && d.severity > 0f);
        if (high == null) return;
        double share = LifeAboardRule.BystanderShare(comp.ClosedCab, comp.WindowsDown || Glass.Gone(comp.Vehicle), Rules.bystanderShareClosed, Rules.bystanderShareOpen);
        float severity = (float)LifeAboardRule.BystanderSeverity(high.severity, share, Rules.bystanderLeast);
        if (severity <= 0f) return;
        bool hard = drug.ingestible!.drugCategory == DrugCategory.Hard;
        int shared = 0;
        foreach (Pawn pawn in vehicle.AllPawnsAboard.ToList())
        {
            if (pawn == smoker || pawn.Dead || !pawn.RaceProps.Humanlike || pawn.health == null) continue;
            HealthUtility.AdjustSeverity(pawn, high.hediffDef, severity);
            shared++;
            if (pawn.needs?.mood?.thoughts?.memories is not { } memories) continue;
            ThoughtDef thought = LifeAboardRule.Remembers(pawn.IsTeetotaler(), hard) == LifeAboardRule.Feeling.Bad ? LifeAboardDefOf.PhobosHotboxedAgainstWill : LifeAboardDefOf.PhobosHotboxed;
            memories.TryGainMemory(thought);
        }
        if (shared > 0 && Seen.Wants("vehicles.hotbox-shared", vehicle, comp.ClosedCab && !comp.WindowsDown ? "closed" : "open"))
        {
            Seen.Note("vehicles.hotbox-shared", vehicle, shared + " shared " + severity.ToString("0.00"), comp.ClosedCab && !comp.WindowsDown ? "closed" : "open");
        }
    }

    /// <summary>Wisps of smoke from the cab while it holds smoke, on the map being looked at only.</summary>
    static void Puff(VehiclePawn vehicle)
    {
        if (!vehicle.Spawned || vehicle.Map != Find.CurrentMap) return;
        Vector3 at = vehicle.DrawPos + new Vector3(Rand.Range(-0.4f, 0.4f), 0f, Rand.Range(0.2f, 0.8f));
        FleckMaker.ThrowSmoke(at, vehicle.Map, 0.5f + SmokeAboard(vehicle) * 0.5f);
    }

    /// <summary>
    /// Another faction's driver lights up on the way in, now and then (the owner, 12 Nov 2026: a rare roll): once a vehicle arrives with people
    /// aboard, a small chance its driver takes the base game's smokeleaf high, and the cab fills. Slower through the driver rule, as ours are.
    /// </summary>
    static void RaiderRoll(CompPhobosVehicleSystems comp, Cab cab, List<Pawn> aboard)
    {
        if (cab.rolled || !VehicleSystemsMod.Settings.hotboxing || !comp.Vehicle.Spawned) return;
        cab.rolled = true;
        if (!Rand.Chance(Rules.raiderHotboxChance)) return;
        ThingDef? joint = DefDatabase<ThingDef>.GetNamedSilentFail("SmokeleafJoint");
        IngestionOutcomeDoer_GiveHediff? high = joint?.ingestible?.outcomeDoers?.OfType<IngestionOutcomeDoer_GiveHediff>().FirstOrDefault(d => d.hediffDef != null);
        Pawn? driver = VehicleStory.Driver(comp.Vehicle) ?? aboard.FirstOrDefault(p => p.RaceProps.Humanlike);
        if (joint == null || high == null || driver == null) return;
        HealthUtility.AdjustSeverity(driver, high.hediffDef, high.severity);
        Hotbox(comp, driver, joint);
        if (Seen.Wants("vehicles.hotbox-raider", comp.Vehicle)) Seen.Note("vehicles.hotbox-raider", comp.Vehicle, comp.Vehicle.Faction?.Name ?? "");
    }
}

[DefOf]
public static class LifeAboardDefOf
{
    public static ThoughtDef PhobosHotboxed = null!;
    public static ThoughtDef PhobosHotboxedAgainstWill = null!;
    /// <summary>The game's "music" and "chemical consumption" recreation kinds.</summary>
    public static JoyKindDef HighCulture = null!;
    public static JoyKindDef Chemical = null!;

    static LifeAboardDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(LifeAboardDefOf));
}

/// <summary>Life aboard in the player's vehicle caravans: one look per vehicle every 250 ticks (Framework 0.233.0).</summary>
public class LifeAboardOnTheRoad : WorldComponent
{
    public LifeAboardOnTheRoad(World world) : base(world)
    {
    }

    public override void WorldComponentTick()
    {
        base.WorldComponentTick();
        if (Find.TickManager.TicksGame % LifeAboard.LookTicks != 17) return;
        foreach (Caravan caravan in Find.WorldObjects.Caravans)
        {
            if (caravan is not VehicleCaravan convoy || convoy.Faction != Faction.OfPlayer) continue;
            foreach (VehiclePawn vehicle in convoy.VehiclesListForReading)
            {
                if (vehicle.GetComp<CompPhobosVehicleSystems>() is { Usable: true } comp) LifeAboard.Look(comp, caravan);
            }
        }
    }
}

/// <summary>
/// The game's own caravan drug-taking (a scheduled drug in a caravan, Caravan_NeedsTracker.IngestDrug): when the person is aboard a vehicle and
/// the drug is smoked, the cab fills and the others share it, as with our own look (Framework 0.233.0). A postfix; the game's work is unchanged.
/// </summary>
[HarmonyPatch(typeof(Caravan_NeedsTracker), nameof(Caravan_NeedsTracker.IngestDrug), new[] { typeof(Pawn), typeof(Thing), typeof(Pawn) })]
public static class HotboxInCaravan
{
    public static void Postfix(Pawn pawn, Thing drug)
    {
        if (!VehicleSystemsMod.Settings.hotboxing || drug == null || !LifeAboard.Smoked(drug.def)) return;
        if (pawn.ParentHolder is not VehicleRoleHandler handler || handler.vehicle?.GetComp<CompPhobosVehicleSystems>() is not { Usable: true } comp) return;
        LifeAboard.Hotbox(comp, pawn, drug.def);
    }
}

/// <summary>Someone is aboard; with <c>passengers</c>, someone not driving (Framework 0.233.0: the stereo and the reading light).</summary>
public class When_RidersAboard : VehicleSystemCondition
{
    public bool passengers;

    protected override bool HoldsRaw(SystemContext context)
    {
        List<Pawn> aboard = context.Vehicle.AllPawnsAboard;
        if (aboard.Count == 0) return false;
        if (!passengers) return true;
        HashSet<Pawn> driving = new(VehicleStory.Drivers(context.Vehicle));
        return aboard.Any(p => !driving.Contains(p));
    }
}
