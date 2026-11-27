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

/// <summary>
/// One piece of work a halted convoy does with one of its vehicles (Framework 0.322.0: the shape shared by the water bore, the survey halt
/// and the test hole; docs/development/vehicle-road-work.md, "work at a halt"). Stepped by <see cref="RoadWork.Step"/>: it ends when the
/// convoy drives on or is gone; what holds it up is said once; otherwise it works. Saved with the world.
/// </summary>
public abstract class RoadHaltJob : IExposable
{
    public Caravan? caravan;
    public VehiclePawn? vehicle;
    /// <summary>What holds it up now, said once when it changes; not saved.</summary>
    public string? heldUp;

    /// <summary>Why it cannot start, or go on, now, in-world; null when it can.</summary>
    public abstract string? Refusal(bool starting);

    /// <summary>One step of work. It may end the job (<see cref="RoadWork.End"/>).</summary>
    public abstract void Work(float hours);

    /// <summary>Said when the convoy drives on and leaves it.</summary>
    public abstract string LeftText();

    /// <summary>The caravan's inspect line while it works.</summary>
    public abstract string InspectLine();

    /// <summary>After it ends, for whatever is still owed (the logbook).</summary>
    public virtual void Ended() { }

    public abstract void ExposeData();
}

/// <summary>A water bore a halted convoy is drilling or pumping (Framework 0.317.0, EM34). Its saved keys are its first version's.</summary>
public sealed class RoadBore : RoadHaltJob
{
    /// <summary>Hours drilled, at the plain pace, to <see cref="BoreRule.DrillHours"/>.</summary>
    public float drilled;
    /// <summary>Litres it has given so far.</summary>
    public float pumped;

    public VehiclePawn? Rig => vehicle;
    public bool Drilled => drilled >= BoreRule.DrillHours - 0.0001f;

    public override string? Refusal(bool starting) => RoadWork.BoreRefusal(caravan!, vehicle!, starting);

    public override void Work(float hours) => RoadWork.BoreWork(this, hours);

    public override string LeftText() => "PhobosRoadWork_BoreLeft".Translate(caravan!.LabelCap.Named("CARAVAN"), pumped.ToString("0").Named("LITRES")).Resolve();

    public override string InspectLine() => !Drilled
        ? "PhobosRoadWork_InspectDrilling".Translate((drilled / BoreRule.DrillHours).ToStringPercent().Named("SHARE")).Resolve()
        : "PhobosRoadWork_InspectPumping".Translate(BoreRule.LitresPerHour(RoadWork.Rainfall(caravan!)).ToString("0").Named("LITRES"),
            pumped.ToString("0").Named("PUMPED")).Resolve();

    public override void Ended()
    {
        if (pumped <= 0f) return;
        if (Seen.Wants("vehicles.road-bore", vehicle, "filled")) Seen.Note("vehicles.road-bore", vehicle, pumped.ToString("0") + " L", "filled");
        VehicleStory.Of(vehicle, RoadWorkDefOf.PhobosLog_Bore, detail: "PhobosRoadWork_Litres".Translate(pumped.ToString("N0").Named("LITRES")).Resolve());
    }

    public override void ExposeData()
    {
        Scribe_References.Look(ref caravan, "PhobosBoreCaravan");
        Scribe_References.Look(ref vehicle, "PhobosBoreRig");
        Scribe_Values.Look(ref drilled, "PhobosBoreDrilled");
        Scribe_Values.Look(ref pumped, "PhobosBorePumped");
    }
}

/// <summary>The Datum stopped to read the ground under the convoy (Framework 0.322.0, EM31).</summary>
public sealed class RoadSurveyHalt : RoadHaltJob
{
    /// <summary>Hours of console work done, at the plain pace, to <see cref="RoadHaltRule.SurveyHaltHours"/>.</summary>
    public float read;

    public override string? Refusal(bool starting) => RoadWork.SurveyHaltRefusal(caravan!, vehicle!, starting);

    public override void Work(float hours) => RoadWork.SurveyHaltWork(this, hours);

    public override string LeftText() => "PhobosRoadWork_SurveyHaltLeft".Translate(caravan!.LabelCap.Named("CARAVAN")).Resolve();

    public override string InspectLine() =>
        "PhobosRoadWork_InspectSurveyHalt".Translate(vehicle!.LabelShort.Named("VEHICLE"), (read / RoadHaltRule.SurveyHaltHours).ToStringPercent().Named("SHARE")).Resolve();

    public override void ExposeData()
    {
        Scribe_References.Look(ref caravan, "PhobosSurveyHaltCaravan");
        Scribe_References.Look(ref vehicle, "PhobosSurveyHaltTruck");
        Scribe_Values.Look(ref read, "PhobosSurveyHaltRead");
    }
}

/// <summary>The Kelly proving a read find with a test hole (Framework 0.322.0, EM35).</summary>
public sealed class RoadTestHole : RoadHaltJob
{
    /// <summary>Hours drilled, at the plain pace, to <see cref="RoadHaltRule.TestHoleHours"/>.</summary>
    public float drilled;

    public override string? Refusal(bool starting) => RoadWork.TestHoleRefusal(caravan!, vehicle!, starting);

    public override void Work(float hours) => RoadWork.TestHoleWork(this, hours);

    public override string LeftText() => "PhobosRoadWork_TestHoleLeft".Translate(caravan!.LabelCap.Named("CARAVAN")).Resolve();

    public override string InspectLine() =>
        "PhobosRoadWork_InspectTestHole".Translate((drilled / RoadHaltRule.TestHoleHours).ToStringPercent().Named("SHARE")).Resolve();

    public override void ExposeData()
    {
        Scribe_References.Look(ref caravan, "PhobosTestHoleCaravan");
        Scribe_References.Look(ref vehicle, "PhobosTestHoleRig");
        Scribe_Values.Look(ref drilled, "PhobosTestHoleDrilled");
    }
}

/// <summary>The Kelly with its hammer bit quarrying stone chunks at a halt (Framework 0.324.0, EM36).</summary>
public sealed class RoadQuarry : RoadHaltJob
{
    /// <summary>Chunks broken out but not yet whole: carried to the next step.</summary>
    public float progress;
    /// <summary>Chunks given so far.</summary>
    public int made;

    public override string? Refusal(bool starting) => RoadWork.QuarryRefusal(caravan!, vehicle!, starting);

    public override void Work(float hours) => RoadWork.QuarryWork(this, hours);

