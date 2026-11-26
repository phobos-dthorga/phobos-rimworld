using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using RimWorld;
using SmashTools;
using UnityEngine;
using RimWorld.Planet;
using Vehicles;
using Vehicles.World;
using Verse;
using Verse.AI.Group;

namespace Phobos.VehicleSystems;

/// <summary>Where an aircraft on our own flight is in its flight over a map (saved; the design's states, the hover half).</summary>
public enum FlightState
{
    /// <summary>On the ground: Vehicle Framework drives and draws it as any vehicle.</summary>
    Ground,
    /// <summary>Climbing straight up from where it stood.</summary>
    LiftingOff,
    /// <summary>Holding still in the air.</summary>
    Hover,
    /// <summary>Flying to a point over the map.</summary>
    Moving,
    /// <summary>Coming straight down onto a landing spot.</summary>
    Settling,
    /// <summary>It cannot stay up: coming down where it is.</summary>
    ComingDown,
    /// <summary>Rolling along a strip to take off, too heavy to lift straight up (Framework 0.151.0).</summary>
    TakeoffRoll,
    /// <summary>Coming down onto a strip and rolling to a stop, unable to hover (Framework 0.151.0).</summary>
    RollingLanding,
    /// <summary>Flying round a centre, ordered or because it cannot hover where it would hold (Framework 0.153.0).</summary>
    Orbit,
    /// <summary>Hovering low at its ropes or hoist (Framework 0.155.0): coming down to its work height, at work, or climbing back.</summary>
    LineWork,
}

/// <summary>
/// Our own flight over a map, on any vehicle whose Def carries these properties (D'thorga Framework 0.147.0;
/// docs/development/vehicle-flight-over-the-map.md, the hover half). Every figure authored per aircraft; another author gives a vehicle
/// of theirs our flight by adding this in XML.
/// </summary>
public class CompProperties_PhobosFlight : VehicleCompProperties
{
    /// <summary>Its top speed over a map, cells a second at normal speed.</summary>
    public float speedCellsPerSecond = 6f;
    /// <summary>How fast it speeds up and brakes, cells a second, each second.</summary>
    public float accelerationCellsPerSecond = 1.5f;
    /// <summary>How fast it turns, degrees a second.</summary>
    public float turnDegreesPerSecond = 45f;
    /// <summary>Seconds from the ground to its flight height.</summary>
    public float climbSeconds = 6f;
    /// <summary>Seconds from its flight height down onto a landing spot.</summary>
    public float descentSeconds = 8f;
    /// <summary>Seconds from its flight height to the ground when it cannot stay up.</summary>
    public float fallSeconds = 3f;
    /// <summary>The one height it flies a map at, metres: what a rope, a hoist and the shadow read. Shown in Engineer mode.</summary>
    public float flightHeightMetres = 60f;
    /// <summary>Its shortest take-off roll, just too heavy to lift straight up, cells (Framework 0.151.0).</summary>
    public int shortestRollCells = 5;
    /// <summary>Its full take-off roll at its rolling share, and the strip a rolling landing needs, cells.</summary>
    public int fullRollCells = 20;
    /// <summary>Whether its strip must be a built floor (a jet's would); rough ground does for most.</summary>
    public bool needsBuiltFloor;

    /// <summary>
    /// Whether it can hover at all (Framework 0.302.0; docs/development/vehicle-c27j.md): false for an aeroplane, which takes off along a strip
    /// by its weight (TakeoffRule), lands rolling, circles where a rotorcraft would hold, and never drops from a hover, slings a load or works a
    /// hoist or ropes. A rotorcraft's ability to hover is still judged by its engines.
    /// </summary>
    public bool canHover = true;
    /// <summary>An aeroplane's maximum take-off weight, kilograms: its full roll at this weight, and it may not go over it. 0: no limit.</summary>
    public float maxTakeoffKilograms;
    /// <summary>The strip a rolling landing needs, cells (Framework 0.302.0); left out, its full roll.</summary>
    public int landingRollCells = -1;
    /// <summary>Its speed leaving the ground at the end of a roll, as a share of its top speed over the map (authored).</summary>
    public float liftOffSpeedShare = 0.6f;
    /// <summary>Its speed circling, cells a second (Framework 0.153.0); left out, seven tenths of its top speed (authored).</summary>
    public float orbitSpeedCellsPerSecond = float.NaN;
    /// <summary>Its tightest circle's radius, cells: never tighter than its turn allows at its circling speed.</summary>
    public float orbitRadiusCells = 12f;

    /// <summary>The height it comes down to for its ropes and hoist, metres (Framework 0.155.0; authored from the Army's 90 ft fast ropes less a
    /// margin, about 21 m, a rule read on a study card and unverified).</summary>
    public float workHeightMetres = 20f;
    /// <summary>Its fast-rope points: people on the ropes at once (the V-22 guide's two).</summary>
    public int ropePoints = 2;
    /// <summary>How fast a person slides down a fast rope, metres a second (authored).</summary>
    public float ropeSlideMetresPerSecond = 4f;
    /// <summary>Its hoist's rated load, kilograms: the person and everything they carry (the V-22 guide's 600 lb).</summary>
    public float hoistRatedKilograms = 272f;
    /// <summary>The hoist's speed at its rated load, metres a second (125 to 150 ft a minute at 600 lb, the Army manual and Breeze-Eastern).</summary>
    public float hoistLoadedMetresPerSecond = 0.7f;
    /// <summary>The hoist's speed with the hook empty, metres a second (250 ft a minute at 300 lb, the Army manual).</summary>
    public float hoistEmptyMetresPerSecond = 1.2f;
    /// <summary>Seconds to strap a person in at the bottom (authored).</summary>
    public float hoistHookSeconds = 10f;
    /// <summary>How far from the foot of the hoist, on the ground, it can reach someone or set them down, cells (authored).</summary>
    public float hoistReachCells = 3f;

    /// <summary>
    /// Stands idle unless the player's "Stock helicopters fly over the map" setting is on (Framework 0.160.0; owner, 18 Nov 2026: a setting, off
    /// by default): set on another author's helicopter given our flight in XML, so it flies as its author made it while the setting is off.
    /// </summary>
    public bool onlyWithStockSetting;

    /// <summary>
    /// Flies on our flight only while it makes a parachute drop pass (Framework 0.288.0; docs/development/airdrop.md): set on another
    /// author's aeroplane given our airdrop (the C-17), so its take-off, landing and world flight stay its author's. It comes in over the edge
    /// already flying, makes its pass and leaves; on the ground our flight never answers for it.
    /// </summary>
    public bool onlyInDrops;

    /// <summary>The sound it makes in the air on our flight (Framework 0.174.0). Left out, the sound its Def starts on Vehicle Framework's launch
    /// event, if any.</summary>
    public SoundDef? flightSound;

    /// <summary>
    /// How fast its drawn rotors turn at full speed, degrees a tick (Framework 0.173.0): Vehicle Framework's rotor overlays (Graphic_Propeller)
    /// are turned only inside its own take-off and landing animation, which our flight replaces. Left out, the full rate of the aircraft's own
    /// take-off animation, as its author drew it (the Cherokee's 48); 0 for none.
    /// </summary>
    public float rotorDegreesPerTick = float.NaN;
    /// <summary>Seconds for its rotors to spin up to full once the engines run (authored).</summary>
    public float rotorSpinUpSeconds = 6f;
    /// <summary>Seconds for its rotors to run down to rest once the engines stop (authored; a real rotor takes longer without a brake).</summary>
    public float rotorRunDownSeconds = 10f;

    /// <summary>The strip a rolling landing needs, cells.</summary>
    public int LandingRollCells => landingRollCells > 0 ? landingRollCells : fullRollCells;

    /// <summary>Its speed circling, cells a second.</summary>
    public float OrbitSpeed => float.IsNaN(orbitSpeedCellsPerSecond) ? speedCellsPerSecond * 0.7f : orbitSpeedCellsPerSecond;

    public CompProperties_PhobosFlight() => compClass = typeof(CompPhobosFlight);

    public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        foreach (string error in base.ConfigErrors(parentDef)) yield return error;
        if (speedCellsPerSecond <= 0f || accelerationCellsPerSecond <= 0f || turnDegreesPerSecond <= 0f) yield return "flight speeds must be positive";
        if (climbSeconds <= 0f || descentSeconds <= 0f || fallSeconds <= 0f) yield return "flight climb, descent and fall times must be positive";
        if (shortestRollCells < 1 || fullRollCells < shortestRollCells) yield return "a roll needs shortestRollCells of at least 1 and fullRollCells no shorter";
        if (OrbitSpeed <= 0f || OrbitSpeed > speedCellsPerSecond) yield return "orbitSpeedCellsPerSecond must be above 0 and no more than speedCellsPerSecond";
        double tightest = OrbitRule.TightestRadius(OrbitSpeed, turnDegreesPerSecond);
        if (workHeightMetres <= 0f || workHeightMetres > flightHeightMetres) yield return "workHeightMetres must be above 0 and no more than flightHeightMetres";
        if (ropePoints < 1 || ropeSlideMetresPerSecond <= 0f || hoistLoadedMetresPerSecond <= 0f || hoistEmptyMetresPerSecond <= 0f || hoistRatedKilograms <= 0f)
            yield return "ropePoints, rope and hoist speeds and the hoist's rated load must be positive";
        if (!canHover && (maxTakeoffKilograms < 0f)) yield return "maxTakeoffKilograms may not be negative";
        if (landingRollCells > 0 && landingRollCells < shortestRollCells) yield return "landingRollCells may not be shorter than shortestRollCells";
        if (orbitRadiusCells < tightest) yield return $"orbitRadiusCells {orbitRadiusCells} is tighter than it can turn at its circling speed ({tightest:0.0} cells)";
    }
}

/// <summary>
/// The aircraft stays an ordinary Vehicle Framework vehicle of the stock class, spawned on the cell beneath it; this comp moves it, keeps
/// its true position and heading, and decides what it may do from the engine model's one hover margin. It ticks only while airborne
/// (Vehicle Framework's tick by request), so a parked aircraft costs nothing. Orders are services here; the gizmos only call them.
/// </summary>
public partial class CompPhobosFlight : VehicleComp, IKeepsTicking
{
    /// <summary>Whether it still needs its tick (fault-read W11): in the air, or a raiders' aircraft unloading on the ground.</summary>
    public bool WantsTicks => Vehicle.Spawned && (Airborne || raider != null || RotorsWantTicks);

    const float TicksPerSecond = 60f;
    const int JudgeEveryTicks = 60;
    const int IdleResetTicks = 250;
    /// <summary>The share of one over-limit lift-off's strain each second of over-limit hover costs (authored: a minute of it as one).</summary>
    const float HoverStrainShare = 1f / 60f;

