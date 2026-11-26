using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Phobos.Framework;
using Phobos.Framework.Rules;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Vehicles;
using Vehicles.World;
using Verse;
using Verse.AI.Group;

namespace Phobos.VehicleSystems;

/// <summary>
/// A test hole on the world map meets the game's insects (Framework 0.322.0, EM35; the owner's card of 26 Nov 2026: high risk, so its
/// setting starts off). The game's deep drill rule, as the rig keeps it on a map (<c>CompPhobosDrillRig.MaybeInsects</c>): the storyteller's
/// own mean time between infestations a drill, its difficulty's factor, and the same threat points. A convoy has no map, so the insects
/// open one, as an ambush does.
/// </summary>
public static class RoadInsects
{
    /// <summary>The game's insects by their own Def names, cheapest first; any missing is left out.</summary>
    static readonly string[] Kinds = { "Megascarab", "Spelopede", "Megaspider" };

    /// <summary>The game's roll for these hours of drilling at the plain pace: the same risk a portion as on a map.</summary>
    public static bool Roll(Caravan caravan, float drilledHours)
    {
        if (Faction.OfInsects == null || drilledHours <= 0f) return false;
        float chance = Find.Storyteller.difficulty.deepDrillInfestationChanceFactor;
        float baseMtb = Find.Storyteller.def.comps.OfType<StorytellerCompProperties_DeepDrillInfestation>().FirstOrDefault()?.baseMtbDaysPerDrill ?? -1f;
        if (chance <= 0f || baseMtb <= 0f) return false;
        return Rand.MTBEventOccurs(DrillRule.InsectMtbDays(baseMtb / chance, 1f), GenDate.TicksPerDay, drilledHours * GenDate.TicksPerHour);
    }

    /// <summary>The insects come up the hole: an encounter map opens round the convoy, with the rig's own threat points.</summary>
    public static void Bring(Caravan caravan, VehiclePawn rig)
    {
        float points = Mathf.Clamp(StorytellerUtility.DefaultThreatPointsNow(caravan) * Rand.Range(0.3f, 0.6f), 200f, 1000f);
        List<Pawn> insects = Generate(points);
        if (insects.Count == 0) return;
        if (Seen.Wants("vehicles.road-test-hole", rig, "insects")) Seen.Note("vehicles.road-test-hole", rig, insects.Count + " insects", "insects");
        LongEventHandler.QueueLongEvent(() =>
        {
            if (caravan.Destroyed) return;
            Map map = CaravanIncidentUtility.SetupCaravanAttackMap(caravan, insects, sendLetterIfRelatedPawns: false);
            LordMaker.MakeNewLord(Faction.OfInsects, new LordJob_AssaultColony(Faction.OfInsects, canKidnap: false, canTimeoutOrFlee: false, canSteal: false), map, insects);
            Find.LetterStack.ReceiveLetter("PhobosRoadWork_TestHoleInsectsLabel".Translate(), "PhobosRoadWork_TestHoleInsectsText".Translate(rig.LabelShort.Named("VEHICLE")),
                LetterDefOf.ThreatBig, insects[0]);
            Find.TickManager.Notify_GeneratedPotentiallyHostileMap();
        }, "GeneratingMapForNewEncounter", doAsynchronously: false, null);
    }

    static List<Pawn> Generate(float points)
    {
        List<PawnKindDef> kinds = Kinds.Select(name => DefDatabase<PawnKindDef>.GetNamedSilentFail(name)).Where(k => k != null).ToList();
        var pawns = new List<Pawn>();
        if (kinds.Count == 0) return pawns;
        float least = kinds.Min(k => k.combatPower);
        for (int guard = 0; points >= least && guard < 50; guard++)
        {
            PawnKindDef kind = kinds.Where(k => k.combatPower <= points).RandomElement();
            pawns.Add(PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, Faction.OfInsects)));
            points -= kind.combatPower;
        }
        return pawns;
    }
}

/// <summary>
/// What a warned convoy does about an ambush (Framework 0.322.0, EM44; docs/development/vehicle-road-work.md; the owner's card of 26 Nov
/// 2026: one named roll, shaped by the convoy's state, with its own setting; the game's default, to fight, stays). A convoy on the move
/// sees the ambush coming and the game stops for the player's choice: fight, as the game would; drive through; or turn back. A way out is
/// <see cref="AmbushRule.Chance"/>, rolled once; if it fails, the ambush goes ahead as the game's. The rules are here; the dialog only asks.
/// </summary>
public static class AmbushChoices
{
    /// <summary>True while the game's own ambush runs from here, so the patch lets it through.</summary>
    static bool running;