    public override string LeftText() => "PhobosRoadWork_QuarryLeft".Translate(caravan!.LabelCap.Named("CARAVAN"), made.Named("COUNT")).Resolve();

    public override string InspectLine() => "PhobosRoadWork_InspectQuarry".Translate(vehicle!.LabelShort.Named("VEHICLE"), made.Named("COUNT")).Resolve();

    public override void Ended()
    {
        if (made <= 0) return;
        VehicleStory.Of(vehicle, RoadWorkDefOf.PhobosLog_Quarry, detail: made.ToString("N0"));
    }

    public override void ExposeData()
    {
        Scribe_References.Look(ref caravan, "PhobosQuarryCaravan");
        Scribe_References.Look(ref vehicle, "PhobosQuarryRig");
        Scribe_Values.Look(ref progress, "PhobosQuarryProgress");
        Scribe_Values.Look(ref made, "PhobosQuarryMade");
    }
}

/// <summary>
/// What a survey halt read of a world tile (Framework 0.322.0, EM31): its deep ore and roughly how much, and its yearly rain (the water
/// bore's yield). Fixed for the tile: the same tile always reads the same. A simplification: no map is made, so this is not the ore a map
/// opened there would scatter.
/// </summary>
public sealed class TileReading : IExposable
{
    public PlanetTile tile = PlanetTile.Invalid;
    public ThingDef? ore;
    public int units;
    public float rainfall;
    public bool bedrock;
    /// <summary>A test hole has been drilled here (EM35): one to a tile.</summary>
    public bool proved;

    public void ExposeData()
    {
        Scribe_Values.Look(ref tile, "PhobosReadTile", PlanetTile.Invalid);
        Scribe_Defs.Look(ref ore, "PhobosReadOre");
        Scribe_Values.Look(ref units, "PhobosReadUnits");
        Scribe_Values.Look(ref rainfall, "PhobosReadRain");
        Scribe_Values.Look(ref bedrock, "PhobosReadBedrock");
        Scribe_Values.Look(ref proved, "PhobosReadProved");
    }
}

/// <summary>A convoy waiting before it can drive on: turned back from an ambush (Framework 0.322.0, EM44), or a vehicle stuck in soft ground
/// (0.325.0, EM43). A wait saved before 0.325.0 is an ambush's.</summary>
public sealed class ConvoyHold : IExposable
{
    public Caravan? caravan;
    public int until;
    /// <summary><see cref="RoadGround.HoldAmbush"/> or <see cref="RoadGround.HoldBogged"/>.</summary>
    public int kind;
    /// <summary>The stuck vehicle, for a bogged convoy.</summary>
    public VehiclePawn? stuck;

    public void ExposeData()
    {
        Scribe_References.Look(ref caravan, "PhobosHoldCaravan");
        Scribe_Values.Look(ref until, "PhobosHoldUntil");
        Scribe_Values.Look(ref kind, "PhobosHoldKind");
        Scribe_References.Look(ref stuck, "PhobosHoldStuck");
    }
}

/// <summary>How far a heavy vehicle has sunk on soft ground since it was last freed (Framework 0.325.0, EM43): state, saved.</summary>
public sealed class SinkMark : IExposable
{
    public VehiclePawn? vehicle;
    public float sunk;

    public void ExposeData()
    {
        Scribe_References.Look(ref vehicle, "PhobosSinkVehicle");
        Scribe_Values.Look(ref sunk, "PhobosSinkMeasure");
    }
}

/// <summary>A winter tile a Talus has gritted, until the grit is gone (Framework 0.325.0, EM39).</summary>
public sealed class GritMark : IExposable
{
    public PlanetTile tile = PlanetTile.Invalid;
    public int until;

    public void ExposeData()
    {
        Scribe_Values.Look(ref tile, "PhobosGritTile", PlanetTile.Invalid);
        Scribe_Values.Look(ref until, "PhobosGritUntil");
    }
}

/// <summary>The convoys' work on the road, stepped a few times a game hour: halt jobs, the tiles read, and convoys held after turning back.
/// The running survey's state is the Datum's own.</summary>
public sealed class WorldComponent_PhobosRoadWork : WorldComponent
{
    /// <summary>The water bores, in their own list as first saved (Framework 0.317.0).</summary>
    List<RoadBore> bores = new();
    /// <summary>Every other halt job, each saved with its class.</summary>
    List<RoadHaltJob> halts = new();
    List<TileReading> readings = new();
    List<ConvoyHold> holds = new();
    List<SinkMark> sinks = new();
    List<GritMark> grit = new();
    /// <summary>The Taluses spreading grit as they drive (EM39).</summary>
    List<VehiclePawn> spreaders = new();
    /// <summary>The tile each moving convoy was last seen on, to know when it reaches a new one. Derived, never saved: after a load the first
    /// look counts the tile it stands on as reached.</summary>
    readonly Dictionary<Caravan, PlanetTile> lastTiles = new();
    /// <summary>Gritted tiles and until when, for the path costs' frequent question. Derived, rebuilt when grit is laid or wears off.</summary>
    Dictionary<PlanetTile, int>? gritUntil;

    public WorldComponent_PhobosRoadWork(World world) : base(world)
    {
    }

    public static WorldComponent_PhobosRoadWork? Current => Find.World?.GetComponent<WorldComponent_PhobosRoadWork>();

    public IEnumerable<RoadHaltJob> Jobs => bores.Cast<RoadHaltJob>().Concat(halts);

    public RoadBore? BoreOf(Caravan caravan) => bores.FirstOrDefault(b => b.caravan == caravan);

    public T? JobOf<T>(Caravan caravan) where T : RoadHaltJob => Jobs.OfType<T>().FirstOrDefault(j => j.caravan == caravan);

    /// <summary>The job a vehicle is busy with, or null: one at a time.</summary>
    public RoadHaltJob? JobOn(VehiclePawn vehicle) => Jobs.FirstOrDefault(j => j.vehicle == vehicle);

    public void Add(RoadHaltJob job)
    {
        if (job is RoadBore bore) bores.Add(bore);
        else halts.Add(job);
    }

    public void Remove(RoadHaltJob job)
    {
        if (job is RoadBore bore) bores.Remove(bore);
        else halts.Remove(job);
    }

    public TileReading? ReadingAt(PlanetTile tile) => readings.FirstOrDefault(r => r.tile == tile);

    /// <summary>The tiles read with an ore, for the deep ore overlay's frequent question (EM32). Derived, rebuilt when a reading is added.</summary>
    HashSet<PlanetTile>? oreTiles;

