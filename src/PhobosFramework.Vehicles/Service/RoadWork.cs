using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>A water bore a halted convoy is drilling or pumping (Framework 0.317.0, EM34). Saved with the world.</summary>
public sealed class RoadBore : IExposable
{
    public Caravan? caravan;
    public VehiclePawn? rig;
    /// <summary>Hours drilled, at the plain pace, to <see cref="BoreRule.DrillHours"/>.</summary>
    public float drilled;
    /// <summary>Litres it has given so far.</summary>
    public float pumped;
    /// <summary>What holds it up now, said once when it changes; not saved.</summary>
    public string? heldUp;

    public bool Drilled => drilled >= BoreRule.DrillHours - 0.0001f;

    public void ExposeData()
    {
        Scribe_References.Look(ref caravan, "PhobosBoreCaravan");
        Scribe_References.Look(ref rig, "PhobosBoreRig");
        Scribe_Values.Look(ref drilled, "PhobosBoreDrilled");
        Scribe_Values.Look(ref pumped, "PhobosBorePumped");
    }
}

/// <summary>The convoys' work on the road: the water bores, stepped a few times a game hour. The running survey's state is the Datum's own.</summary>
public sealed class WorldComponent_PhobosRoadWork : WorldComponent
{
    List<RoadBore> bores = new();

    public WorldComponent_PhobosRoadWork(World world) : base(world)
    {
    }

    public static WorldComponent_PhobosRoadWork? Current => Find.World?.GetComponent<WorldComponent_PhobosRoadWork>();

    public List<RoadBore> Bores => bores;

    public RoadBore? BoreOf(Caravan caravan) => bores.FirstOrDefault(b => b.caravan == caravan);

    public override void WorldComponentTick()
    {
        if (Find.TickManager.TicksGame % RoadWork.Interval != 0) return;
        RoadWork.Step(this);
    }

    public override void ExposeData()
    {
        Scribe_Collections.Look(ref bores, "PhobosRoadBores", LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            bores ??= new List<RoadBore>();
            bores.RemoveAll(b => b?.caravan == null || b.rig == null);
        }
    }
}

/// <summary>
/// Work on the road (Framework 0.317.0; docs/development/vehicle-road-work.md; EM30 and EM34, the owner's cards of 26 Nov 2026): the Datum's
/// running survey in a moving convoy, and the Kelly's water bore at a halt. Every rule is here; the vehicles' caravan buttons and the
/// caravan's inspect lines only ask.
/// </summary>
public static class RoadWork
{
    /// <summary>Ticks between steps: a tenth of a game hour.</summary>
    public const int Interval = 250;
    /// <summary>Mining experience a tick of drilling gives (agent's figure, as the contracts' work).</summary>
    public const float LearnPerTick = 0.04f;
    /// <summary>Fuel the Kelly burns an hour at a bore, drilling or pumping (agent's figure, as its water bore contract).</summary>
    public const float BoreFuelPerHour = 0.6f;
    /// <summary>The drill bit's health lost an hour of drilling at the plain pace (agent's figure: about a dozen bores from a new 25-health bit;
    /// drilling on a map wears it only in overdrive).</summary>
    public const float BitWearPerDrillHour = 0.25f;
    /// <summary>The Kelly's drill bit, by its part key.</summary>
    public const string DrillBitKey = "DrillBit";

    static float Hours => Interval / (float)GenDate.TicksPerHour;

    public static void Step(WorldComponent_PhobosRoadWork work)
    {
        foreach (Caravan caravan in Find.WorldObjects.Caravans)
        {
            if (caravan is VehicleCaravan convoy && convoy.Faction == Faction.OfPlayer && Moving(convoy)) SurveyStep(convoy);
        }
        foreach (RoadBore bore in work.Bores.ToList()) BoreStep(work, bore);
    }

    /// <summary>Whether a convoy is driving now (Vehicle Framework's own pather; the game's reads still).</summary>
    public static bool Moving(Caravan caravan) => caravan is VehicleCaravan { vehiclePather.MovingNow: true };

    /// <summary>The convoy's people in a driving seat now.</summary>
    static HashSet<Pawn> Drivers(Caravan caravan) => TankLarder.Vehicles(caravan).SelectMany(VehicleStory.Drivers).ToHashSet();