    /// <summary>The vehicles' outer armour against sharp hits counted as full armour (agent's figures): a rating of 1 without Combat
    /// Extended (a truck's panels are 0.1 to 0.35, an armour kit adds to them); 10 mm with it, where ratings are millimetres.</summary>
    public const float FullArmour = 1f;
    public const float FullArmourCombatExtended = 10f;

    /// <summary>The game's ambush itself, to run once the player has chosen.</summary>
    static readonly System.Reflection.MethodInfo? GameAmbush = AccessTools.Method(typeof(IncidentWorker_Ambush), "TryExecuteWorker");

    /// <summary>Whether this ambush asks the player first: a convoy of the player's vehicles on the move, with the setting on.</summary>
    public static bool Offers(IncidentParms parms) =>
        !running && GameAmbush != null && VehicleSystemsMod.Settings.ambushChoices && parms.target is VehicleCaravan convoy && convoy.Faction == Faction.OfPlayer
        && RoadWork.Moving(convoy);

    public readonly struct Odds
    {
        public readonly float DriveThrough;
        public readonly float TurnBack;
        public readonly string? Stuck;

        public Odds(float driveThrough, float turnBack, string? stuck)
        {
            DriveThrough = driveThrough;
            TurnBack = turnBack;
            Stuck = stuck;
        }
    }

    /// <summary>The odds of each way out for this convoy now, and the vehicle that cannot move, if any.</summary>
    public static Odds OddsFor(VehicleCaravan convoy)
    {
        VehiclePawn? stuck = convoy.VehiclesListForReading.FirstOrDefault(Mobility.Stopped);
        float speed = AmbushRule.SpeedTerm(convoy.TicksPerMove, CaravanTicksPerMoveUtility.DefaultTicksPerMove);
        float full = OptionalMods.IsActive(OptionalMods.CombatExtended) ? FullArmourCombatExtended : FullArmour;
        float armour = AmbushRule.ArmourTerm(MeanOuterArmour(convoy), full);
        bool allMove = stuck == null;
        return new Odds(AmbushRule.Chance(AmbushChoice.DriveThrough, speed, armour, allMove), AmbushRule.Chance(AmbushChoice.TurnBack, speed, armour, allMove),
            stuck == null ? null : "PhobosAmbush_Stuck".Translate(stuck.LabelShort.Named("VEHICLE")).Resolve());
    }

    /// <summary>The convoy's vehicles' mean sharp armour over their outer parts, Vehicle Framework's own ratings with upgrades.</summary>
    static float MeanOuterArmour(VehicleCaravan convoy)
    {
        float sum = 0f;
        int vehicles = 0;
        foreach (VehiclePawn vehicle in convoy.VehiclesListForReading)
        {
            if (vehicle.statHandler?.components is not { } parts) continue;
            float vehicleSum = 0f;
            int outer = 0;
            foreach (VehicleComponent part in parts)
            {
                if (part.Depth != VehicleComponent.VehiclePartDepth.External) continue;
                vehicleSum += part.ArmorRating(DamageArmorCategoryDefOf.Sharp, out _);
                outer++;
            }
            if (outer == 0) continue;
            sum += vehicleSum / outer;
            vehicles++;
        }
        return vehicles == 0 ? 0f : sum / vehicles;
    }

    /// <summary>
    /// Asks the player, holding the game's ambush until they choose. The game's dialog cannot be closed but by an option (no cancel, no
    /// accept key), and it closes before the option's action runs, so the choice is made only in the options' actions.
    /// </summary>
    public static void Offer(IncidentWorker_Ambush worker, IncidentParms parms)
    {
        var convoy = (VehicleCaravan)parms.target;
        Odds odds = OddsFor(convoy);
        string who = parms.faction?.Name ?? "PhobosAmbush_Animals".Translate().Resolve();
        var node = new DiaNode("PhobosAmbush_Text".Translate(convoy.LabelCap.Named("CONVOY"), who.Named("WHO")));
        DiaOption Option(string label, AmbushChoice choice)
        {
            var option = new DiaOption(label)
            {
                resolveTree = true,
                action = () => Choose(worker, parms, choice),
            };
            if (choice != AmbushChoice.Fight && odds.Stuck != null) option.Disable(odds.Stuck);
            return option;
        }
        node.options.Add(Option("PhobosAmbush_Fight".Translate().Resolve(), AmbushChoice.Fight));
        node.options.Add(Option("PhobosAmbush_DriveThrough".Translate(odds.DriveThrough.ToStringPercent().Named("CHANCE")).Resolve(), AmbushChoice.DriveThrough));
        node.options.Add(Option("PhobosAmbush_TurnBack".Translate(odds.TurnBack.ToStringPercent().Named("CHANCE"),
            AmbushRule.TurnBackHoldHours.ToString("0").Named("HOURS")).Resolve(), AmbushChoice.TurnBack));
        Find.WindowStack.Add(new Dialog_NodeTree(node, delayInteractivity: true, radioMode: false, "PhobosAmbush_Title".Translate().Resolve()));
    }