    public bool HasOreReading(PlanetTile tile) => (oreTiles ??= readings.Where(r => r.ore != null).Select(r => r.tile).ToHashSet()).Contains(tile);

    public void AddReading(TileReading reading)
    {
        readings.Add(reading);
        oreTiles = null;
    }

    public bool AnyHolds => holds.Count > 0;

    /// <summary>The tick until which a convoy waits, or 0.</summary>
    public int HeldUntil(Caravan caravan) => HoldOf(caravan)?.until ?? 0;

    public ConvoyHold? HoldOf(Caravan caravan) => holds.FirstOrDefault(h => h.caravan == caravan);

    public void Hold(Caravan caravan, int until, int kind, VehiclePawn? stuck)
    {
        holds.RemoveAll(h => h.caravan == caravan);
        holds.Add(new ConvoyHold { caravan = caravan, until = until, kind = kind, stuck = stuck });
    }

    public void Release(Caravan caravan) => holds.RemoveAll(h => h.caravan == caravan);

    // ---- The ground (EM43, EM39) ----

    /// <summary>Adds to how far a vehicle has sunk; returns the total.</summary>
    public float AddSink(VehiclePawn vehicle, float measure)
    {
        SinkMark? mark = sinks.FirstOrDefault(s => s.vehicle == vehicle);
        if (measure <= 0f) return mark?.sunk ?? 0f;
        if (mark == null) sinks.Add(mark = new SinkMark { vehicle = vehicle });
        mark.sunk += measure;
        return mark.sunk;
    }

    public void ClearSink(VehiclePawn vehicle) => sinks.RemoveAll(s => s.vehicle == vehicle);

    public List<VehiclePawn> Spreaders => spreaders;

    public bool AnyGrit => grit.Count > 0;

    public bool Gritted(PlanetTile tile)
    {
        gritUntil ??= grit.ToDictionary(g => g.tile, g => g.until);
        return gritUntil.TryGetValue(tile, out int until) && until > Find.TickManager.TicksGame;
    }

    public void Grit(PlanetTile tile, int until)
    {
        grit.RemoveAll(g => g.tile == tile);
        grit.Add(new GritMark { tile = tile, until = until });
        gritUntil = null;
    }

    /// <summary>True, once, when a moving convoy stands on a tile other than the one it was last seen on.</summary>
    public bool Reached(Caravan caravan, out PlanetTile tile)
    {
        tile = caravan.Tile;
        if (lastTiles.TryGetValue(caravan, out PlanetTile last) && last == tile) return false;
        lastTiles[caravan] = tile;
        return true;
    }

    public override void WorldComponentTick()
    {
        if (Find.TickManager.TicksGame % RoadWork.Interval != 0) return;
        int now = Find.TickManager.TicksGame;
        if (holds.Count > 0)
        {
            foreach (ConvoyHold done in holds.Where(h => h.caravan != null && !h.caravan.Destroyed && h.until <= now).ToList())
                RoadWork.HoldEnded(done);
            holds.RemoveAll(h => h.caravan == null || h.caravan.Destroyed || h.until <= now);
        }
        if (grit.Count > 0 && grit.RemoveAll(g => g.until <= now) > 0) gritUntil = null;
        if (lastTiles.Count > 0) foreach (Caravan gone in lastTiles.Keys.Where(c => c.Destroyed).ToList()) lastTiles.Remove(gone);
        RoadWork.Step(this);
    }

    public override void ExposeData()
    {
        Scribe_Collections.Look(ref bores, "PhobosRoadBores", LookMode.Deep);
        Scribe_Collections.Look(ref halts, "PhobosRoadHalts", LookMode.Deep);
        Scribe_Collections.Look(ref readings, "PhobosRoadReadings", LookMode.Deep);
        Scribe_Collections.Look(ref holds, "PhobosRoadHolds", LookMode.Deep);
        Scribe_Collections.Look(ref sinks, "PhobosRoadSinks", LookMode.Deep);
        Scribe_Collections.Look(ref grit, "PhobosRoadGrit", LookMode.Deep);
        Scribe_Collections.Look(ref spreaders, "PhobosRoadSpreaders", LookMode.Reference);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            bores ??= new List<RoadBore>();
            halts ??= new List<RoadHaltJob>();
            readings ??= new List<TileReading>();
            holds ??= new List<ConvoyHold>();
            bores.RemoveAll(b => b?.caravan == null || b.vehicle == null);
            halts.RemoveAll(h => h?.caravan == null || h.vehicle == null);
            readings.RemoveAll(r => r == null || !r.tile.Valid);
            holds.RemoveAll(h => h?.caravan == null);
            sinks ??= new List<SinkMark>();
            grit ??= new List<GritMark>();
            spreaders ??= new List<VehiclePawn>();
            sinks.RemoveAll(s => s?.vehicle == null);
            grit.RemoveAll(g => g == null || !g.tile.Valid);
            spreaders.RemoveAll(v => v == null);
        }
    }
}

/// <summary>
/// Work on the road (Framework 0.317.0 and 0.322.0; docs/development/vehicle-road-work.md; EM30, EM31, EM34, EM35 and EM37, the owner's
/// cards and word of 26 Nov 2026): the Datum's running survey in a moving convoy; at a halt, the Kelly's water bore and test hole and the
/// Datum's survey halt; the Talus tipping its load into a depot. Every rule is here; the vehicles' caravan buttons and the caravan's inspect
/// lines only ask.
/// </summary>
public static class RoadWork
{
    /// <summary>Ticks between steps: a tenth of a game hour.</summary>
    public const int Interval = 250;
    /// <summary>Mining experience a tick of drilling gives (agent's figure, as the contracts' work).</summary>
    public const float LearnPerTick = 0.04f;
    /// <summary>Intellectual experience a tick of the survey halt gives (the survey truck's own, deployed).</summary>
    public const float SurveyLearnPerTick = 0.035f;
    /// <summary>Fuel the Kelly burns an hour at a bore or test hole, drilling or pumping (agent's figure, as its water bore contract).</summary>
    public const float BoreFuelPerHour = 0.6f;
    /// <summary>The drill bit's health lost an hour of drilling at the plain pace (agent's figure: about a dozen bores from a new 25-health bit;
    /// drilling on a map wears it only in overdrive).</summary>
    public const float BitWearPerDrillHour = 0.25f;
    /// <summary>The Kelly's drill bit, by its part key.</summary>
    public const string DrillBitKey = "DrillBit";
    /// <summary>Mixed with a tile's number so a tile's reading is its own, and not the same draw as other rules seeded by tile.</summary>
    const int ReadingSalt = 0x5EED31;