    /// <summary>The convoy's best hand at a skill: a person of the player's fit to work, the driving seats left out when asked.</summary>
    public static Pawn? Hand(Caravan caravan, SkillDef skill, bool notDriving)
    {
        HashSet<Pawn>? drivers = notDriving ? Drivers(caravan) : null;
        return caravan.PawnsListForReading
            .Where(p => p.RaceProps.Humanlike && p.Faction == Faction.OfPlayer && !p.Downed && !p.Dead && p.skills != null
                && !p.skills.GetSkill(skill).TotallyDisabled && (drivers == null || !drivers.Contains(p)))
            .OrderByDescending(p => p.skills.GetSkill(skill).Level).FirstOrDefault();
    }

    // ---- The running survey (EM30) ----

    /// <summary>One step for each Datum of a moving convoy that is surveying: its non-driving hand at Intellectual work.</summary>
    static void SurveyStep(VehicleCaravan convoy)
    {
        foreach (VehiclePawn vehicle in convoy.VehiclesListForReading)
        {
            if (vehicle.GetComp<CompPhobosSurveyTruck>() is not { RoadSurvey: true } survey) continue;
            if (!survey.LongRangeFitted)
            {
                survey.RoadHeldUp("PhobosRoadWork_SurveyNoKit".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve(), convoy);
                continue;
            }
            if (Hand(convoy, SkillDefOf.Intellectual, notDriving: true) is not { } hand)
            {
                survey.RoadHeldUp("PhobosRoadWork_SurveyNoHand".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve(), convoy);
                continue;
            }
            survey.RoadStep(hand, convoy, Interval);
        }
    }

    /// <summary>
    /// The find of a running survey: the game's own long-range lump quest, as the deployed truck's (the colony's map stands in for the
    /// convoy's, which it has none of), and then its site, not yet on the world map, is moved to a free tile near the convoy (the owner's
    /// card: near the convoy, so the route matters). False when the game would not make one.
    /// </summary>
    public static bool FindOnTheRoad(CompPhobosSurveyTruck survey, Pawn hand, Caravan convoy)
    {
        Map? home = Find.AnyPlayerHomeMap;
        if (home == null) return false;
        var slate = new RimWorld.QuestGen.Slate();
        slate.Set("map", home);
        slate.Set("targetMineable", survey.Target);
        slate.Set("targetMineableThing", survey.Target.building.mineableThing);
        slate.Set("worker", hand);
        if (!QuestScriptDefOf.LongRangeMineralScannerLump.CanRun(slate, home)) return false;
        Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(QuestScriptDefOf.LongRangeMineralScannerLump, slate);
        PlanetTile placed = PlanetTile.Invalid;
        foreach (QuestPart_SpawnWorldObject part in quest.PartsListForReading.OfType<QuestPart_SpawnWorldObject>())
        {
            if (part.worldObject is not Site { Spawned: false } site) continue;
            if (TileFinder.TryFindNewSiteTile(out PlanetTile near, convoy.Tile, FindNearestTiles, FindFarthestTiles)) site.Tile = placed = near;
        }
        Find.LetterStack.ReceiveLetter(quest.name, quest.description, LetterDefOf.PositiveEvent, null, null, quest);
        VehicleStory.Of(survey.Vehicle, RoadWorkDefOf.PhobosLog_RoadFind, who: hand.LabelShort, subject: survey.Target.building.mineableThing.label);
        if (Seen.Wants("vehicles.road-survey-find", survey.Vehicle))
            Seen.Note("vehicles.road-survey-find", survey.Vehicle, survey.Target.building.mineableThing.label + ", " + (placed.Valid ? Find.WorldGrid.ApproxDistanceInTiles(convoy.Tile, placed).ToString("0") + " tiles away" : "not moved"));
        return true;
    }

    /// <summary>How far from the convoy a running survey's find is put, in tiles (agent's figures).</summary>
    public const int FindNearestTiles = 2;
    public const int FindFarthestTiles = 6;

    // ---- The water bore (EM34) ----

    static VehicleComponent? Bit(VehiclePawn rig) => rig.statHandler?.GetComponent(DrillBitKey);