    /// <summary>
    /// The player's choice. To fight is the game's ambush. A way out rolls <b>the named roll "drive through or turn back"</b> once, at
    /// <see cref="OddsFor"/>: getting through keeps the convoy's way; turning back stops it and holds it <see cref="AmbushRule.TurnBackHoldHours"/>
    /// hours; a failed roll is the game's ambush.
    /// </summary>
    public static void Choose(IncidentWorker_Ambush worker, IncidentParms parms, AmbushChoice choice)
    {
        var convoy = (VehicleCaravan)parms.target;
        if (choice == AmbushChoice.Fight || convoy.Destroyed)
        {
            RunGameAmbush(worker, parms);
            return;
        }
        Odds odds = OddsFor(convoy);
        float chance = choice == AmbushChoice.DriveThrough ? odds.DriveThrough : odds.TurnBack;
        bool away = Rand.Chance(chance);
        VehicleSystemsMod.Logger.Debug("Ambush on " + convoy.LabelCap + ": " + choice + " at " + chance.ToStringPercent() + ", " + (away ? "got away" : "caught"));
        if (Seen.Wants("vehicles.road-ambush", convoy.VehiclesListForReading.FirstOrDefault(), choice.ToString()))
            Seen.Note("vehicles.road-ambush", convoy.VehiclesListForReading.FirstOrDefault(), chance.ToStringPercent() + ", " + (away ? "away" : "caught"), choice.ToString());
        if (!away)
        {
            Messages.Message("PhobosAmbush_Caught".Translate(convoy.LabelCap.Named("CONVOY")).Resolve(), convoy, MessageTypeDefOf.ThreatBig, historical: false);
            RunGameAmbush(worker, parms);
            return;
        }
        if (choice == AmbushChoice.TurnBack)
        {
            convoy.vehiclePather.StopDead();
            WorldComponent_PhobosRoadWork.Current?.Hold(convoy, Find.TickManager.TicksGame + (int)(AmbushRule.TurnBackHoldHours * GenDate.TicksPerHour));
        }
        string text = (choice == AmbushChoice.DriveThrough ? "PhobosAmbush_DroveThrough" : "PhobosAmbush_TurnedBack")
            .Translate(convoy.LabelCap.Named("CONVOY"), AmbushRule.TurnBackHoldHours.ToString("0").Named("HOURS")).Resolve();
        Messages.Message(text, convoy, MessageTypeDefOf.PositiveEvent, historical: true);
        foreach (VehiclePawn vehicle in convoy.VehiclesListForReading)
            VehicleStory.Of(vehicle, RoadWorkDefOf.PhobosLog_AmbushEscaped, detail: (choice == AmbushChoice.DriveThrough ? "PhobosAmbush_LogDrove" : "PhobosAmbush_LogTurned").Translate().Resolve());
    }

    static void RunGameAmbush(IncidentWorker_Ambush worker, IncidentParms parms)
    {
        running = true;
        try { GameAmbush?.Invoke(worker, new object[] { parms }); }
        finally { running = false; }
    }

    /// <summary>Whether a convoy that turned back is still waiting (read by the caravan's move check: off a map only, and only while any wait).</summary>
    public static bool Holding(VehiclePawn vehicle) =>
        WorldComponent_PhobosRoadWork.Current is { AnyHolds: true } work && vehicle.GetVehicleCaravan() is { } convoy
        && work.HeldUntil(convoy) > Find.TickManager.TicksGame;
}

/// <summary>
/// The game's ambush on a convoy of the player's vehicles waits for the player's choice (Framework 0.322.0, EM44). The game builds the
/// ambush in one protected method with no extension point; this prefix skips it only to ask first, and the choice runs it as it was
/// (<see cref="AmbushChoices.Choose"/>). The incident counts as fired, as the game would count it.
/// </summary>
[HarmonyPatch(typeof(IncidentWorker_Ambush), "TryExecuteWorker")]
public static class AmbushChoicePatch
{
    public static bool Prefix(IncidentWorker_Ambush __instance, IncidentParms parms, ref bool __result)
    {
        if (!AmbushChoices.Offers(parms)) return true;
        AmbushChoices.Offer(__instance, parms);
        __result = true;
        return false;
    }
}