    FlightState state = FlightState.Ground;
    float x, z, heading, climb;
    float targetX = float.NaN, targetZ = float.NaN;
    IntVec3 landCell = IntVec3.Invalid;
    Rot4 landRot = Rot4.North;
    // Leaving the map (Framework 0.149.0): the journey Vehicle Framework's destination picker chose, kept until it reaches the edge.
    List<FlightNode>? departNodes;
    IArrivalAction? departAction;
    // A roll along a strip (Framework 0.151.0): where it began and how long it is; a landing that rolls.
    float rollFromX = float.NaN, rollFromZ = float.NaN;
    int rollCells;
    bool landRolling;
    // Circling (Framework 0.153.0): the centre and radius flown now (NaN when not circling), and the player's size and way round.
    float orbitX = float.NaN, orbitZ = float.NaN, orbitRadius;
    int orbitSize = 1;
    bool orbitClockwise = true;

    // Derived, never saved.
    float speed;
    float margin = float.NaN;
    FlightHold hold = FlightHold.Hover;
    bool redraftTold;
    // Fault-read F9: whether it has been judged since it was spawned or loaded; until then its margin is unknown (not saved).
    bool judged;
    // Fault-read F7: whether the cells under its hover have had their path cost worked out (not saved: worked out again after a load).
    bool hoverCosted;

    public CompProperties_PhobosFlight Props => (CompProperties_PhobosFlight)props;

    public FlightState State => state;
    public bool Airborne => state != FlightState.Ground;

    /// <summary>
    /// Whether our flight answers for this aircraft now (Framework 0.160.0): always for our own aircraft; for a stock helicopter only with the
    /// player's setting on. One already in the air finishes its flight first, whatever the setting.
    /// </summary>
    public bool Active => Airborne || (!Props.onlyInDrops && (!Props.onlyWithStockSetting || VehicleSystemsMod.Settings.stockHelicoptersFly));

    /// <summary>Whether this vehicle flies on the map on our own flight now: it has the comp, and the comp is active.</summary>
    public static bool FliesOnOurs(VehiclePawn? vehicle) => vehicle?.GetComp<CompPhobosFlight>() is { Active: true };

    /// <summary>Whether it is hovering, or flying and able to stop in one (Framework 0.161.0): what a lift from a hover needs.</summary>
    public bool HoversSteadily => (state is FlightState.Hover or FlightState.Moving or FlightState.Orbit) && hold == FlightHold.Hover;

    /// <summary>Whether it could still hold a hover with this much more weight on it (Framework 0.161.0: a load on the hook), under the player's
    /// hover-limit setting; true where its engines are not judged.</summary>
    public bool WouldHoverWith(double extraKg) => FlightRules.WouldHoverWith(Vehicle, extraKg);

    /// <summary>Its height now, metres.</summary>
    public float HeightMetresNow => Climb * Props.flightHeightMetres;

    /// <summary>The share of its speed it flies at now: less with a load hanging (the sling's slung flight, Framework 0.161.0).</summary>
    float LoadSpeedShare => Vehicle.GetComp<CompPhobosSling>()?.MapSpeedShare ?? 1f;

    /// <summary>How far it has climbed: 0 on the ground to 1 at its flight height.</summary>
    public float Climb => state == FlightState.Ground ? 0f : climb;

    /// <summary>Its true centre in the world, at ground level.</summary>
    public Vector3 TrueCenter => new(x, 0f, z);

    /// <summary>Its true heading, degrees clockwise from north.</summary>
    public float Heading => heading;

    /// <summary>Its speed over the map now, cells a second (derived).</summary>
    public float Speed => speed;

    /// <summary>The hover margin at the last judgement (1 is just enough), NaN when it cannot be judged.</summary>
    public float Margin => margin;

    public override bool TickByRequest => true;

    PhobosVehicleFlightProfileDef? Profile => CompPhobosVehicleSystems.Of(Vehicle)?.Powertrain?.Profile;
    float ForwardShare => Profile?.forwardFlightShare ?? 0.6f;
    float RollingShare => Profile?.rollingShare ?? float.NaN;
    /// <summary>Whether it can roll along a strip at all: an aeroplane always; a rotorcraft whose flight profile gives a rolling share.</summary>
    bool CanRoll => !Props.canHover || !float.IsNaN(RollingShare);
    float HeightCells => Props.flightHeightMetres / FlightRules.MetresPerCell;