    /// <summary>The tile's yearly rainfall, in millimetres.</summary>
    public static float Rainfall(Caravan caravan) => Find.WorldGrid[caravan.Tile].rainfall;

    /// <summary>Why this convoy's drill rig cannot drill a bore here now, in-world, or null. The dry ground is said before anything is spent.</summary>
    public static string? BoreRefusal(Caravan caravan, VehiclePawn rig, bool starting)
    {
        if (!CaravanWater.HasSource) return "PhobosRoadWork_BoreNoWater".Translate().Resolve();
        if (Moving(caravan)) return "PhobosRoadWork_BoreMoving".Translate().Resolve();
        if (CaravanVisitUtility.SettlementVisitedNow(caravan) != null) return "PhobosRoadWork_BoreSettlement".Translate().Resolve();
        float rain = Rainfall(caravan);
        if (BoreRule.Dry(rain))
            return "PhobosRoadWork_BoreDry".Translate(rain.ToString("0").Named("RAIN"), BoreRule.DryBelowMillimetres.ToString("0").Named("LINE")).Resolve();
        if (Bit(rig) is { Health: <= 0f }) return "PhobosRoadWork_BoreBitWrecked".Translate(rig.LabelShort.Named("VEHICLE")).Resolve();
        if (Hand(caravan, SkillDefOf.Mining, notDriving: false) == null) return "PhobosRoadWork_BoreNoHand".Translate().Resolve();
        if (rig.CompFueledTravel is { EmptyTank: true }) return "PhobosRoadWork_BoreNoFuel".Translate(rig.LabelShort.Named("VEHICLE")).Resolve();
        if (starting && CaravanWater.Room(caravan) < 1f) return "PhobosRoadWork_BoreTanksFull".Translate().Resolve();
        return null;
    }