    static float Hours => Interval / (float)GenDate.TicksPerHour;

    public static void Step(WorldComponent_PhobosRoadWork work)
    {
        foreach (Caravan caravan in Find.WorldObjects.Caravans)
        {
            if (caravan is not VehicleCaravan convoy || convoy.Faction != Faction.OfPlayer || !Moving(convoy)) continue;
            SurveyStep(convoy);
            // Framework 0.325.0 (EM43, EM39): the ground under a convoy reaching a new tile.
            if (work.Reached(convoy, out PlanetTile tile)) RoadGround.Entered(work, convoy, tile);
        }
        foreach (RoadHaltJob job in work.Jobs.ToList()) JobStep(work, job);
    }

    /// <summary>Whether a convoy is driving now (Vehicle Framework's own pather; the game's reads still).</summary>
    // Framework 0.353.0: a caravan on foot moves by the game's own pather (a caravan with a radio can now put up a relay mast, at a halt).
    public static bool Moving(Caravan caravan) =>
        caravan is VehicleCaravan vehicles ? vehicles.vehiclePather?.MovingNow == true : caravan.pather?.MovingNow == true;

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

    // ---- Halt jobs: started, stepped and ended the same way ----

    /// <summary>Why no halt job can run on this convoy here, or null: shared by every job's refusal.</summary>
    static string? HaltRefusal(Caravan caravan)
    {
        if (Moving(caravan)) return "PhobosRoadWork_BoreMoving".Translate().Resolve();
        if (CaravanVisitUtility.SettlementVisitedNow(caravan) != null) return "PhobosRoadWork_BoreSettlement".Translate().Resolve();
        return null;
    }