    // ---- Saving ----

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref state, "PhobosFlight_State", FlightState.Ground);
        Scribe_Values.Look(ref x, "PhobosFlight_X");
        Scribe_Values.Look(ref z, "PhobosFlight_Z");
        Scribe_Values.Look(ref heading, "PhobosFlight_Heading");
        Scribe_Values.Look(ref enginesCut, "PhobosFlight_EnginesCut", false);
        Scribe_Values.Look(ref climb, "PhobosFlight_Climb");
        Scribe_Values.Look(ref targetX, "PhobosFlight_TargetX", float.NaN);
        Scribe_Values.Look(ref targetZ, "PhobosFlight_TargetZ", float.NaN);
        Scribe_Values.Look(ref landCell, "PhobosFlight_LandCell", IntVec3.Invalid);
        Scribe_Values.Look(ref landRot, "PhobosFlight_LandRot", Rot4.North);
        Scribe_Collections.Look(ref departNodes, "PhobosFlight_DepartNodes", LookMode.Deep);
        Scribe_Deep.Look(ref departAction, "PhobosFlight_DepartAction");
        Scribe_Values.Look(ref rollFromX, "PhobosFlight_RollFromX", float.NaN);
        Scribe_Values.Look(ref rollFromZ, "PhobosFlight_RollFromZ", float.NaN);
        Scribe_Values.Look(ref rollCells, "PhobosFlight_RollCells");
        Scribe_Values.Look(ref landRolling, "PhobosFlight_LandRolling");
        Scribe_Values.Look(ref orbitX, "PhobosFlight_OrbitX", float.NaN);
        Scribe_Values.Look(ref orbitZ, "PhobosFlight_OrbitZ", float.NaN);
        Scribe_Values.Look(ref orbitRadius, "PhobosFlight_OrbitRadius");
        Scribe_Values.Look(ref orbitSize, "PhobosFlight_OrbitSize", 1);
        Scribe_Values.Look(ref orbitClockwise, "PhobosFlight_OrbitClockwise", true);
        ExposeLines();
        ExposeRaider();
        ExposeDrop();
        if (Scribe.mode == LoadSaveMode.PostLoadInit && departNodes is { Count: 0 }) departNodes = null;
    }

    /// <summary>Whether it is on its way to the map's edge to leave for another place.</summary>
    public bool Departing => departNodes != null;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (!Airborne)
        {
            // A raiders' aircraft loaded on the ground, its raiders stepping out: it goes on thinking (Framework 0.157.0); and one with its
            // engines running turns its rotors (Framework 0.173.0).
            if (raider != null || RotorsWantTicks) StartTicking();
            return;
        }
        // An aircraft spawned in the air: after a load (Transform's turn is not saved) or on the map it was on. Never from anywhere else.
        // Framework 0.174.0 (the owner's decision 3): its rotors are already turning at full.
        rotorRate = RotorFull;
        FlightRegistry.Add(this);
        ApplyTurn();
        StartTicking();
    }

    public override void EventRegistration()
    {
        base.EventRegistration();
        // Framework 0.173.0: the engines started on the ground spin the rotors up. Once only, as Vehicle Framework may ask again.
        if (eventsRegistered) return;
        eventsRegistered = true;
        Vehicle.AddEvent(VehicleEventDefOf.IgnitionOn, OnIgnitionOn);
    }

    bool eventsRegistered;

    public override void OnDeSpawn()
    {
        base.OnDeSpawn();
        FlightRegistry.Remove(this);
        judged = false;
        // Fault-read F2 (Framework 0.163.0): taken off the map in the air by anything but its own hand-over (a caravan formed with it, the map
        // removed), it is put back in the ground state, its people off the lines and aboard, so it never comes back "airborne" at old points.
        if (state != FlightState.Ground) Grounded();
    }

    /// <summary>Back to the ground state with nothing under way: no target, landing, circle, roll, journey or raid.</summary>
    void Grounded()
    {
        EndFlightSound();
        ReturnLinesAboard();
        state = FlightState.Ground;
        climb = 0f;
        speed = 0f;
        ClearTarget();
        landCell = IntVec3.Invalid;
        landRolling = false;
        orbitX = orbitZ = float.NaN;
        rollFromX = rollFromZ = float.NaN;
        departNodes = null;
        departAction = null;
        raider = null;
        drop = null;
        inTransit = false;
        hoverCosted = false;
        Vehicle.Transform.rotation = 0f;
        StopTicking();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        FlightRegistry.Remove(this);
    }

    // ---- Orders: each says what it did, or why not ----

    /// <summary>Why it may not leave the ground from where it stands now, or null: straight up, or too heavy for that, along a clear strip
    /// ahead (Framework 0.151.0; since 0.153.0 for any flight, as it can circle where it cannot hover).</summary>
    public string? TakeOffRefusal()
    {
        if (GroundRefusal() is { } ground) return ground;
        (RollVerdict verdict, int cells, float m) = RollNeed();
        if (verdict == RollVerdict.Refused) return TooHeavy(m);
        if (verdict == RollVerdict.Rolls && StripAheadRefusal(cells) is { } why)
        {
            return "PhobosFlight_RefuseStrip".Translate(Vehicle.LabelShort.Named("VEHICLE"), cells.Named("CELLS"), why.Named("WHY")).Resolve();
        }
        return null;
    }

    /// <summary>
    /// How it would leave the ground now (Framework 0.151.0): straight up when it can hover, or when the player's setting lets it past the
    /// limit; else a roll along a strip of so many cells, or not at all (RollRule). The margin judged.
    /// </summary>
    public (RollVerdict Verdict, int Cells, float Margin) RollNeed()
    {
        if (!Props.canHover)
        {
            // Framework 0.302.0: an aeroplane always rolls, its roll set by its weight against its maximum take-off weight; with every engine
            // out, or over that weight, it does not go.
            float em = FlightRules.Margin(Vehicle);
            if ((!float.IsNaN(em) && em <= 0f) || FlightRules.EnginesOut(Vehicle)) return (RollVerdict.Refused, 0, em);
            double kg = MassKgNow();
            if (!TakeoffRule.MayTakeOff(kg, Props.maxTakeoffKilograms)) return (RollVerdict.Refused, 0, em);
            return (RollVerdict.Rolls, TakeoffRule.RollCells(kg, Props.maxTakeoffKilograms, Props.shortestRollCells, Props.fullRollCells), em);
        }
        if (!FlightRules.HoverRefused(Vehicle, out float m)) return (RollVerdict.Straight, 0, m);
        RollVerdict verdict = RollRule.Judge(m, RollingShare);
        return (verdict, verdict == RollVerdict.Rolls ? RollRule.StripCells(m, RollingShare, Props.shortestRollCells, Props.fullRollCells) : 0, m);
    }

    /// <summary>Its weight now, kilograms: the engine model's, else Vehicle Framework's mass stat.</summary>
    double MassKgNow() => CompPhobosVehicleSystems.Of(Vehicle)?.Powertrain is { } train ? train.MassKg() : Vehicle.GetStatValue(StatDefOf.Mass);

    /// <summary>Too heavy even to roll: how much lighter it must be, where it can roll at all.</summary>
    string TooHeavy(float m)
    {
        VehiclePawn v = Vehicle;
        NamedArgument who = v.LabelShort.Named("VEHICLE");
        if (!Props.canHover)
        {
            // Framework 0.302.0: an aeroplane over its maximum take-off weight sheds the difference; with its engines out, it is not its weight.
            double over = TakeoffRule.ShedKg(MassKgNow(), Props.maxTakeoffKilograms);
            if (over <= 0) return "PhobosFlight_WhyEngines".Translate().Resolve();
            return "PhobosFlight_RefuseTooHeavyShed".Translate(who, Mathf.Max(1f, (float)over).ToString("0").Named("KILOGRAMS")).Resolve();
        }
        double keep = RollRule.WeightShareToRoll(m, RollingShare, rotor: true);
        if (double.IsNaN(keep) || CompPhobosVehicleSystems.Of(v)?.Powertrain is not { } train) return "PhobosFlight_RefuseTooHeavy".Translate(who).Resolve();
        float shed = Mathf.Max(1f, (float)(train.MassKg() * (1 - keep)));
        return "PhobosFlight_RefuseTooHeavyShed".Translate(who, shed.ToString("0").Named("KILOGRAMS")).Resolve();
    }

    /// <summary>The strip ahead for a roll of so many cells: why it will not do, or null. Its result is kept for the tick, since the launch
    /// button asks every frame.</summary>
    public string? StripAheadRefusal(int cells)
    {
        int now = Find.TickManager.TicksGame;
        if (stripTick == now && stripCellsAsked == cells) return stripRefusal;
        stripTick = now;
        stripCellsAsked = cells;
        stripRefusal = FlightRules.StripRefusal(Vehicle, Vehicle.Position, Vehicle.Rotation, cells, Props.needsBuiltFloor, out _);
        return stripRefusal;
    }

    int stripTick = -1, stripCellsAsked;
    string? stripRefusal;

    /// <summary>Why it may not leave the ground in any way now (crew, fuel, roof, the systems), or null: everything but its weight.</summary>
    string? GroundRefusal()
    {
        VehiclePawn v = Vehicle;
        NamedArgument who = v.LabelShort.Named("VEHICLE");
        if (!v.Spawned || Airborne) return "PhobosFlight_RefuseNotOnGround".Translate(who).Resolve();
        if (v.vehiclePather.Moving) return "PhobosFlight_RefuseRolling".Translate(who).Resolve();
        if (!v.Drafted) return "PhobosFlight_RefuseEnginesOff".Translate(who).Resolve();
        if (!v.HasEnoughOperators) return "PhobosFlight_RefuseCrew".Translate(who).Resolve();
        if (v.CompFueledTravel is { } fuel && fuel.Fuel <= 0f) return "PhobosFlight_RefuseNoFuel".Translate(who).Resolve();
        if (FootprintRoofed(v.Position, v.Rotation)) return "PhobosFlight_RefuseRoof".Translate(who).Resolve();
        // Framework 0.174.0 (the audit of 19 Nov 2026, finding 4, after VTOL Vehicle): no air to fly in over a map in space, unless Vehicle
        // Framework's own launcher says this aircraft flies in space.
        if (v.Map.Tile.LayerDef is { isSpace: true } && v.CompVehicleLauncher is not { SpaceFlight: true })
        {
            Seen.Note("aircraft.space-refused", v);
            return "PhobosFlight_RefuseSpace".Translate(who).Resolve();
        }
        if (CompPhobosVehicleSystems.Of(v)?.LaunchRefusal() is { } systems) return systems;
        if (LaunchChecks.Refusal(v) is { } other) return other;
        return null;
    }

    /// <summary>Lifts straight up from where it stands, or rolls along the strip ahead and circles once up.</summary>
    public void TakeOff()
    {
        if (TakeOffRefusal() is { } refused)
        {
            Messages.Message(refused, Vehicle, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        (RollVerdict verdict, int cells, _) = RollNeed();
        if (verdict == RollVerdict.Rolls)
        {
            BeginRoll(cells);
            return;
        }
        BeginTakeOff();
        HoverLimits.OnLaunch(Vehicle);
    }

    /// <summary>Starts the climb from where it stands. The over-limit strain or warning is the caller's: our button's, or Vehicle
    /// Framework's launch event when it is leaving for another place (whose listeners include it).</summary>
    void BeginTakeOff()
    {
        VehiclePawn v = Vehicle;
        v.vehiclePather.StopDead();
        Vector3 centre = v.TrueCenter();
        x = centre.x;
        z = centre.z;
        heading = (float)FlightPathRule.Normalise(v.FullRotation.AsAngle + v.Transform.rotation);
        climb = 0f;
        speed = 0f;
        ClearTarget();
        state = FlightState.LiftingOff;
        FlightRegistry.Add(this);
        StartTicking();
        EndJobsOn(v);
        Seen.Note("aircraft.flight-takeoff", v);
        SayRotorsSpinningUp(v);
        // Framework 0.160.0: a stock helicopter on our flight, by its Def.
        if (Props.onlyWithStockSetting && Seen.On && Seen.Wants("aircraft.flight-stock", v, v.def.defName)) Seen.Note("aircraft.flight-stock", v, variant: v.def.defName);
        LandingSurface.OnTakeOff(v);    // Framework 0.158.0: what the ground beneath does as it lifts off
    }

    /// <summary>Ends the jobs of anyone working on, boarding, loading or refuelling it (fault-read W10, Framework 0.167.0): it is leaving the
    /// ground, and the guards would only refuse them on arrival.</summary>
    static void EndJobsOn(VehiclePawn v)
    {
        Map map = v.Map;
        if (map == null) return;
        foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned.ToList())
        {
            if (pawn == v || pawn.jobs?.curJob is not { } job) continue;
            if (job.targetA.Thing == v || job.targetB.Thing == v || job.targetC.Thing == v) pawn.jobs.EndCurrentJob(Verse.AI.JobCondition.InterruptForced);
        }
    }

    /// <summary>Starts a roll along the strip ahead, straight along its heading of four, to leave the ground at the end (Framework 0.151.0).</summary>
    void BeginRoll(int cells)
    {
        VehiclePawn v = Vehicle;
        // Framework 0.316.0: an aeroplane's roll by its weight (TakeoffRule), its cells and weight the evidence.
        if (!Props.canHover && Seen.Wants("aircraft.aeroplane-roll", v))
        {
            Seen.Note("aircraft.aeroplane-roll", v, values: new List<KeyValuePair<string, double>>
            {
                new("cells", cells), new("kg", MassKgNow()), new("maxTakeoffKg", Props.maxTakeoffKilograms),
            });
        }
        v.vehiclePather.StopDead();
        Vector3 centre = v.TrueCenter();
        x = rollFromX = centre.x;
        z = rollFromZ = centre.z;
        heading = v.Rotation.AsAngle;
        rollCells = Mathf.Max(1, cells);
        climb = 0f;
        speed = 0f;
        ClearTarget();
        state = FlightState.TakeoffRoll;
        FlightRegistry.Add(this);
        StartTicking();
        EndJobsOn(v);
        SayRotorsSpinningUp(v);
        Seen.Note("aircraft.flight-roll", v, variant: "takeoff");
        LandingSurface.OnTakeOff(v, rolling: true);
    }

    /// <summary>Why it may not set off for another place from where it is now, or null: on the ground, why it may not take off; in the air,
    /// a steady flight and its crew and fuel (Vehicle Framework judged it as standing on the ground).</summary>
    public string? DepartRefusal()
    {
        if (!Airborne) return TakeOffRefusal();
        VehiclePawn v = Vehicle;
        NamedArgument who = v.LabelShort.Named("VEHICLE");
        if (LinesOutRefusal() is { } lines) return lines;
        if (state is FlightState.LiftingOff or FlightState.ComingDown or FlightState.Settling or FlightState.RollingLanding) return "PhobosFlight_RefuseNotFlying".Translate(who).Resolve();
        if (!v.HasEnoughOperators) return "PhobosFlight_RefuseCrew".Translate(who).Resolve();
        if (v.CompFueledTravel is { } fuel && fuel.Fuel <= 0f) return "PhobosFlight_RefuseNoFuel".Translate(who).Resolve();
        return null;
    }

    /// <summary>
    /// Sets off for another place (Framework 0.149.0; the design's slice 3): lifts off if it is on the ground, flies to the map's edge on the
    /// side of the first place on the journey, and there hands over to Vehicle Framework's world flight with the journey and arrival the
    /// destination picker chose. Vehicle Framework's launch event fires now, as it does at its own lift-off.
    /// </summary>
    public void Depart(List<FlightNode> nodes, IArrivalAction arrivalAction)
    {
        VehiclePawn v = Vehicle;
        if (DepartRefusal() is { } refused)
        {
            Messages.Message(refused, v, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        if (nodes.NullOrEmpty())
        {
            return;
        }
        // Fault-read W4 (Framework 0.167.0): sent to land on a spot on the map it is on, it flies there.
        if (FlightArrival.SameMapSpot(v, nodes, arrivalAction, out IntVec3 spot, out Rot4 spotRot))
        {
            if (!Airborne)
            {
                (RollVerdict verdict, int cells, _) = RollNeed();
                if (verdict == RollVerdict.Rolls) BeginRoll(cells);
                else
                {
                    BeginTakeOff();
                    HoverLimits.OnLaunch(v);
                }
            }
            if (LandRefusal(spot, spotRot) is { } why)
            {
                Messages.Message(why, v, MessageTypeDefOf.RejectInput, historical: false);
                return;
            }
            Land(spot, spotRot);
            return;
        }
        if (!Airborne)
        {
            (RollVerdict verdict, int cells, _) = RollNeed();
            if (verdict == RollVerdict.Rolls) BeginRoll(cells);
            else BeginTakeOff();
        }
        departNodes = nodes;
        departAction = arrivalAction;
        landCell = IntVec3.Invalid;
        PlanetTile first = nodes[0].Tile;
        PlanetTile here = v.Map.Tile;
        double bearing = first.Valid && first != here ? Find.WorldGrid.GetHeadingFromTo(here, first) : heading;
        (float ex, float ez) = EdgeBounds();
        (double tx, double tz) = FlightPathRule.EdgePoint(x, z, bearing, ex, v.Map.Size.x - ex, ez, v.Map.Size.z - ez);
        (targetX, targetZ) = ((float)tx, (float)tz);
        LeaveOrbit();
        EndFlightSound();    // Framework 0.174.0: Vehicle Framework's launch sound takes over for the journey
        v.EventRegistry[VehicleEventDefOf.AerialVehicleLaunch].ExecuteEvents();
        if (v.Faction == Faction.OfPlayer)
        {
            Messages.Message("PhobosFlight_MsgDeparting".Translate(v.LabelShort.Named("VEHICLE")).Resolve(), v, MessageTypeDefOf.SilentInput, historical: false);
        }
    }

    /// <summary>A new order on the map ends a departure under way, and says so.</summary>
    void CancelDeparture()
    {
        if (!Departing) return;
        departNodes = null;
        departAction = null;
        EndLaunchSound(Vehicle);
        Messages.Message("PhobosFlight_MsgDepartureCancelled".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(), Vehicle, MessageTypeDefOf.SilentInput, historical: false);
    }

    /// <summary>
    /// Ends the sounds the aircraft's Def starts on Vehicle Framework's launch event (Framework 0.173.1; the audit of 19 Nov 2026). <see cref="Depart"/>
    /// fires that event as a journey starts, and the Def stops them only when it leaves the map (the Cherokee's and Mosquito's rotor sound), so
    /// a journey cancelled by a new order, or ended by coming down, left the sound playing after landing, where stopping the engines could not
    /// stop it: the likeliest cause of the owner's "impossible to shut off the engines" (19 Nov 2026). Only those sounds are ended; the launch
    /// event's other listeners are not told it left.
    /// </summary>
    static void EndLaunchSound(VehiclePawn v)
    {
        List<VehicleSustainerEventEntry<VehicleEventDef>>? entries = v.VehicleDef.soundSustainersOnEvent;
        if (entries == null || v.sustainers == null) return;
        for (int i = 0; i < entries.Count; i++)
        {
            VehicleSustainerEventEntry<VehicleEventDef> entry = entries[i];
            if (entry.start != VehicleEventDefOf.AerialVehicleLaunch || entry.value == null) continue;
            v.sustainers.EndAll(entry.value);
            if (Seen.On) Seen.Note("aircraft.launch-sound-ended", v, entry.value.defName);
        }
    }

    /// <summary>At the map's edge: off the map and into Vehicle Framework's world flight, as its own lift-off hands over.</summary>
    void HandOver()
    {
        VehiclePawn v = Vehicle;
        Map map = v.Map;
        List<FlightNode> nodes = departNodes!;
        IArrivalAction action = departAction!;
        departNodes = null;
        departAction = null;
        // Framework 0.288.0: a drop cut short by the edge ends here; what did not go stays aboard for home.
        EndDrop(null);
        state = FlightState.Ground;
        climb = 0f;
        speed = 0f;
        ClearTarget();
        orbitX = orbitZ = float.NaN;
        v.Transform.rotation = 0f;
        FlightRegistry.Remove(this);
        StopTicking();
        Seen.Note("aircraft.flight-departed", v);
        EndFlightSound();
        // Fault-read H2 (Framework 0.163.0): patients on its litters stay aboard; the litter would set them down as it despawns.
        v.GetComp<CompPhobosLitter>()?.HoldThroughDespawn();
        v.DeSpawn(DestroyMode.Vanish);
        map.GetDetachedMapComponent<VehiclePositionManager>().ReleaseClaimed(v);
        AerialVehicleInFlight flight = AerialVehicleInFlight.Create(v, map.Tile);
        flight.OrderFlyToTiles(nodes, action);
        v.EventRegistry[VehicleEventDefOf.AerialVehicleLeftMap].ExecuteEvents();
        if (v.Faction == Faction.OfPlayer)
        {
            Messages.Message("VF_AerialVehicleLeft".Translate(v.LabelShort.Named("VEHICLE")).Resolve(), MessageTypeDefOf.PositiveEvent);
        }
    }

    /// <summary>
    /// Enters a map already flying, at a point on its edge, heading inward (Framework 0.149.0): an arrival from another place. The vehicle has
    /// just been spawned there by FlightArrival; this puts it in the air at full height and speed.
    /// </summary>
    public void EnterFlying(float px, float pz, float inward)
    {
        VehiclePawn v = Vehicle;
        (x, z) = Clamped(px, pz);
        heading = (float)FlightPathRule.Normalise(inward);
        climb = 1f;
        speed = Props.speedCellsPerSecond;
        ClearTarget();
        landCell = IntVec3.Invalid;
        departNodes = null;
        departAction = null;
        state = FlightState.Hover;
        inTransit = true;
        rotorRate = RotorFull;    // Framework 0.174.0: it arrives flying, its rotors at full
        if (v.CompVehicleLauncher is { } launcher)
        {
            launcher.inFlight = false;
            launcher.launchProtocol?.Release();
        }
        v.vehiclePather.StopDead();
        FlightRegistry.Add(this);
        StartTicking();
        SyncPosition();
    }

    /// <summary>Why it may not be sent to this point now, or null.</summary>
    public string? FlyToRefusal(IntVec3 cell)
    {
        if (LinesOutRefusal() is { } lines) return lines;
        NamedArgument who = Vehicle.LabelShort.Named("VEHICLE");
        if (!Airborne || state is FlightState.LiftingOff or FlightState.ComingDown or FlightState.TakeoffRoll or FlightState.RollingLanding)
            return "PhobosFlight_RefuseNotFlying".Translate(who).Resolve();
        if (!cell.InBounds(Vehicle.Map)) return "PhobosFlight_RefuseOffMap".Translate(who).Resolve();
        return null;
    }

    /// <summary>Flies to the middle of a cell and holds there (as near as the map's edge allows).</summary>
    public void FlyTo(IntVec3 cell)
    {
        if (FlyToRefusal(cell) is { } refused)
        {
            Messages.Message(refused, Vehicle, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        CancelDeparture();
        Vector3 c = cell.ToVector3Shifted();
        (targetX, targetZ) = Clamped(c.x, c.z);
        landCell = IntVec3.Invalid;
        if (state == FlightState.Settling) state = FlightState.LiftingOff;
        else LeaveOrbit();
        FleckMaker.Static(cell, Vehicle.Map, FleckDefOf.FeedbackGoto);
    }

    /// <summary>Whether it is flying somewhere, or circling, so that it could be told to hold.</summary>
    public bool CanHold => state is FlightState.Moving or FlightState.Orbit;

    /// <summary>Why it may not hold now, or null: not moving, or circling already and unable to hover.</summary>
    public string? HoldRefusal()
    {
        if (LinesOutRefusal() is { } lines) return lines;
        NamedArgument who = Vehicle.LabelShort.Named("VEHICLE");
        if (!CanHold) return "PhobosFlight_RefuseNotMoving".Translate(who).Resolve();
        if (state == FlightState.Orbit && hold != FlightHold.Hover) return "PhobosFlight_RefuseCirclingNoHover".Translate(who).Resolve();
        return null;
    }

    /// <summary>Stops where it is and hovers; one that cannot hover circles here instead (Framework 0.153.0), and says so.</summary>
    public void Hold()
    {
        VehiclePawn v = Vehicle;
        if (HoldRefusal() is { } refused)
        {
            Messages.Message(refused, v, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        CancelDeparture();
        if (hold != FlightHold.Hover)
        {
            // The design's holding: where a hovering aircraft would hold, one that cannot hover circles, starting on its present course.
            ClearTarget();
            float r = OrbitRadiusNow();
            (double cx, double cz) = OrbitRule.CentreFrom(x, z, heading, r, OrbitClockwise);
            BeginOrbit((float)cx, (float)cz);
            Seen.Note("aircraft.flight-orbit", v, variant: "holding");
            Messages.Message("PhobosFlight_MsgCirclingInstead".Translate(v.LabelShort.Named("VEHICLE")).Resolve(), v, MessageTypeDefOf.SilentInput, historical: false);
            return;
        }
        LeaveOrbit();
        // Brake to rest where it can stop: the point its present speed carries it to.
        double stop = speed * speed / (2 * Props.accelerationCellsPerSecond);
        double rad = heading * Math.PI / 180;
        (targetX, targetZ) = Clamped((float)(x + Math.Sin(rad) * stop), (float)(z + Math.Cos(rad) * stop));
    }

    /// <summary>Why it may not land on this cell facing this way, or null.</summary>
    public string? LandRefusal(IntVec3 cell, Rot4 rot)
    {
        if (LinesOutRefusal() is { } lines) return lines;
        NamedArgument who = Vehicle.LabelShort.Named("VEHICLE");
        if (!Airborne || state is FlightState.ComingDown or FlightState.TakeoffRoll or FlightState.RollingLanding or FlightState.Settling)
            return "PhobosFlight_RefuseNotFlying".Translate(who).Resolve();
        // Framework 0.161.0: a load hanging below would be landed on; it is set down first.
        if (Vehicle.GetComp<CompPhobosSling>() is { Carried: { } load }) return "PhobosFlight_RefuseLandWithLoad".Translate(who, load.LabelShort.Named("LOAD")).Resolve();
        if (NeedsRollingLanding())
        {
            // Framework 0.151.0: it cannot hover, so it lands on a strip of its full roll ahead of the spot, facing as asked.
            if (!CanRoll) return "PhobosFlight_RefuseCannotHoverNoStrip".Translate(who).Resolve();
            return FlightRules.StripRefusal(Vehicle, cell, rot, Props.LandingRollCells, Props.needsBuiltFloor, out _) is { } strip
                ? "PhobosFlight_RefuseLand".Translate(who, strip.Named("WHY")).Resolve() : null;
        }
        return FlightRules.SpotRefusal(Vehicle, cell, rot, forced: false) is { } why
            ? "PhobosFlight_RefuseLand".Translate(who, why.Named("WHY")).Resolve() : null;
    }

    /// <summary>Whether a landing now must roll: its engines cannot hold it in a hover (judged afresh).</summary>
    bool NeedsRollingLanding()
    {
        margin = FlightRules.Margin(Vehicle);
        hold = FlightRules.Hold(Vehicle, margin, ForwardShare);
        return hold == FlightHold.ForwardOnly;
    }

    /// <summary>Flies over the spot, turns to face as asked, and comes straight down.</summary>
    public void Land(IntVec3 cell, Rot4 rot)
    {
        if (LandRefusal(cell, rot) is { } refused)
        {
            Messages.Message(refused, Vehicle, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        CancelDeparture();
        landCell = cell;
        landRot = rot;
        landRolling = NeedsRollingLanding();
        Vector3 c = Ext_Vehicles.TrueCenter(cell, new Rot8(rot), Vehicle.VehicleDef.Size, 0f);
        (targetX, targetZ) = (c.x, c.z);
        LeaveOrbit();
    }

    // ---- Circling (Framework 0.153.0; the design's slice 5) ----

    /// <summary>Why it may not be told to circle this point now, or null.</summary>
    public string? CircleRefusal(IntVec3 cell)
    {
        if (LinesOutRefusal() is { } lines) return lines;
        NamedArgument who = Vehicle.LabelShort.Named("VEHICLE");
        if (!Airborne || state is FlightState.LiftingOff or FlightState.ComingDown or FlightState.Settling or FlightState.TakeoffRoll or FlightState.RollingLanding)
            return "PhobosFlight_RefuseNotFlying".Translate(who).Resolve();
        if (!cell.InBounds(Vehicle.Map)) return "PhobosFlight_RefuseOffMap".Translate(who).Resolve();
        return null;
    }

    /// <summary>Flies round a point, at the player's chosen size and way round, until told otherwise; the circle is kept inside the map.</summary>
    public void Circle(IntVec3 cell)
    {
        if (CircleRefusal(cell) is { } refused)
        {
            Messages.Message(refused, Vehicle, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        CancelDeparture();
        landCell = IntVec3.Invalid;
        landRolling = false;
        ClearTarget();
        Vector3 c = cell.ToVector3Shifted();
        BeginOrbit(c.x, c.z);
        Seen.Note("aircraft.flight-orbit", Vehicle, variant: "ordered");
        FleckMaker.Static(cell, Vehicle.Map, FleckDefOf.FeedbackGoto);
    }

    /// <summary>The player's circle size, an index into <see cref="OrbitRule.SizeShares"/>.</summary>
    public int OrbitSize => Mathf.Clamp(orbitSize, 0, OrbitRule.SizeShares.Length - 1);

    /// <summary>
    /// Whether it circles clockwise, seen from above: toward a fitted side gun's side (Framework 0.329.0, <see cref="SideGuns"/>), else the
    /// player's choice.
    /// </summary>
    public bool OrbitClockwise => SideGuns.CirclesClockwise(Vehicle) ?? orbitClockwise;

    /// <summary>Whether a fitted side gun sets which way it circles, so the player's choice does not.</summary>
    public bool SideGunSetsWay => SideGuns.CirclesClockwise(Vehicle) != null;

    /// <summary>A circle size's radius for this aircraft, cells, before fitting it to the map.</summary>
    public float RadiusOfSize(int size) => Props.orbitRadiusCells * (float)OrbitRule.SizeShares[Mathf.Clamp(size, 0, OrbitRule.SizeShares.Length - 1)];

    /// <summary>Sets the size of its circles; a circle under way widens or tightens at once.</summary>
    public void SetOrbitSize(int size)
    {
        orbitSize = Mathf.Clamp(size, 0, OrbitRule.SizeShares.Length - 1);
        if (state == FlightState.Orbit) BeginOrbit(orbitX, orbitZ);
    }

    /// <summary>Turns its circles the other way round; a circle under way changes at once.</summary>
    public void ReverseOrbit()
    {
        orbitClockwise = !orbitClockwise;
    }

    /// <summary>The radius it circles at now: the player's size, made smaller where the map is too small for it.</summary>
    float OrbitRadiusNow()
    {
        (float ex, float ez) = EdgeBounds();
        IntVec3 size = Vehicle.Map.Size;
        return (float)OrbitRule.FitRadius(RadiusOfSize(OrbitSize), ex, size.x - ex, ez, size.z - ez);
    }

    /// <summary>Starts circling a centre (moved inward as little as it must be so the circle stays on the map).</summary>
    void BeginOrbit(float cx, float cz)
    {
        (float ex, float ez) = EdgeBounds();
        IntVec3 size = Vehicle.Map.Size;
        orbitRadius = OrbitRadiusNow();
        (double fx, double fz) = OrbitRule.FitCentre(cx, cz, orbitRadius, ex, size.x - ex, ez, size.z - ez);
        (orbitX, orbitZ) = ((float)fx, (float)fz);
        state = FlightState.Orbit;
    }

    /// <summary>Stops circling, flying on to whatever target it now has.</summary>
    void LeaveOrbit()
    {
        orbitX = orbitZ = float.NaN;
        if (state is FlightState.Hover or FlightState.Orbit) state = FlightState.Moving;
    }

    /// <summary>One tick round the circle: steering at its tangent (OrbitRule), within its turn rate, at its circling speed.</summary>
    void OrbitStep(float dt)
    {
        if (float.IsNaN(orbitX))
        {
            state = FlightState.Moving;
            return;
        }
        if (climb < 1f) climb = Mathf.Min(1f, climb + dt / Props.climbSeconds);
        double wanted = OrbitRule.Heading(x, z, orbitX, orbitZ, orbitRadius, OrbitClockwise, heading);
        heading = (float)FlightPathRule.TurnToward(heading, wanted, Props.turnDegreesPerSecond * dt);
        speed = (float)FlightPathRule.NextSpeed(speed, 1e6, Props.OrbitSpeed * LoadSpeedShare, Props.accelerationCellsPerSecond, dt);
        MoveAlongHeading(speed * dt);
    }

    // ---- Each tick in the air ----

    public override void CompTick()
    {
        base.CompTick();
        VehiclePawn v = Vehicle;
        int now = Find.TickManager.TicksGame;
        if (!v.Spawned || !Airborne)
        {
            // Framework 0.157.0: a raiders' aircraft keeps thinking on the ground while its raiders step out.
            if (v.Spawned && raider != null)
            {
                RotorStep();
                if ((now + v.thingIDNumber) % RaiderThinkTicks == 0) RaiderThink();
                return;
            }
            // Framework 0.173.0: on the ground, the rotors turn while the engines run and run down after.
            if (v.Spawned && RotorsWantTicks)
            {
                RotorStep();
                return;
            }
            StopTicking();
            FlightRegistry.Remove(this);
            return;
        }
        float dt = 1f / TicksPerSecond;
        RotorStep();
        // Framework 0.174.0: the sound of its flight, except while Vehicle Framework's own launch sound plays for a journey.
        if (!flightSoundOn && !Departing) StartFlightSound();
        if (v.vehiclePather.Moving) v.vehiclePather.StopDead();
        if ((now + v.thingIDNumber) % IdleResetTicks == 0) v.ResetIdleTicks();
        if (state != FlightState.ComingDown && Engines(v)) Judge(v, now);
        switch (state)
        {
            case FlightState.TakeoffRoll:
                // Framework 0.174.0 (the owner's decision 2): it rolls only once its rotors are up to speed.
                if (RotorsSpinningUp) break;
                RollStep(dt, now);
                break;
            case FlightState.RollingLanding:
                RollingLandingStep(dt, now);
                break;
            case FlightState.Orbit:
                OrbitStep(dt);
                break;
            case FlightState.LineWork:
                LineStep(dt, now);
                break;
            case FlightState.LiftingOff:
                // Framework 0.174.0 (the owner's decision 2): it leaves the ground only once its rotors are up to speed.
                if (RotorsSpinningUp) break;
                climb = Mathf.Min(1f, climb + dt / Props.climbSeconds);
                if (climb >= 1f) state = HasTarget ? FlightState.Moving : FlightState.Hover;
                break;
            case FlightState.Hover:
                // Fault-read W8: lifted into a hover low (a roll's end), it climbs on to its height.
                if (climb < 1f) climb = Mathf.Min(1f, climb + dt / Props.climbSeconds);
                if (HasTarget) state = FlightState.Moving;
                break;
            case FlightState.Moving:
                // After a roll it climbs as it flies on.
                if (climb < 1f) climb = Mathf.Min(1f, climb + dt / Props.climbSeconds);
                Step(dt);
                break;
            case FlightState.Settling:
                climb = Mathf.Max(0f, climb - dt / Props.descentSeconds);
                if (climb <= 0f) TouchDown();
                break;
            case FlightState.ComingDown:
                if (speed > 0f && HasTarget) Step(dt);
                climb = Mathf.Max(0f, climb - dt / Props.fallSeconds);
                if (climb <= 0f) ForcedLanding();
                break;
        }
        if (v.Spawned) HoverPathCost(v);
        // Framework 0.288.0: a parachute drop under way.
        if (drop != null && v.Spawned && Airborne) DropStep(dt);
        if (raider != null && v.Spawned && Airborne && (now + v.thingIDNumber) % RaiderThinkTicks == 0) RaiderThink();
        // Framework 0.161.0: a load to lift or set down from a hover.
        if (v.Spawned && Airborne && (now + v.thingIDNumber) % RaiderThinkTicks == 15) v.GetComp<CompPhobosSling>()?.FlightThink(this);
    }

    bool HasTarget => !float.IsNaN(targetX);

    void ClearTarget()
    {
        targetX = targetZ = float.NaN;
    }

    /// <summary>Keeps the engines running, or starts the fall when they cannot run. False when it is now coming down.</summary>
    bool Engines(VehiclePawn v)
    {
        if (v.CompFueledTravel is { } fuel && fuel.Fuel <= 0f)
        {
            if (state == FlightState.TakeoffRoll) AbortRoll("PhobosFlight_WhyNoFuel".Translate().Resolve());
            else BeginFall("PhobosFlight_WhyNoFuel".Translate().Resolve());
            return false;
        }
        if (!v.HasEnoughOperators)
        {
            // Fault-read F5 and W1 (Framework 0.163.0): nobody left aboard who can fly it.
            string why = "PhobosFlight_WhyCrewLost".Translate().Resolve();
            if (state == FlightState.TakeoffRoll) AbortRoll(why);
            else if (state != FlightState.RollingLanding) BeginFall(why);
            return state == FlightState.RollingLanding;
        }
        if (!v.Drafted)
        {
            // Framework 0.174.0 (the owner's decision 1): on the take-off roll, switching the engines off stops the take-off where it is.
            if (state == FlightState.TakeoffRoll && v.Faction == Faction.OfPlayer)
            {
                AbortRoll("PhobosFlight_WhyEnginesSwitchedOff".Translate().Resolve());
                return false;
            }
            // Stopping the engines in the air cannot be refused through Vehicle Framework: they are started again at once. If the game
            // refuses that too, it comes down (fault-read F5: before, the refusal repeated every tick).
            v.ignition.Drafted = true;
            // Framework 0.173.0: said in the verbose log each time, with what our flight believes, so an aircraft that looks landed but is
            // still flying by our state (its engines then cannot be stopped) can be told from a report.
            if (VehicleSystemsMod.Logger.DebugEnabled)
                VehicleSystemsMod.Logger.Debug(v.LabelShort + " (" + v.ThingID + ") engines stopped in the air and started again: state " + state + ", climb "
                    + climb.ToString("0.00") + ", at " + v.Position);
            if (!v.Drafted)
            {
                string why = "PhobosFlight_WhyEngines".Translate().Resolve();
                if (state == FlightState.TakeoffRoll) AbortRoll(why);
                else if (state != FlightState.RollingLanding) BeginFall(why);
                return state == FlightState.RollingLanding;
            }
            // Framework 0.174.0 (the owner's decision 1): switched off in the air, it lands on the nearest clear spot and shuts down there,
            // as VTOL Vehicle does. Only where it can find one; otherwise its engines are kept, and it says why.
            if (v.Faction == Faction.OfPlayer && !redraftTold)
            {
                redraftTold = true;
                if (!LandToShutDown(v))
                    Messages.Message("PhobosFlight_MsgEnginesKept".Translate(v.LabelShort.Named("VEHICLE")).Resolve(), v, MessageTypeDefOf.RejectInput, historical: false);
            }
        }
        else redraftTold = false;
        return true;
    }

    /// <summary>Judges what the engines leave it able to do, once a second: the engine model's hover margin read three ways.</summary>
    void Judge(VehiclePawn v, int now)
    {
        if (judged && (now + v.thingIDNumber) % JudgeEveryTicks != 0) return;
        judged = true;
        margin = FlightRules.Margin(v);
        hold = FlightRules.Hold(v, margin, ForwardShare);
        if (state == FlightState.TakeoffRoll)
        {
            // On the roll: it stops if the engines can no longer roll it off the ground (an engine lost, the air hotter).
            if (RollNeed().Verdict == RollVerdict.Refused) AbortRoll("PhobosFlight_WhyEngines".Translate().Resolve());
            return;
        }
        // Coming down onto a strip already: it rolls in whatever the margin.
        if (state == FlightState.RollingLanding) return;
        if (hold == FlightHold.Falling)
        {
            // Framework 0.316.0: an aeroplane with every engine out comes down.
            if (!Props.canHover) Seen.Note("aircraft.aeroplane-engines-out", v);
            BeginFall("PhobosFlight_WhyEngines".Translate().Resolve());
            return;
        }
        if (hold == FlightHold.ForwardOnly && state is FlightState.Hover or FlightState.LiftingOff or FlightState.Settling or FlightState.LineWork)
        {
            // It cannot hold a hover: it comes down where it is (the engine lost in a hover). Moving, it keeps flying (below).
            BeginFall("PhobosFlight_WhyCannotHover".Translate().Resolve());
        }
        if (margin < 1f && hold == FlightHold.Hover && VehicleSystemsMod.Settings.hoverLimit == VehicleSystemsSettings.HoverLimit.AllowWithDamage
            && (state is FlightState.Hover or FlightState.LiftingOff or FlightState.Settling or FlightState.LineWork) && CompPhobosVehicleSystems.Of(v) is { } comp)
        {
            // Allowed at a cost (fault-read F11, Framework 0.163.0): each second hovering past the limit strains the engines by a sixtieth of an
            // over-limit lift-off (authored: a minute's over-limit hover as one lift-off), never in forward flight, which asks less.
            HoverLimits.Strain(comp, HoverStrainShare);
        }
    }

    /// <summary>One tick of flight toward the target: turn, speed up or brake, move, and arrive.</summary>
    void Step(float dt)
    {
        if (!HasTarget)
        {
            if (state == FlightState.Moving && hold == FlightHold.ForwardOnly)
            {
                // Framework 0.153.0: nowhere to go and unable to stop: it circles on from its course.
                (double cx, double cz) = OrbitRule.CentreFrom(x, z, heading, OrbitRadiusNow(), OrbitClockwise);
                BeginOrbit((float)cx, (float)cz);
                return;
            }
            speed = 0f;
            if (state == FlightState.Moving) state = FlightState.Hover;
            return;
        }
        float dx = targetX - x, dz = targetZ - z;
        float distance = Mathf.Sqrt(dx * dx + dz * dz);
        double wanted = FlightPathRule.HeadingTo(dx, dz, heading);
        heading = (float)FlightPathRule.TurnToward(heading, wanted, Props.turnDegreesPerSecond * dt);
        double share = FlightPathRule.TurnSpeedShare(FlightPathRule.Turn(heading, wanted));
        bool mustKeepMoving = (hold == FlightHold.ForwardOnly && state == FlightState.Moving) || Departing || raider is { stage: RaiderStage.Leaving };
        // An aircraft that cannot hover flies its leg through at speed; it does not brake to stop where it cannot hold.
        speed = (float)FlightPathRule.NextSpeed(speed, mustKeepMoving ? 1e6 : distance, Props.speedCellsPerSecond * share * LoadSpeedShare, Props.accelerationCellsPerSecond, dt);
        float move = speed * dt;
        if (move >= distance)
        {
            x = targetX;
            z = targetZ;
            Arrive();
            // Fault-read F6 (Framework 0.163.0): the hand-over took it off the map; there is no map to hold it inside.
            if (!Vehicle.Spawned) return;
        }
        else
        {
            x += dx / distance * move;
            z += dz / distance * move;
        }
        (x, z) = Clamped(x, z);
        SyncPosition();
    }

    /// <summary>Over the target: hold there, or begin the landing; one that cannot hover comes down on the move.</summary>
    void Arrive()
    {
        inTransit = false;
        if (state == FlightState.ComingDown) return;
        // Leaving for another place: at the edge it hands over, whether or not it could hover (Framework 0.149.0).
        if (Departing)
        {
            HandOver();
            return;
        }
        // Framework 0.151.0: one that cannot hover lands rolling on a clear strip ahead of its spot; chosen while it could hover, the strip
        // is looked at now.
        if (landCell.IsValid && (landRolling || hold == FlightHold.ForwardOnly) && CanRoll)
        {
            // Fault-read W7 (Framework 0.167.0): the whole strip is looked at as it arrives, a rolling landing chosen earlier too.
            if (FlightRules.StripRefusal(Vehicle, landCell, landRot, Props.LandingRollCells, Props.needsBuiltFloor, out _) == null)
            {
                BeginRollingLanding();
                return;
            }
            landRolling = false;
        }
        if (hold == FlightHold.ForwardOnly)
        {
            // Framework 0.153.0: over a point it cannot hover at, it circles over it (the design's holding). Sent to land there and no strip
            // clear ahead of the spot, it circles too, and says why, rather than come down where it is.
            VehiclePawn v = Vehicle;
            if (landCell.IsValid && v.Faction == Faction.OfPlayer)
            {
                Messages.Message("PhobosFlight_MsgCannotLandCircling".Translate(v.LabelShort.Named("VEHICLE")).Resolve(), v, MessageTypeDefOf.CautionInput);
            }
            landCell = IntVec3.Invalid;
            ClearTarget();
            BeginOrbit(x, z);
            Seen.Note("aircraft.flight-orbit", v, variant: "holding");
            return;
        }
        speed = 0f;
        ClearTarget();
        if (landCell.IsValid)
        {
            heading = landRot.AsAngle;
            state = FlightState.Settling;
            SyncPosition();
            VehicleWorkSounds.Play(Vehicle, "Landing");    // Framework 0.169.0: the aircraft's own landing sound, if its Def names one
            return;
        }
        state = FlightState.Hover;
    }

    /// <summary>
    /// One tick of the take-off roll: speeding up along its heading to leave the ground at the strip's end (an even acceleration that reaches
    /// its lift-off speed there), looking at the strip ahead every half second. Then it climbs as it flies on.
    /// </summary>
    void RollStep(float dt, int now)
    {
        float liftOff = Mathf.Max(0.5f, Props.speedCellsPerSecond * Props.liftOffSpeedShare);
        float acceleration = liftOff * liftOff / (2f * rollCells);
        speed = Mathf.Min(liftOff, speed + acceleration * dt);
        float before = Rolled();
        MoveAlongHeading(speed * dt);
        float rolled = Rolled();
        // Fault-read F4: held at the map's edge it can roll no further, so it lifts off there.
        if (rolled < rollCells && speed > 0.1f && rolled - before < 1e-5f) rolled = rollCells;
        if ((now + Vehicle.thingIDNumber) % 30 == 0)
        {
            int left = Mathf.CeilToInt(rollCells - rolled);
            if (left > 0 && FlightRules.StripRefusal(Vehicle, Vehicle.Position, Vehicle.Rotation, left, Props.needsBuiltFloor, out _) is { } blocked)
            {
                AbortRoll(blocked);
                return;
            }
        }
        if (rolled < rollCells) return;
        // Off the ground: on to wherever it was going, climbing.
        rollFromX = rollFromZ = float.NaN;
        climb = 0.01f;
        Seen.Note("aircraft.flight-roll", Vehicle, variant: "lifted-off");
        if (HasTarget) state = FlightState.Moving;
        else if (hold == FlightHold.Hover) state = FlightState.Hover;
        else
        {
            // Framework 0.153.0: rolled off with no journey, it circles on from its course, climbing, until it is told where to go.
            (double cx, double cz) = OrbitRule.CentreFrom(x, z, heading, OrbitRadiusNow(), OrbitClockwise);
            BeginOrbit((float)cx, (float)cz);
            Seen.Note("aircraft.flight-orbit", Vehicle, variant: "holding");
        }
    }

    /// <summary>The roll is stopped (the strip blocked, the engines failing): it brakes to a stop where it is, the journey off, and says why.</summary>
    void AbortRoll(string why)
    {
        VehiclePawn v = Vehicle;
        departNodes = null;
        departAction = null;
        rollFromX = rollFromZ = float.NaN;
        PutDown(v.Position, v.Rotation);
        Seen.Note("aircraft.flight-roll", v, variant: "stopped");
        if (v.Faction == Faction.OfPlayer)
        {
            Messages.Message("PhobosFlight_MsgRollAborted".Translate(v.LabelShort.Named("VEHICLE"), why.Named("WHY")).Resolve(), v, MessageTypeDefOf.RejectInput);
        }
    }

    /// <summary>Over the start of its strip: it turns to face along it, comes down over its first part and rolls to a stop at its end.</summary>
    void BeginRollingLanding()
    {
        VehiclePawn v = Vehicle;
        heading = landRot.AsAngle;
        rollFromX = x;
        rollFromZ = z;
        rollCells = Props.LandingRollCells;
        IntVec3 end = landCell + landRot.FacingCell * rollCells;
        Vector3 c = Ext_Vehicles.TrueCenter(end, new Rot8(landRot), v.VehicleDef.Size, 0f);
        (targetX, targetZ) = (c.x, c.z);
        speed = Mathf.Max(speed, Props.speedCellsPerSecond * Props.liftOffSpeedShare);
        state = FlightState.RollingLanding;
        SyncPosition();
        VehicleWorkSounds.Play(v, "Landing");    // Framework 0.169.0
    }

    /// <summary>
    /// One tick of a rolling landing: down over the first two-fifths of the strip (authored), braking evenly to stop at its end, the strip
    /// ahead looked at every half second. Blocked, it stops hard where it is.
    /// </summary>
    void RollingLandingStep(float dt, int now)
    {
        VehiclePawn v = Vehicle;
        float dx = targetX - x, dz = targetZ - z;
        float left = Mathf.Sqrt(dx * dx + dz * dz);
        float braking = Mathf.Max(0.5f, speed * speed / (2f * Mathf.Max(0.5f, left)));
        speed = Mathf.Max(0.3f, speed - braking * dt);
        float move = Mathf.Min(left, speed * dt);
        float wasX = x, wasZ = z;
        MoveAlongHeading(move);
        // Fault-read F4: held at the map's edge it can roll no further: it stops there.
        if (move > 0.01f && Mathf.Abs(x - wasX) + Mathf.Abs(z - wasZ) < 1e-5f)
        {
            RolledIn(v.Position, hard: false, null);
            return;
        }
        climb = Mathf.Clamp01(1f - Rolled() / (0.4f * rollCells));
        if (climb <= 0f && (now + v.thingIDNumber) % 30 == 0)
        {
            int ahead = Mathf.CeilToInt(left);
            if (ahead > 0 && FlightRules.StripRefusal(v, v.Position, landRot, ahead, Props.needsBuiltFloor, out _) is { } blocked)
            {
                RolledIn(v.Position, hard: true, blocked);
                return;
            }
        }
        if (left - move <= 0.05f)
        {
            IntVec3 end = landCell + landRot.FacingCell * rollCells;
            RolledIn(end, hard: false, null);
        }
    }

    /// <summary>A rolling landing is over: on the ground at this cell, the undercarriage worn as a rolling landing wears it, or harder where it
    /// stopped short.</summary>
    void RolledIn(IntVec3 cell, bool hard, string? why)
    {
        VehiclePawn v = Vehicle;
        float m = float.IsNaN(margin) ? 0f : margin;
        landRolling = false;
        rollFromX = rollFromZ = float.NaN;
        PutDown(cell, landRot);
        LandingSurface.OnTouchDown(v, rolling: true);    // Framework 0.158.0, judged where it came to rest
        if (!VehicleSystemsMod.Settings.showOnly)
        {
            CompPhobosVehicleSystems.Of(v)?.WearLanding(hard ? (float)FlightPathRule.ForcedLandingShare(m) : LandingCheck.RunOnWearShare);
        }
        Seen.Note("aircraft.flight-landed", v, variant: hard ? "rolled-short" : "rolled");
        EnginesOffAfterLanding(v);
        if (v.Faction != Faction.OfPlayer) return;
        string text = hard
            ? "PhobosFlight_MsgRolledShort".Translate(v.LabelShort.Named("VEHICLE"), (why ?? "").Named("WHY")).Resolve()
            : "PhobosFlight_MsgRolledIn".Translate(v.LabelShort.Named("VEHICLE")).Resolve();
        Messages.Message(text, v, hard ? MessageTypeDefOf.NegativeEvent : MessageTypeDefOf.SilentInput);
    }

    float Rolled() => float.IsNaN(rollFromX) ? 0f : Mathf.Sqrt((x - rollFromX) * (x - rollFromX) + (z - rollFromZ) * (z - rollFromZ));

    void MoveAlongHeading(float distance)
    {
        double rad = heading * Math.PI / 180;
        (x, z) = Clamped(x + (float)Math.Sin(rad) * distance, z + (float)Math.Cos(rad) * distance);
        SyncPosition();
    }

    /// <summary>The descent onto the spot is over: it lands if the spot is still clear, or climbs back and says why.</summary>
    void TouchDown()
    {
        VehiclePawn v = Vehicle;
        if (FlightRules.SpotRefusal(v, landCell, landRot, forced: false, knowsGround: IsRaider) is { } blocked)
        {
            if (v.Faction == Faction.OfPlayer)
                Messages.Message("PhobosFlight_MsgSpotBlocked".Translate(v.LabelShort.Named("VEHICLE"), blocked.Named("WHY")).Resolve(), v, MessageTypeDefOf.RejectInput);
            landCell = IntVec3.Invalid;
            state = FlightState.LiftingOff;
            Seen.Note("aircraft.flight-landed", v, variant: "climbed-back");
            return;
        }
        PutDown(landCell, landRot);
        LandingSurface.OnTouchDown(v, rolling: false);    // Framework 0.158.0
        Seen.Note("aircraft.flight-landed", v, variant: "landed");
        EnginesOffAfterLanding(v);
    }

    /// <summary>The crew cut the engines in the air (Framework 0.185.0, EM29): the fall that follows is judged as one with no power at all.</summary>
    bool enginesCut;

    /// <summary>Why the engines cannot be cut now, or null: only in the air, and not while it is already coming down or rolling.</summary>
    public string? CutEnginesRefusal() =>
        !Airborne || state is FlightState.ComingDown or FlightState.TakeoffRoll or FlightState.RollingLanding
            ? "PhobosFlight_RefuseNotFlying".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve() : null;

    /// <summary>
    /// Cuts the engines in the air (Framework 0.185.0; docs/development/vehicle-emergent-play.md, EM29: the owner chose "warn, then allow" on
    /// 20 Nov 2026). Switching them off the ordinary way still lands it first (the owner's decision of Framework 0.174.0); this is the order
    /// the player gives knowingly, from its own button, whose text says what follows: it falls where it is, everyone aboard is hurt as in a
    /// fall with no power, a load on the hook drops, and over bad ground it is wrecked.
    /// </summary>
    public void CutEngines()
    {
        if (CutEnginesRefusal() != null) return;
        enginesCut = true;
        Seen.Note("aircraft.engines-cut", Vehicle);
        BeginFall("PhobosFlight_WhyEnginesCut".Translate().Resolve());
    }

    /// <summary>It cannot stay up: it comes down where it is, carrying on a little way if it was moving.</summary>
    void BeginFall(string why)
    {
        if (state == FlightState.ComingDown) return;
        VehiclePawn v = Vehicle;
        if (!HasTarget && speed > 0f)
        {
            // Framework 0.153.0: circling, it has no point ahead; it glides on along its course while it comes down.
            double stop = speed * speed / (2 * Props.accelerationCellsPerSecond);
            double rad = heading * Math.PI / 180;
            (targetX, targetZ) = Clamped((float)(x + Math.Sin(rad) * stop), (float)(z + Math.Cos(rad) * stop));
        }
        orbitX = orbitZ = float.NaN;
        // Fault-read W6: coming down ends a journey under way; it is not kept for after the landing.
        if (departNodes != null) EndLaunchSound(v);
        departNodes = null;
        departAction = null;
        // Framework 0.161.0: a load hanging below falls from where it hangs (with release and drops on).
        v.GetComp<CompPhobosSling>()?.OnFlightFalling(HeightMetresNow);
        // Framework 0.155.0: whoever is on a rope or the hoist falls from where they were (the owner's real consequences).
        if (state == FlightState.LineWork) DropLines();
        state = FlightState.ComingDown;
        landCell = IntVec3.Invalid;
        if (v.Faction == Faction.OfPlayer)
        {
            Messages.Message("PhobosFlight_MsgComingDown".Translate(v.LabelShort.Named("VEHICLE"), why.Named("WHY")).Resolve(), v, MessageTypeDefOf.ThreatSmall);
        }
    }

    /// <summary>
    /// The end of a fall: over open ground it lands hard, the undercarriage and airframe worn by how far short of a hover it was and everyone
    /// aboard jolted; over a building, impassable ground, another vehicle or deep water it is wrecked through Vehicle Framework's own
    /// destruction (owner, 18 Nov 2026: real consequences).
    /// </summary>
    void ForcedLanding()
    {
        VehiclePawn v = Vehicle;
        Rot4 rot = FlightRules.Rot4For(heading);
        IntVec3 cell = FlightRules.CellFor(v, x, z, new Rot8(rot));
        // Framework 0.185.0 (EM29): with its engines cut there is no power left to soften the fall.
        float m = enginesCut ? 0f : float.IsNaN(margin) ? 0f : margin;
        enginesCut = false;
        HurtAboard(v, (float)FlightPathRule.ForcedLandingHarm(m));
        string? wreck = FlightRules.SpotRefusal(v, cell, rot, forced: true);
        bool wasRaider = IsRaider;
        NamedArgument who = v.LabelShort.Named("VEHICLE");
        if (wreck != null || IsRaider)
        {
            if (IsRaider)
            {
                // Framework 0.157.0: a raiders' aircraft that comes down is always a wreck, never a prize (the owner's choice).
                raider = null;
                Seen.Note("aircraft.raider", v, variant: "shot-down");
                Messages.Message("PhobosFlight_MsgRaiderDown".Translate(who, (v.Faction?.Name ?? "").Named("FACTION")).Resolve(), new TargetInfo(cell, v.Map),
                    v.Faction != null && v.Faction.HostileTo(Faction.OfPlayer) ? MessageTypeDefOf.PositiveEvent : MessageTypeDefOf.NegativeEvent);
            }
            else
            {
                Seen.Note("aircraft.flight-forced", v, wreck, "wrecked");
                if (v.Faction == Faction.OfPlayer) Messages.Message("PhobosFlight_MsgWrecked".Translate(who, wreck!.Named("WHY")).Resolve(), new TargetInfo(cell, v.Map), MessageTypeDefOf.NegativeEvent);
            }
            bool prize = raider == null && !wasRaider;
            state = FlightState.Ground;
            FlightRegistry.Remove(this);
            StopTicking();
            UnloadBeforeWreck(v, cell);
            // Fault-read W9 (Framework 0.167.0): through Vehicle Framework's own destruction, its wreck left as the design says; a raiders'
            // aircraft leaves none, so it is never a prize.
            // Framework 0.187.0: with hulls left on, a raiders' aircraft still leaves none (the owner's choice: never a prize).
            using (prize ? null : VehicleHulls.Suppress()) v.Kill(null, DestroyMode.KillFinalize, spawnWreckage: prize);
            return;
        }
        PutDown(cell, rot);
        // Fault-read F8 (Framework 0.163.0): trees and bushes beneath are crushed.
        CrushPlants(v);
        LandingSurface.OnTouchDown(v, rolling: false);    // Framework 0.158.0; a wreck is judged by nothing
        CompPhobosVehicleSystems? comp = CompPhobosVehicleSystems.Of(v);
        if (comp != null && !VehicleSystemsMod.Settings.showOnly)
        {
            float share = (float)FlightPathRule.ForcedLandingShare(m);
            comp.WearLanding(share);
            comp.WearLanding(share);
        }
        Seen.Note("aircraft.flight-forced", v, variant: "hard-landing");
        if (v.Faction == Faction.OfPlayer) Messages.Message("PhobosFlight_MsgHardLanding".Translate(who).Resolve(), v, MessageTypeDefOf.NegativeEvent);
        // Framework 0.185.0 (EM23, EM13): the logbook, and a tale for everyone who walks away from it.
        VehicleStory.HardLanding(v);
        EnginesOffAfterLanding(v);
    }

    /// <summary>
    /// Sets the cargo down on standable ground beside a wreck before Vehicle Framework destroys the aircraft (Framework 0.173.0; the owner's log
    /// of 19 Nov 2026). Its destruction drops the cargo where the aircraft stands, which for a wreck is over a building or impassable ground: a
    /// chunk set down there was gone at once, the game's own drop then threw, and the aircraft's tick failed. Here the game's own placement
    /// finds the cells, from a standable one nearby, and nothing is done to a thing after it is placed. People aboard are left to Vehicle
    /// Framework, as before.
    /// </summary>
    static void UnloadBeforeWreck(VehiclePawn v, IntVec3 cell)
    {
        Map? map = v.Map;
        ThingOwner? cargo = v.inventory?.innerContainer;
        if (map == null || cargo == null || cargo.Count == 0) return;
        // Never under the aircraft: its wreck takes those cells.
        CellRect footprint = v.OccupiedRect();
        Predicate<IntVec3> clear = c => !footprint.Contains(c);
        IntVec3 root = CellFinder.StandableCellNear(cell, map, CargoDropRadius, clear);
        if (!root.IsValid) return;
        int before = cargo.Count;
        cargo.TryDropAll(root, map, ThingPlaceMode.Near, nearPlaceValidator: clear);
        if (Seen.Wants("aircraft.wreck-cargo-dropped", v)) Seen.Note("aircraft.wreck-cargo-dropped", v, (before - cargo.Count) + " of " + before + " stacks");
    }

    /// <summary>How far from a wreck its cargo may be set down, cells (authored: a few aircraft lengths).</summary>
    const float CargoDropRadius = 12f;

    static void CrushPlants(VehiclePawn v)
    {
        Map map = v.Map;
        if (map == null) return;
        foreach (IntVec3 c in v.OccupiedRect())
        {
            if (!c.InBounds(map)) continue;
            foreach (Thing thing in c.GetThingList(map).ToList())
            {
                if (thing is Plant plant && !plant.Destroyed) plant.Destroy();
            }
        }
    }

    static void HurtAboard(VehiclePawn v, float harm)
    {
        if (harm <= 0f || VehicleSystemsMod.Settings.showOnly) return;
        foreach (Pawn pawn in v.AllPawnsAboard.ToList())
        {
            if (pawn.Dead || pawn.health == null) continue;
            pawn.TakeDamage(new DamageInfo(DamageDefOf.Blunt, harm));
        }
    }

    /// <summary>
    /// The engines were switched off in the air (Framework 0.174.0; the owner's decision 1, after VTOL Vehicle): it lands on the nearest spot it
    /// may land on, within <see cref="ShutDownSearchCells"/> of where it is, and its landing then shuts the engines off. Says where it goes.
    /// False, and nothing ordered, where it may not land now (its lines out, a load on the hook, already coming down) or no spot is in reach.
    /// </summary>
    bool LandToShutDown(VehiclePawn v)
    {
        if (state is FlightState.Settling or FlightState.RollingLanding or FlightState.ComingDown || landCell.IsValid) return true;
        Rot4 rot = FlightRules.Rot4For(heading);
        IntVec3 under = FlightRules.CellFor(v, x, z, new Rot8(rot));
        IntVec3 spot = IntVec3.Invalid;
        if (LandRefusal(under, rot) == null) spot = under;
        else if (!landRolling && LinesOutRefusal() == null && v.GetComp<CompPhobosSling>() is not { Carried: not null } && !NeedsRollingLanding())
        {
            // The general refusals pass, so only the spot can be at fault: look outwards for the nearest clear one.
            foreach (IntVec3 c in GenRadial.RadialCellsAround(under, ShutDownSearchCells, useCenter: false))
            {
                if (!c.InBounds(v.Map) || FlightRules.SpotRefusal(v, c, rot, forced: false) != null) continue;
                spot = c;
                break;
            }
        }
        if (!spot.IsValid) return false;
        Land(spot, rot);
        if (!landCell.IsValid) return false;
        Messages.Message("PhobosFlight_MsgLandingToShutDown".Translate(v.LabelShort.Named("VEHICLE")).Resolve(), new TargetInfo(spot, v.Map),
            MessageTypeDefOf.TaskCompletion, historical: false);
        Seen.Note("aircraft.shutdown-lands", v);
        return true;
    }

    /// <summary>How far it looks for a spot to land on when its engines are switched off in the air, cells (authored).</summary>
    const float ShutDownSearchCells = 15f;

    /// <summary>
    /// A landing's last step, as Vehicle Framework's own landings end (Framework 0.173.0; the owner's report of 19 Nov 2026: the Cherokee
    /// never shut its engines off after landing): the brakes on and the engines off, so it stands parked, burns nothing and its rotors run
    /// down. Only the player's aircraft: a raiders' goes on thinking with its engines running. Start the engines again to take off.
    /// </summary>
    void EnginesOffAfterLanding(VehiclePawn v)
    {
        if (raider != null || v.Faction != Faction.OfPlayer || !v.Spawned) return;
        if (v.vehiclePather.Moving) v.vehiclePather.StopDead();
        if (!v.Drafted) return;
        v.ignition.Drafted = false;
        if (v.Drafted)
        {
            // Something refused or turned them on again at once; said in the verbose log, so a repeat names it.
            if (VehicleSystemsMod.Logger.DebugEnabled)
                VehicleSystemsMod.Logger.Debug(v.LabelShort + " (" + v.ThingID + ") landed but its engines stayed on: state " + state + ", pather moving "
                    + v.vehiclePather.Moving + ", lord " + (v.GetLord()?.LordJob?.GetType().Name ?? "none") + ", job " + (v.CurJobDef?.defName ?? "none"));
            return;
        }
        Seen.Note("aircraft.engines-off-after-landing", v);
    }

    /// <summary>On the ground at this cell and heading: back to Vehicle Framework's own driving and drawing.</summary>
    void PutDown(IntVec3 cell, Rot4 rot)
    {
        VehiclePawn v = Vehicle;
        EndFlightSound();    // Framework 0.174.0
        state = FlightState.Ground;
        climb = 0f;
        speed = 0f;
        ClearTarget();
        landCell = IntVec3.Invalid;
        landRolling = false;
        orbitX = orbitZ = float.NaN;
        v.Transform.rotation = 0f;
        CellRect was = v.OccupiedRect();
        v.FullRotation = new Rot8(rot);
        if (v.Position != cell) v.Position = cell;
        // Fault-read F7 (Framework 0.163.0): the ground it left and the ground it stands on now.
        RecalculatePathCost(v, was);
        RecalculatePathCost(v, v.OccupiedRect());
        hoverCosted = false;
        Vector3 c = v.TrueCenter();
        x = c.x;
        z = c.z;
        heading = rot.AsAngle;
        v.vehiclePather.ResetToCurrentPosition();
        v.DrawTracker.tweener.ResetTweenedPosToRoot();
        FlightRegistry.Remove(this);
        // A raiders' aircraft goes on thinking on the ground (Framework 0.157.0); rotors still turning run down first (Framework 0.173.0).
        if (raider == null && !RotorsWantTicks) StopTicking();
        // Framework 0.161.0: down with a load still on the hook (its release off when it came down): set down beside it.
        v.GetComp<CompPhobosSling>()?.OnFlightLanded();
    }

    // ---- Where it is ----

    /// <summary>A point brought inside the map, less half the aircraft's longest side and a cell, so no footprint cell is ever off it.</summary>
    (float X, float Z) Clamped(float px, float pz)
    {
        IntVec3 size = Vehicle.Map.Size;
        (float ex, float ez) = EdgeBounds();
        (double cx, double cz) = FlightPathRule.Clamp(px, pz, ex, size.x - ex, ez, size.z - ez);
        return ((float)cx, (float)cz);
    }

    /// <summary>How far in from each side its true centre is held: half its longest side and a cell, the same both ways so it can turn.</summary>
    (float X, float Z) EdgeBounds()
    {
        IntVec2 footprint = Vehicle.VehicleDef.Size;
        float edge = Mathf.Max(footprint.x, footprint.z) / 2f + 1f;
        return (edge, edge);
    }

    /// <summary>
    /// Puts the vehicle on the cell beneath its true centre, facing the nearest of its headings, when either changes: Vehicle Framework then
    /// claims the footprint again. The picture's turn between headings is the transform's extra rotation.
    /// </summary>
    void SyncPosition()
    {
        VehiclePawn v = Vehicle;
        CellRect was = v.OccupiedRect();
        Rot8 rot = FlightRules.RotFor(FlightPathRule.NearestOfEight(heading));
        if (v.FullRotation != rot) v.FullRotation = rot;
        IntVec3 cell = FlightRules.CellFor(v, x, z, v.FullRotation);
        if (cell != v.Position && cell.InBounds(v.Map))
        {
            v.Position = cell;
            v.vehiclePather.ResetToCurrentPosition();
        }
        // Fault-read F7: the cells it has left are worked out again, as Vehicle Framework's own mover does; those it has entered only once it
        // settles into a hover (the design: a passing aircraft slows nobody).
        CellRect now = v.OccupiedRect();
        if (now != was)
        {
            foreach (IntVec3 c in was)
            {
                if (!now.Contains(c) && c.InBounds(v.Map)) v.Map.pathing.RecalculatePerceivedPathCostAt(c);
            }
            hoverCosted = false;
        }
        ApplyTurn();
    }

    /// <summary>Works out the path cost of every cell of a rect again.</summary>
    static void RecalculatePathCost(VehiclePawn v, CellRect rect)
    {
        Map map = v.Map;
        if (map == null) return;
        foreach (IntVec3 c in rect)
        {
            if (c.InBounds(map)) map.pathing.RecalculatePerceivedPathCostAt(c);
        }
    }

    /// <summary>Settled into a hover (or at its lines), the cells beneath it slow people crossing, as any vehicle's do; worked out once.</summary>
    void HoverPathCost(VehiclePawn v)
    {
        bool hovering = state is FlightState.Hover or FlightState.LineWork;
        if (!hovering)
        {
            hoverCosted = false;
            return;
        }
        if (hoverCosted) return;
        hoverCosted = true;
        RecalculatePathCost(v, v.OccupiedRect());
    }

    /// <summary>The picture's angle away from the heading Vehicle Framework draws (read back, so four headings work as eight do).</summary>
    void ApplyTurn() => Vehicle.Transform.rotation = (float)FlightPathRule.Turn(Vehicle.FullRotation.AsAngle, heading);

    bool FootprintRoofed(IntVec3 cell, Rot4 rot)
    {
        Map map = Vehicle.Map;
        foreach (IntVec3 c in Vehicle.VehicleRect(cell, rot))
        {
            if (c.InBounds(map) && c.Roofed(map)) return true;
        }
        return false;
    }

    // ---- What it shows ----

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        if (!Vehicle.Spawned || Vehicle.Faction != Faction.OfPlayer || !Active) yield break;
        foreach (Gizmo gizmo in FlightGizmos.For(this)) yield return gizmo;
    }

    public override string CompInspectStringExtra()
    {
        if (!Airborne) return base.CompInspectStringExtra();
        return FlightText.Inspect(this);
    }

    /// <summary>Draws its shadow on the ground beneath it while it is up.</summary>
    public override void PostDraw()
    {
        base.PostDraw();
        if (Airborne)
        {
            FlightDrawing.Shadow(this);
            DrawLines();
        }
        else if (Vehicle.Spawned && Active && Find.Selector.IsSelected(Vehicle)) FlightDrawing.Strip(this);
    }

    /// <summary>
    /// How far forward the nacelles of a tiltrotor should lean now, 0 upright to 1 fully forward, from its speed over the map (for D'thorga
    /// Vehicles' tiltrotor renderer; 0 on the ground and in a hover). Flying about a map, at most half way (45 degrees); on the way out to the
    /// edge or in from it, all the way, as the design says (Framework 0.162.0, fault-read T3: before, the cap kept it to 30 degrees drawn).
    /// </summary>
    public float ForwardTiltShare => state is FlightState.Moving or FlightState.Orbit
        ? Mathf.Clamp01(speed / Props.speedCellsPerSecond) * (InTransit ? 1f : 0.5f) : 0f;

    /// <summary>Whether it is on its way out over the map's edge, or in from it (derived, not saved: after a load it reads as flying about).</summary>
    bool InTransit => Departing || inTransit || raider is { stage: RaiderStage.Leaving };

    // Set as it comes in over the edge, cleared when it first reaches a point.
    bool inTransit;

    /// <summary><see cref="ForwardTiltShare"/> for any vehicle; 0 for one without our flight.</summary>
    public static float ForwardTiltShareOf(VehiclePawn vehicle) => FlightRegistry.Get(vehicle)?.ForwardTiltShare ?? 0f;
}