    public static void StartBore(Caravan caravan, VehiclePawn rig)
    {
        if (WorldComponent_PhobosRoadWork.Current is not { } work || work.BoreOf(caravan) != null) return;
        if (BoreRefusal(caravan, rig, starting: true) is { } refused)
        {
            Messages.Message(refused, caravan, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        work.Bores.Add(new RoadBore { caravan = caravan, rig = rig });
        Messages.Message("PhobosRoadWork_BoreStarted".Translate(rig.LabelShort.Named("VEHICLE"), BoreRule.LitresPerHour(Rainfall(caravan)).ToString("0").Named("LITRES")),
            caravan, MessageTypeDefOf.TaskCompletion, historical: false);
    }

    public static void StopBore(Caravan caravan)
    {
        if (WorldComponent_PhobosRoadWork.Current is not { } work || work.BoreOf(caravan) is not { } bore) return;
        End(work, bore, "PhobosRoadWork_BoreStopped".Translate(bore.pumped.ToString("0").Named("LITRES")).Resolve(), MessageTypeDefOf.TaskCompletion);
    }

    /// <summary>
    /// One step of a bore: it ends when the convoy drives on or is gone, or its tanks are full; it is held up (said once) by what the
    /// refusal names; drilling stops at night while the crew rests, pumping runs on. Drilling wears the bit and teaches Mining; both burn the
    /// Kelly's fuel.
    /// </summary>
    static void BoreStep(WorldComponent_PhobosRoadWork work, RoadBore bore)
    {
        Caravan? caravan = bore.caravan;
        VehiclePawn? rig = bore.rig;
        if (caravan == null || caravan.Destroyed || rig == null || rig.Destroyed || !TankLarder.Vehicles(caravan).Contains(rig))
        {
            work.Bores.Remove(bore);
            return;
        }
        if (Moving(caravan))
        {
            End(work, bore, "PhobosRoadWork_BoreLeft".Translate(caravan.LabelCap.Named("CARAVAN"), bore.pumped.ToString("0").Named("LITRES")).Resolve(),
                MessageTypeDefOf.CautionInput);
            return;
        }
        string? refused = BoreRefusal(caravan, rig, starting: false);
        if (refused != null)
        {
            if (refused != bore.heldUp)
                Messages.Message("PhobosRoadWork_BoreHeldUp".Translate(caravan.LabelCap.Named("CARAVAN"), refused.Named("REASON")), caravan,
                    MessageTypeDefOf.CautionInput, historical: false);
            bore.heldUp = refused;
            return;
        }
        bore.heldUp = null;
        float hours = Hours;
        if (!bore.Drilled)
        {
            if (caravan.NightResting) return;
            Pawn hand = Hand(caravan, SkillDefOf.Mining, notDriving: false)!;
            float pace = ContractRule.Pace(hand.skills.GetSkill(SkillDefOf.Mining).Level);
            float before = bore.drilled;
            bore.drilled = BoreRule.Drilled(bore.drilled, hours, pace);
            if (Bit(rig) is { } bit) bit.SetHealth(Mathf.Max(0f, bit.Health - BitWearPerDrillHour * (bore.drilled - before)));
            hand.skills.Learn(SkillDefOf.Mining, LearnPerTick * Interval);
            rig.CompFueledTravel?.ConsumeFuel(BoreFuelPerHour * hours);
            if (bore.Drilled && Seen.Wants("vehicles.road-bore", rig, "drilled")) Seen.Note("vehicles.road-bore", rig, Rainfall(caravan).ToString("0") + " mm", "drilled");
            if (bore.Drilled)
                Messages.Message("PhobosRoadWork_BoreDrilled".Translate(caravan.LabelCap.Named("CARAVAN")), caravan, MessageTypeDefOf.TaskCompletion, historical: false);
            return;
        }
        rig.CompFueledTravel?.ConsumeFuel(BoreFuelPerHour * hours);
        bore.pumped += CaravanWater.PutUntreated(caravan, BoreRule.LitresPerHour(Rainfall(caravan)) * hours);
        if (CaravanWater.Room(caravan) < 1f)
            End(work, bore, "PhobosRoadWork_BoreFull".Translate(caravan.LabelCap.Named("CARAVAN"), bore.pumped.ToString("0").Named("LITRES")).Resolve(),
                MessageTypeDefOf.PositiveEvent);
    }

    static void End(WorldComponent_PhobosRoadWork work, RoadBore bore, string message, MessageTypeDef kind)
    {
        work.Bores.Remove(bore);
        Messages.Message(message, bore.caravan, kind, historical: false);
        if (bore.pumped > 0f && Seen.Wants("vehicles.road-bore", bore.rig, "filled")) Seen.Note("vehicles.road-bore", bore.rig, bore.pumped.ToString("0") + " L", "filled");
        if (bore.pumped > 0f)
            VehicleStory.Of(bore.rig, RoadWorkDefOf.PhobosLog_Bore, detail: "PhobosRoadWork_Litres".Translate(bore.pumped.ToString("N0").Named("LITRES")).Resolve());
    }

    // ---- What the caravan's inspect pane says ----

    public static IEnumerable<string> InspectLines(VehicleCaravan caravan)
    {
        foreach (VehiclePawn vehicle in caravan.VehiclesListForReading)
        {
            if (vehicle.GetComp<CompPhobosSurveyTruck>() is { RoadSurvey: true } survey)
                yield return survey.RoadHeldUpLine ?? "PhobosRoadWork_InspectSurvey".Translate(vehicle.LabelShort.Named("VEHICLE"),
                    survey.Target.building.mineableThing.label.Named("ORE")).Resolve();
        }
        if (WorldComponent_PhobosRoadWork.Current?.BoreOf(caravan) is not { } bore) yield break;
        if (bore.heldUp != null) yield return "PhobosRoadWork_InspectBoreHeldUp".Translate(bore.heldUp.Named("REASON")).Resolve();
        else if (!bore.Drilled)
            yield return "PhobosRoadWork_InspectDrilling".Translate((bore.drilled / BoreRule.DrillHours).ToStringPercent().Named("SHARE")).Resolve();
        else
            yield return "PhobosRoadWork_InspectPumping".Translate(BoreRule.LitresPerHour(Rainfall(caravan)).ToString("0").Named("LITRES"),
                bore.pumped.ToString("0").Named("PUMPED")).Resolve();
    }
}

[DefOf]
public static class RoadWorkDefOf
{
    public static Phobos.Framework.Persistence.PhobosLogEntryDef PhobosLog_RoadFind = null!;
    public static Phobos.Framework.Persistence.PhobosLogEntryDef PhobosLog_Bore = null!;

    static RoadWorkDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(RoadWorkDefOf));
}