    /// <summary>Starts a halt job, saying why not when it cannot, and what it will do when it can.</summary>
    public static void Start(RoadHaltJob job, string started)
    {
        if (WorldComponent_PhobosRoadWork.Current is not { } work || job.caravan == null || job.vehicle == null) return;
        if (work.JobOn(job.vehicle) != null) return;
        if (job.Refusal(starting: true) is { } refused)
        {
            Messages.Message(refused, job.caravan, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        work.Add(job);
        Messages.Message(started, job.caravan, MessageTypeDefOf.TaskCompletion, historical: false);
    }

    /// <summary>The player stops a job.</summary>
    public static void Stop(RoadHaltJob job, string message) => End(job, message, MessageTypeDefOf.TaskCompletion);

    /// <summary>
    /// One step of a job: it ends when the convoy drives on or is gone, or its vehicle has left the convoy; it is held up (said once) by what
    /// its refusal names; otherwise it works.
    /// </summary>
    static void JobStep(WorldComponent_PhobosRoadWork work, RoadHaltJob job)
    {
        Caravan? caravan = job.caravan;
        VehiclePawn? vehicle = job.vehicle;
        if (caravan == null || caravan.Destroyed || vehicle == null || vehicle.Destroyed || !TankLarder.Vehicles(caravan).Contains(vehicle))
        {
            work.Remove(job);
            return;
        }
        if (Moving(caravan))
        {
            End(job, job.LeftText(), MessageTypeDefOf.CautionInput);
            return;
        }
        string? refused = job.Refusal(starting: false);
        if (refused != null)
        {
            if (refused != job.heldUp)
                Messages.Message("PhobosRoadWork_JobHeldUp".Translate(caravan.LabelCap.Named("CARAVAN"), refused.Named("REASON")), caravan,
                    MessageTypeDefOf.CautionInput, historical: false);
            job.heldUp = refused;
            return;
        }
        job.heldUp = null;
        job.Work(Hours);
    }

    public static void End(RoadHaltJob job, string message, MessageTypeDef kind)
    {
        WorldComponent_PhobosRoadWork.Current?.Remove(job);
        Messages.Message(message, job.caravan, kind, historical: false);
        job.Ended();
    }

    /// <summary>A step of drilling at the Mining hand's pace, by day only: wears the bit, teaches Mining, burns the rig's fuel. Returns the
    /// hours drilled at the plain pace, or 0 when the crew rests.</summary>
    static float Drill(Caravan caravan, VehiclePawn rig, float hours)
    {
        if (caravan.NightResting) return 0f;
        Pawn hand = Hand(caravan, SkillDefOf.Mining, notDriving: false)!;
        float pace = ContractRule.Pace(hand.skills.GetSkill(SkillDefOf.Mining).Level);
        if (Bit(rig) is { } bit) bit.SetHealth(Mathf.Max(0f, bit.Health - BitWearPerDrillHour * hours * pace));
        hand.skills.Learn(SkillDefOf.Mining, LearnPerTick * Interval);
        rig.CompFueledTravel?.ConsumeFuel(BoreFuelPerHour * hours);
        return hours * pace;
    }

    /// <summary>Why the rig cannot drill at all here now, or null: the bit, a hand, its fuel.</summary>
    static string? RigRefusal(Caravan caravan, VehiclePawn rig)
    {
        if (Bit(rig) is { Health: <= 0f }) return "PhobosRoadWork_BoreBitWrecked".Translate(rig.LabelShort.Named("VEHICLE")).Resolve();
        if (Hand(caravan, SkillDefOf.Mining, notDriving: false) == null) return "PhobosRoadWork_BoreNoHand".Translate().Resolve();
        if (rig.CompFueledTravel is { EmptyTank: true }) return "PhobosRoadWork_BoreNoFuel".Translate(rig.LabelShort.Named("VEHICLE")).Resolve();
        return null;
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

    // ---- The survey halt (EM31) ----

    /// <summary>The reading of the convoy's tile, if one was made.</summary>
    public static TileReading? Reading(Caravan caravan) => WorldComponent_PhobosRoadWork.Current?.ReadingAt(caravan.Tile);

    /// <summary>Why the Datum cannot read the ground here now, in-world, or null. A tile is read once: the ground does not change.</summary>
    public static string? SurveyHaltRefusal(Caravan caravan, VehiclePawn truck, bool starting)
    {
        if (HaltRefusal(caravan) is { } halt) return halt;
        if (starting && Reading(caravan) is { } done) return "PhobosRoadWork_SurveyHaltDone".Translate(ReadingText(done).Named("READING")).Resolve();
        if (Hand(caravan, SkillDefOf.Intellectual, notDriving: false) == null) return "PhobosRoadWork_SurveyHaltNoHand".Translate().Resolve();
        if (truck.CompFueledTravel is { EmptyTank: true }) return "PhobosRoadWork_BoreNoFuel".Translate(truck.LabelShort.Named("VEHICLE")).Resolve();
        return null;
    }

    public static void StartSurveyHalt(Caravan caravan, VehiclePawn truck) =>
        Start(new RoadSurveyHalt { caravan = caravan, vehicle = truck },
            "PhobosRoadWork_SurveyHaltStarted".Translate(truck.LabelShort.Named("VEHICLE"), RoadHaltRule.SurveyHaltHours.ToString("0").Named("HOURS")).Resolve());

    /// <summary>
    /// A step of the survey halt: the hand's Intellectual work at the truck's own scan rate, as the deployed truck counts it, by day only
    /// (the crew rests at night); the console idles the engine, as when set up. Done, the tile is read.
    /// </summary>
    public static void SurveyHaltWork(RoadSurveyHalt job, float hours)
    {
        Caravan caravan = job.caravan!;
        VehiclePawn truck = job.vehicle!;
        if (caravan.NightResting) return;
        Pawn hand = Hand(caravan, SkillDefOf.Intellectual, notDriving: false)!;
        float pace = hand.GetStatValue(StatDefOf.ResearchSpeed) * truck.GetStatValue(RigDefOf.Phobos_ScanRate);
        job.read = RoadHaltRule.Worked(job.read, hours, pace, RoadHaltRule.SurveyHaltHours);
        hand.skills?.Learn(SkillDefOf.Intellectual, SurveyLearnPerTick * Interval);
        if (truck.GetComp<CompPhobosSurveyTruck>() is { } survey) truck.CompFueledTravel?.ConsumeFuel(survey.Props.idleFuelPerDay * hours / 24f);
        if (job.read < RoadHaltRule.SurveyHaltHours - 0.0001f) return;
        TileReading reading = Read(caravan.Tile);
        WorldComponent_PhobosRoadWork.Current?.AddReading(reading);
        string text = ReadingText(reading);
        End(job, "PhobosRoadWork_SurveyHaltRead".Translate(caravan.LabelCap.Named("CARAVAN"), text.Named("READING")).Resolve(), MessageTypeDefOf.PositiveEvent);
        VehicleStory.Of(truck, RoadWorkDefOf.PhobosLog_SurveyHalt, who: hand.LabelShort, detail: text);
        if (Seen.Wants("vehicles.road-survey-halt", truck))
            Seen.Note("vehicles.road-survey-halt", truck, (reading.ore?.label ?? "no ore") + ", " + reading.units + " units, " + reading.rainfall.ToString("0") + " mm");
    }

    /// <summary>The tile's reading: its ore by the game's own deep commonality and lump sizes, drawn from a value fixed for the tile.</summary>
    public static TileReading Read(PlanetTile tile)
    {
        var reading = new TileReading { tile = tile, rainfall = Find.WorldGrid[tile].rainfall };
        reading.bedrock = Find.WorldGrid[tile]?.PrimaryBiome?.hasBedrock ?? false;
        if (!reading.bedrock) return reading;
        Rand.PushState(Gen.HashCombineInt(tile.tileId, ReadingSalt));
        try
        {
            if (DefDatabase<ThingDef>.AllDefs.Where(d => d.deepCommonality > 0f).TryRandomElementByWeight(d => d.deepCommonality, out ThingDef ore))
            {
                reading.ore = ore;
                reading.units = RoadHaltRule.Units(ore.deepLumpSizeRange.min, ore.deepLumpSizeRange.max, Rand.Value, ore.deepCountPerCell);
            }
        }
        finally
        {
            Rand.PopState();
        }
        return reading;
    }

    /// <summary>"About 1,800 steel under the ground; a bore would give about 400 litres an hour." The water only where water has a use.</summary>
    public static string ReadingText(TileReading reading)
    {
        string ground = reading.ore != null
            ? "PhobosRoadWork_ReadingOre".Translate(reading.units.ToString("N0").Named("UNITS"), reading.ore.label.Named("ORE")).Resolve()
            : (reading.bedrock ? "PhobosRoadWork_ReadingNoOre" : "PhobosRoadWork_ReadingNoBedrock").Translate().Resolve();
        if (!CaravanWater.HasSource) return ground;
        string water = BoreRule.Dry(reading.rainfall)
            ? "PhobosRoadWork_ReadingDry".Translate().Resolve()
            : "PhobosRoadWork_ReadingWater".Translate(BoreRule.LitresPerHour(reading.rainfall).ToString("0").Named("LITRES")).Resolve();
        return ground + " " + water;
    }

    // ---- The test hole (EM35) ----

    /// <summary>Why the Kelly cannot prove a find here now, in-world, or null: it needs this tile read with an ore that comes up as a solid.</summary>
    public static string? TestHoleRefusal(Caravan caravan, VehiclePawn rig, bool starting)
    {
        if (HaltRefusal(caravan) is { } halt) return halt;
        TileReading? reading = Reading(caravan);
        if (reading == null) return "PhobosRoadWork_TestHoleNotRead".Translate().Resolve();
        if (reading.ore == null) return "PhobosRoadWork_TestHoleNoOre".Translate().Resolve();
        if (starting && reading.proved) return "PhobosRoadWork_TestHoleProved".Translate(reading.ore.label.Named("ORE")).Resolve();
        if (Piped(rig, reading.ore)) return "PhobosRoadWork_TestHolePiped".Translate(reading.ore.label.Named("ORE")).Resolve();
        return RigRefusal(caravan, rig);
    }

    /// <summary>A deep resource the rig only pipes, never brings up as items (the chemfuel under the ground).</summary>
    static bool Piped(VehiclePawn rig, ThingDef ore) => rig.GetComp<CompPhobosDrillRig>()?.Props is CompProperties_PhobosDrillRig props && props.pipedResources.Contains(ore);

    public static void StartTestHole(Caravan caravan, VehiclePawn rig) =>
        Start(new RoadTestHole { caravan = caravan, vehicle = rig },
            "PhobosRoadWork_TestHoleStarted".Translate(rig.LabelShort.Named("VEHICLE"), RoadHaltRule.TestHoleHours.ToString("0").Named("HOURS")).Resolve());

    /// <summary>
    /// A step of the test hole: drilling as the bore drills. With the insects' setting on (off by default: the owner's card of 26 Nov 2026,
    /// high risk) the game's deep drill insect rule is rolled as the rig rolls it on a map; with it off the hole yields a little less. Done,
    /// its portions go into the convoy's cargo, the tipper first.
    /// </summary>
    public static void TestHoleWork(RoadTestHole job, float hours)
    {
        Caravan caravan = job.caravan!;
        VehiclePawn rig = job.vehicle!;
        float drilled = Drill(caravan, rig, hours);
        if (drilled <= 0f) return;
        job.drilled = RoadHaltRule.Worked(job.drilled, drilled, 1f, RoadHaltRule.TestHoleHours);
        bool insects = VehicleSystemsMod.Settings.testHoleInsects;
        if (insects && RoadInsects.Roll(caravan, drilled))
        {
            WorldComponent_PhobosRoadWork.Current?.Remove(job);
            RoadInsects.Bring(caravan, rig);
            return;
        }
        if (job.drilled < RoadHaltRule.TestHoleHours - 0.0001f) return;
        TileReading reading = Reading(caravan)!;
        reading.proved = true;
        ThingDef ore = reading.ore!;
        int count = Mathf.Min(reading.units, RoadHaltRule.Portions(insects) * Mathf.Max(1, ore.deepCountPerPortion));
        int given = GiveToConvoy(caravan, ore, count);
        string detail = "PhobosRoadWork_Amount".Translate(given.ToString("N0").Named("COUNT"), ore.label.Named("ORE")).Resolve();
        End(job, "PhobosRoadWork_TestHoleDone".Translate(caravan.LabelCap.Named("CARAVAN"), detail.Named("AMOUNT")).Resolve(), MessageTypeDefOf.PositiveEvent);
        VehicleStory.Of(rig, RoadWorkDefOf.PhobosLog_TestHole, detail: detail);
        if (Seen.Wants("vehicles.road-test-hole", rig, "proved")) Seen.Note("vehicles.road-test-hole", rig, detail, "proved");
    }

    // ---- The quarry halt (EM36) ----

    /// <summary>The rig's factor on stone; above 1 only with a bit made for breaking rock (the hammer's 1.5): read by its effect, not its name.</summary>
    static float StoneFactor(VehiclePawn rig) => rig.GetStatValue(RigDefOf.Phobos_BedrockFactor);

    /// <summary>The stone chunk of the tile's first natural rock, or null where there is none.</summary>
    public static ThingDef? QuarryChunk(Caravan caravan) =>
        Find.World.NaturalRockTypesIn(caravan.Tile).Select(rock => rock.building?.mineableThing).FirstOrDefault(chunk => chunk != null);

    static int Hilliness(Caravan caravan) => (int)Find.WorldGrid[caravan.Tile].hilliness;

    /// <summary>Why the rig cannot quarry here now, in-world, or null: it needs a bit for rock, rock in the ground, and room in the convoy.</summary>
    public static string? QuarryRefusal(Caravan caravan, VehiclePawn rig, bool starting)
    {
        if (HaltRefusal(caravan) is { } halt) return halt;
        if (StoneFactor(rig) <= 1f) return "PhobosRoadWork_QuarryNoBit".Translate(rig.LabelShort.Named("VEHICLE")).Resolve();
        if (QuarryChunk(caravan) == null || RoadHaltRule.QuarryRate(StoneFactor(rig), Hilliness(caravan)) <= 0f)
            return "PhobosRoadWork_QuarryNoRock".Translate().Resolve();
        if (RigRefusal(caravan, rig) is { } rigWhy) return rigWhy;
        if (caravan.MassUsage >= caravan.MassCapacity) return "PhobosRoadWork_QuarryFull".Translate(caravan.LabelCap.Named("CARAVAN")).Resolve();
        return null;
    }

    public static void StartQuarry(Caravan caravan, VehiclePawn rig) =>
        Start(new RoadQuarry { caravan = caravan, vehicle = rig },
            "PhobosRoadWork_QuarryStarted".Translate(rig.LabelShort.Named("VEHICLE"), QuarryChunk(caravan)!.label.Named("CHUNK"),
                RoadHaltRule.QuarryRate(StoneFactor(rig), Hilliness(caravan)).ToString("0.#").Named("RATE")).Resolve());

    /// <summary>
    /// A step of quarrying: drilled hours, as a bore drills them, give chunks at <see cref="RoadHaltRule.QuarryRate"/>; each whole chunk goes
    /// into the convoy's cargo, the tipper first. It stops itself when the convoy can carry no more (its refusal, said once, then ended).
    /// </summary>
    public static void QuarryWork(RoadQuarry job, float hours)
    {
        Caravan caravan = job.caravan!;
        VehiclePawn rig = job.vehicle!;
        float drilled = Drill(caravan, rig, hours);
        if (drilled <= 0f) return;
        job.progress += drilled * RoadHaltRule.QuarryRate(StoneFactor(rig), Hilliness(caravan));
        int whole = (int)job.progress;
        if (whole <= 0) return;
        job.progress -= whole;
        job.made += GiveToConvoy(caravan, QuarryChunk(caravan)!, whole);
        if (Seen.Wants("vehicles.road-quarry", rig)) Seen.Note("vehicles.road-quarry", rig, job.made + " chunks");
        if (caravan.MassUsage >= caravan.MassCapacity)
            End(job, "PhobosRoadWork_QuarryFull".Translate(caravan.LabelCap.Named("CARAVAN")).Resolve(), MessageTypeDefOf.TaskCompletion);
    }

    /// <summary>Puts this much of a thing into the convoy's cargo, a tipper's hold first, then wherever the game puts a caravan's goods.</summary>
    static int GiveToConvoy(Caravan caravan, ThingDef def, int count)
    {
        int given = 0;
        VehiclePawn? tipper = TankLarder.Vehicles(caravan).FirstOrDefault(v => v.GetComp<CompPhobosTipper>() != null && v.inventory?.innerContainer != null);
        while (given < count)
        {
            Thing thing = ThingMaker.MakeThing(def);
            thing.stackCount = Mathf.Min(def.stackLimit, count - given);
            given += thing.stackCount;
            if (tipper != null && tipper.inventory.innerContainer.TryAdd(thing)) continue;
            CaravanInventoryUtility.GiveThing(caravan, thing);
        }
        if (caravan is VehicleCaravan convoy) convoy.RecacheVehicles();
        return given;
    }

    // ---- The water bore (EM34) ----

    static VehicleComponent? Bit(VehiclePawn rig) => rig.statHandler?.GetComponent(DrillBitKey);

    /// <summary>The tile's yearly rainfall, in millimetres.</summary>
    public static float Rainfall(Caravan caravan) => Find.WorldGrid[caravan.Tile].rainfall;

    /// <summary>Why this convoy's drill rig cannot drill a bore here now, in-world, or null. The dry ground is said before anything is spent.</summary>
    public static string? BoreRefusal(Caravan caravan, VehiclePawn rig, bool starting)
    {
        if (!CaravanWater.HasSource) return "PhobosRoadWork_BoreNoWater".Translate().Resolve();
        if (HaltRefusal(caravan) is { } halt) return halt;
        float rain = Rainfall(caravan);
        if (BoreRule.Dry(rain))
            return "PhobosRoadWork_BoreDry".Translate(rain.ToString("0").Named("RAIN"), BoreRule.DryBelowMillimetres.ToString("0").Named("LINE")).Resolve();
        if (RigRefusal(caravan, rig) is { } rigWhy) return rigWhy;
        if (starting && CaravanWater.Room(caravan) < 1f) return "PhobosRoadWork_BoreTanksFull".Translate().Resolve();
        return null;
    }

    public static void StartBore(Caravan caravan, VehiclePawn rig) =>
        Start(new RoadBore { caravan = caravan, vehicle = rig },
            "PhobosRoadWork_BoreStarted".Translate(rig.LabelShort.Named("VEHICLE"), BoreRule.LitresPerHour(Rainfall(caravan)).ToString("0").Named("LITRES")).Resolve());

    public static void StopBore(Caravan caravan)
    {
        if (WorldComponent_PhobosRoadWork.Current?.BoreOf(caravan) is not { } bore) return;
        Stop(bore, "PhobosRoadWork_BoreStopped".Translate(bore.pumped.ToString("0").Named("LITRES")).Resolve());
    }

    /// <summary>A step of a bore: drilled by day while the crew works; then pumping, day and night, until the convoy's water tanks are full.</summary>
    public static void BoreWork(RoadBore bore, float hours)
    {
        Caravan caravan = bore.caravan!;
        VehiclePawn rig = bore.vehicle!;
        if (!bore.Drilled)
        {
            float drilled = Drill(caravan, rig, hours);
            if (drilled <= 0f) return;
            bore.drilled = BoreRule.Drilled(bore.drilled, drilled, 1f);
            if (bore.Drilled && Seen.Wants("vehicles.road-bore", rig, "drilled")) Seen.Note("vehicles.road-bore", rig, Rainfall(caravan).ToString("0") + " mm", "drilled");
            if (bore.Drilled)
                Messages.Message("PhobosRoadWork_BoreDrilled".Translate(caravan.LabelCap.Named("CARAVAN")), caravan, MessageTypeDefOf.TaskCompletion, historical: false);
            return;
        }
        rig.CompFueledTravel?.ConsumeFuel(BoreFuelPerHour * hours);
        bore.pumped += CaravanWater.PutUntreated(caravan, BoreRule.LitresPerHour(Rainfall(caravan)) * hours);
        if (CaravanWater.Room(caravan) < 1f)
            End(bore, "PhobosRoadWork_BoreFull".Translate(caravan.LabelCap.Named("CARAVAN"), bore.pumped.ToString("0").Named("LITRES")).Resolve(),
                MessageTypeDefOf.PositiveEvent);
    }

    // ---- Tip the load into a depot (EM37) ----

    /// <summary>Why the tipper cannot leave its load here now, or null: the depot's own rules (a stopped convoy, ground of its own), and a load.</summary>
    public static string? TipRefusal(VehicleCaravan convoy, VehiclePawn tipper)
    {
        if (tipper.inventory?.innerContainer is not { Count: > 0 }) return "PhobosRoadWork_TipEmpty".Translate(tipper.LabelShort.Named("VEHICLE")).Resolve();
        return Depots.LeaveRefusal(convoy);
    }

    /// <summary>
    /// The tipper's whole load into the depot on the convoy's tile, making one if there is none (the depot window could already do it item
    /// by item; this is one press). A lighter truck climbs faster, as the drivetrain moves the real mass.
    /// </summary>
    public static void TipLoad(VehicleCaravan convoy, VehiclePawn tipper)
    {
        if (TipRefusal(convoy, tipper) is { } refused)
        {
            Messages.Message(refused, convoy, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        PhobosSlingWreck depot = Depots.At(convoy.Tile) ?? PhobosSlingWreck.MakeDepot(convoy.Tile);
        int count = 0;
        foreach (Thing thing in tipper.inventory.innerContainer.ToList())
        {
            count += thing.stackCount;
            depot.Keep(thing);
        }
        convoy.RecacheVehicles();
        Messages.Message("PhobosRoadWork_Tipped".Translate(tipper.LabelShort.Named("VEHICLE"), count.ToString("N0").Named("COUNT")).Resolve(), convoy,
            MessageTypeDefOf.TaskCompletion, historical: false);
        VehicleStory.Of(tipper, RoadWorkDefOf.PhobosLog_TippedAtDepot, detail: count.ToString("N0"));
        if (Seen.Wants("vehicles.road-tip", tipper)) Seen.Note("vehicles.road-tip", tipper, count + " items");
    }

    /// <summary>A wait has run out: a stuck vehicle is free (an ambush's wait ends quietly; the convoy's line stops counting).</summary>
    public static void HoldEnded(ConvoyHold hold)
    {
        if (hold.kind != RoadGround.HoldBogged || hold.caravan == null) return;
        Messages.Message("PhobosGround_FreedInTime".Translate((hold.stuck?.LabelShort ?? hold.caravan.LabelShort).Named("VEHICLE")).Resolve(), hold.caravan,
            MessageTypeDefOf.PositiveEvent, historical: false);
    }

    // ---- The mast (EM33) ----

    /// <summary>Why the Datum cannot hold its boom up as a mast here now, or null: a stopped convoy that has a radio to lift.</summary>
    public static string? MastRefusal(Caravan caravan, VehiclePawn truck)
    {
        if (HaltRefusal(caravan) is { } halt) return halt;
        if (!ConvoyRadio.Available) return "PhobosRadioReach_NoRadioMod".Translate().Resolve();
        if (ConvoyRadio.Of(caravan) == null) return "PhobosRadioReach_NoRadio".Translate().Resolve();
        return null;
    }

    public static void StartMast(Caravan caravan, VehiclePawn truck)
    {
        Start(new RoadMast { caravan = caravan, vehicle = truck },
            "PhobosRadioReach_MastUp".Translate(truck.LabelShort.Named("VEHICLE"), ConvoyRadio.MastMetres.ToString("0").Named("METRES")).Resolve());
        ConvoyRadio.Forget(caravan);
        if (ConvoyRadio.Of(caravan) is { } reach && Seen.Wants("vehicles.radio-reach", truck, "mast"))
            Seen.Note("vehicles.radio-reach", truck, reach.DistanceKm.ToString("0") + " km of " + reach.HorizonKm.ToString("0") + (reach.InTouch ? ", in reach" : ", out"), "mast");
    }

    // ---- What the Fleet board says (Framework 0.326.0) ----

    /// <summary>
    /// The Fleet board's "doing" for a vehicle of a convoy, from the road work, or null to let the board say it the usual way: the halt job
    /// it is busy with, stuck in soft ground, waiting after turning back from an ambush, spreading grit or surveying as it drives.
    /// </summary>
    public static string? FleetDoing(Caravan convoy, VehiclePawn vehicle)
    {
        // Framework 0.329.0: a supply run says where it is going, or what it waits for.
        if (vehicle.GetComp<CompPhobosSupplyRun>()?.DoingLine is { } run) return run;
        if (WorldComponent_PhobosRoadWork.Current is not { } work) return null;
        if (work.HoldOf(convoy) is { } hold && hold.until > Find.TickManager.TicksGame)
        {
            string time = (hold.until - Find.TickManager.TicksGame).ToStringTicksToPeriod();
            if (hold.kind == RoadGround.HoldBogged)
                return hold.stuck == vehicle ? "PhobosFleet_DoingBogged".Translate(time.Named("TIME")).Resolve()
                    : "PhobosFleet_DoingWaitingBogged".Translate((hold.stuck?.LabelShort ?? "?").Named("VEHICLE")).Resolve();
            return "PhobosFleet_DoingAmbushWait".Translate(time.Named("TIME")).Resolve();
        }
        if (work.JobOn(vehicle) is { } job) return job.heldUp != null ? "PhobosRoadWork_InspectJobHeldUp".Translate(job.heldUp.Named("REASON")).Resolve() : job.InspectLine();
        if (!Moving(convoy)) return null;
        if (work.Spreaders.Contains(vehicle)) return "PhobosFleet_DoingGritting".Translate().Resolve();
        if (vehicle.GetComp<CompPhobosSurveyTruck>() is { RoadSurvey: true, LongRangeFitted: true }) return "PhobosFleet_DoingRoadSurvey".Translate().Resolve();
        return null;
    }

    /// <summary>The Fleet board's "needs" for a vehicle of a convoy, from the road work, or null: a way out of soft ground, or what holds its
    /// halt job up.</summary>
    public static string? FleetNeeds(Caravan convoy, VehiclePawn vehicle)
    {
        if (WorldComponent_PhobosRoadWork.Current is not { } work) return null;
        if (work.HoldOf(convoy) is { kind: RoadGround.HoldBogged } hold && hold.stuck == vehicle && hold.until > Find.TickManager.TicksGame)
            return "PhobosFleet_NeedsUnbogging".Translate(GroundRule.StoneChunks.Named("CHUNKS")).Resolve();
        return work.JobOn(vehicle)?.heldUp;
    }

    /// <summary>The Fleet board's detail lines for a convoy: its radio reach home, and the ground it stands on if read.</summary>
    public static IEnumerable<string> FleetDetail(Caravan convoy)
    {
        if (ConvoyRadio.Of(convoy) is { } reach) yield return reach.Line;
        if (Reading(convoy) is { } reading) yield return "PhobosRoadWork_InspectReading".Translate(ReadingText(reading).Named("READING")).Resolve();
        if (WorldComponent_PhobosRoadWork.Current?.Gritted(convoy.Tile) == true) yield return "PhobosGround_InspectGritted".Translate().Resolve();
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
        if (WorldComponent_PhobosRoadWork.Current is not { } work) yield break;
        foreach (RoadHaltJob job in work.Jobs)
        {
            if (job.caravan != caravan) continue;
            yield return job.heldUp != null ? "PhobosRoadWork_InspectJobHeldUp".Translate(job.heldUp.Named("REASON")).Resolve() : job.InspectLine();
        }
        if (work.ReadingAt(caravan.Tile) is { } reading) yield return "PhobosRoadWork_InspectReading".Translate(ReadingText(reading).Named("READING")).Resolve();
        if (work.HoldOf(caravan) is { } hold && hold.until > Find.TickManager.TicksGame)
        {
            string time = (hold.until - Find.TickManager.TicksGame).ToStringTicksToPeriod();
            yield return hold.kind == RoadGround.HoldBogged
                ? "PhobosGround_InspectBogged".Translate((hold.stuck?.LabelShort ?? caravan.LabelShort).Named("VEHICLE"), time.Named("TIME")).Resolve()
                : "PhobosAmbush_InspectHeld".Translate(time.Named("TIME")).Resolve();
        }
        if (work.Gritted(caravan.Tile)) yield return "PhobosGround_InspectGritted".Translate().Resolve();
        // Framework 0.325.0 (EM33): the convoy's radio reach home, with the Radio Framework installed and a radio aboard.
        if (ConvoyRadio.Of(caravan) is { } reach) yield return reach.Line;
    }
}

[DefOf]
public static class RoadWorkDefOf
{
    public static Phobos.Framework.Persistence.PhobosLogEntryDef PhobosLog_RoadFind = null!;
    public static Phobos.Framework.Persistence.PhobosLogEntryDef PhobosLog_Bore = null!;
    public static Phobos.Framework.Persistence.PhobosLogEntryDef PhobosLog_SurveyHalt = null!;
    public static Phobos.Framework.Persistence.PhobosLogEntryDef PhobosLog_TestHole = null!;
    public static Phobos.Framework.Persistence.PhobosLogEntryDef PhobosLog_TippedAtDepot = null!;
    public static Phobos.Framework.Persistence.PhobosLogEntryDef PhobosLog_AmbushEscaped = null!;
    public static Phobos.Framework.Persistence.PhobosLogEntryDef PhobosLog_TrailerWrecked = null!;
    public static Phobos.Framework.Persistence.PhobosLogEntryDef PhobosLog_Quarry = null!;
    public static Phobos.Framework.Persistence.PhobosLogEntryDef PhobosLog_Bogged = null!;

    static RoadWorkDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(RoadWorkDefOf));
}
