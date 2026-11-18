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
    /// <summary>Its speed leaving the ground at the end of a roll, as a share of its top speed over the map (authored).</summary>
    public float liftOffSpeedShare = 0.6f;

    public CompProperties_PhobosFlight() => compClass = typeof(CompPhobosFlight);

    public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        foreach (string error in base.ConfigErrors(parentDef)) yield return error;
        if (speedCellsPerSecond <= 0f || accelerationCellsPerSecond <= 0f || turnDegreesPerSecond <= 0f) yield return "flight speeds must be positive";
        if (climbSeconds <= 0f || descentSeconds <= 0f || fallSeconds <= 0f) yield return "flight climb, descent and fall times must be positive";
        if (shortestRollCells < 1 || fullRollCells < shortestRollCells) yield return "a roll needs shortestRollCells of at least 1 and fullRollCells no shorter";
    }
}

/// <summary>
/// The aircraft stays an ordinary Vehicle Framework vehicle of the stock class, spawned on the cell beneath it; this comp moves it, keeps
/// its true position and heading, and decides what it may do from the engine model's one hover margin. It ticks only while airborne
/// (Vehicle Framework's tick by request), so a parked aircraft costs nothing. Orders are services here; the gizmos only call them.
/// </summary>
public class CompPhobosFlight : VehicleComp
{
    const float TicksPerSecond = 60f;
    const int JudgeEveryTicks = 60;
    const int IdleResetTicks = 250;

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

    // Derived, never saved.
    float speed;
    float margin = float.NaN;
    FlightHold hold = FlightHold.Hover;
    bool redraftTold;

    public CompProperties_PhobosFlight Props => (CompProperties_PhobosFlight)props;

    public FlightState State => state;
    public bool Airborne => state != FlightState.Ground;

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
    float HeightCells => Props.flightHeightMetres / FlightRules.MetresPerCell;

    // ---- Saving ----

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref state, "PhobosFlight_State", FlightState.Ground);
        Scribe_Values.Look(ref x, "PhobosFlight_X");
        Scribe_Values.Look(ref z, "PhobosFlight_Z");
        Scribe_Values.Look(ref heading, "PhobosFlight_Heading");
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
        if (Scribe.mode == LoadSaveMode.PostLoadInit && departNodes is { Count: 0 }) departNodes = null;
    }

    /// <summary>Whether it is on its way to the map's edge to leave for another place.</summary>
    public bool Departing => departNodes != null;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (!Airborne) return;
        // An aircraft spawned in the air: after a load (Transform's turn is not saved) or on the map it was on. Never from anywhere else.
        FlightRegistry.Add(this);
        ApplyTurn();
        StartTicking();
    }

    public override void OnDeSpawn()
    {
        base.OnDeSpawn();
        FlightRegistry.Remove(this);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        FlightRegistry.Remove(this);
    }

    // ---- Orders: each says what it did, or why not ----

    /// <summary>Why it may not lift straight up from where it stands now, or null. Too heavy to lift straight up but able to roll, it can
    /// leave only for another place, since it could hold nowhere on the map until it can circle (the design's slice 4).</summary>
    public string? TakeOffRefusal()
    {
        if (GroundRefusal() is { } ground) return ground;
        (RollVerdict verdict, int cells, float m) = RollNeed();
        NamedArgument who = Vehicle.LabelShort.Named("VEHICLE");
        return verdict switch
        {
            RollVerdict.Rolls => "PhobosFlight_RefuseRollOnlyLeaving".Translate(who, cells.Named("CELLS")).Resolve(),
            RollVerdict.Refused => TooHeavy(m),
            _ => null,
        };
    }

    /// <summary>
    /// How it would leave the ground now (Framework 0.151.0): straight up when it can hover, or when the player's setting lets it past the
    /// limit; else a roll along a strip of so many cells, or not at all (RollRule). The margin judged.
    /// </summary>
    public (RollVerdict Verdict, int Cells, float Margin) RollNeed()
    {
        if (!FlightRules.HoverRefused(Vehicle, out float m)) return (RollVerdict.Straight, 0, m);
        RollVerdict verdict = RollRule.Judge(m, RollingShare);
        return (verdict, verdict == RollVerdict.Rolls ? RollRule.StripCells(m, RollingShare, Props.shortestRollCells, Props.fullRollCells) : 0, m);
    }

    /// <summary>Too heavy even to roll: how much lighter it must be, where it can roll at all.</summary>
    string TooHeavy(float m)
    {
        VehiclePawn v = Vehicle;
        NamedArgument who = v.LabelShort.Named("VEHICLE");
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
        if (CompPhobosVehicleSystems.Of(v)?.LaunchRefusal() is { } systems) return systems;
        if (LaunchChecks.Refusal(v) is { } other) return other;
        return null;
    }

    /// <summary>Lifts straight up from where it stands.</summary>
    public void TakeOff()
    {
        if (TakeOffRefusal() is { } refused)
        {
            Messages.Message(refused, Vehicle, MessageTypeDefOf.RejectInput, historical: false);
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
        Seen.Note("aircraft.flight-takeoff", v);
    }

    /// <summary>Starts a roll along the strip ahead, straight along its heading of four, to leave the ground at the end (Framework 0.151.0).</summary>
    void BeginRoll(int cells)
    {
        VehiclePawn v = Vehicle;
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
        Seen.Note("aircraft.flight-roll", v, variant: "takeoff");
    }

    /// <summary>Why it may not set off for another place from where it is now, or null: on the ground, why it may not take off; in the air,
    /// a steady flight and its crew and fuel (Vehicle Framework judged it as standing on the ground).</summary>
    public string? DepartRefusal()
    {
        if (!Airborne)
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
        VehiclePawn v = Vehicle;
        NamedArgument who = v.LabelShort.Named("VEHICLE");
        if (state is FlightState.LiftingOff or FlightState.ComingDown or FlightState.Settling) return "PhobosFlight_RefuseNotFlying".Translate(who).Resolve();
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
        if (state == FlightState.Hover) state = FlightState.Moving;
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
        Messages.Message("PhobosFlight_MsgDepartureCancelled".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(), Vehicle, MessageTypeDefOf.SilentInput, historical: false);
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
        state = FlightState.Ground;
        climb = 0f;
        speed = 0f;
        ClearTarget();
        v.Transform.rotation = 0f;
        FlightRegistry.Remove(this);
        StopTicking();
        Seen.Note("aircraft.flight-departed", v);
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
        NamedArgument who = Vehicle.LabelShort.Named("VEHICLE");
        if (!Airborne || state is FlightState.LiftingOff or FlightState.ComingDown or FlightState.TakeoffRoll or FlightState.RollingLanding)
            return "PhobosFlight_RefuseNotFlying".Translate(who).Resolve();
        if (!cell.InBounds(Vehicle.Map)) return "PhobosFlight_RefuseOffMap".Translate(who).Resolve();
        // Framework 0.151.0: one that cannot hover could only come down where it stopped, until it can circle (slice 5): it keeps on its
        // way, or lands rolling.
        if (hold == FlightHold.ForwardOnly) return "PhobosFlight_RefuseCannotHoverFlyTo".Translate(who).Resolve();
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
        if (state is FlightState.Hover or FlightState.Settling)
        {
            if (state == FlightState.Settling) state = FlightState.LiftingOff;
            else state = FlightState.Moving;
        }
        FleckMaker.Static(cell, Vehicle.Map, FleckDefOf.FeedbackGoto);
    }

    /// <summary>Whether it is flying somewhere it could be told to stop.</summary>
    public bool CanHold => state == FlightState.Moving;

    /// <summary>Stops where it is and hovers, or says why not.</summary>
    public void Hold()
    {
        if (state != FlightState.Moving)
        {
            Messages.Message("PhobosFlight_RefuseNotMoving".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(), Vehicle, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        if (hold != FlightHold.Hover)
        {
            Messages.Message("PhobosFlight_RefuseCannotHover".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(), Vehicle, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        CancelDeparture();
        // Brake to rest where it can stop: the point its present speed carries it to.
        double stop = speed * speed / (2 * Props.accelerationCellsPerSecond);
        double rad = heading * Math.PI / 180;
        (targetX, targetZ) = Clamped((float)(x + Math.Sin(rad) * stop), (float)(z + Math.Cos(rad) * stop));
    }

    /// <summary>Why it may not land on this cell facing this way, or null.</summary>
    public string? LandRefusal(IntVec3 cell, Rot4 rot)
    {
        NamedArgument who = Vehicle.LabelShort.Named("VEHICLE");
        if (!Airborne || state is FlightState.ComingDown or FlightState.TakeoffRoll or FlightState.RollingLanding)
            return "PhobosFlight_RefuseNotFlying".Translate(who).Resolve();
        if (NeedsRollingLanding())
        {
            // Framework 0.151.0: it cannot hover, so it lands on a strip of its full roll ahead of the spot, facing as asked.
            if (float.IsNaN(RollingShare)) return "PhobosFlight_RefuseCannotHoverNoStrip".Translate(who).Resolve();
            return FlightRules.StripRefusal(Vehicle, cell, rot, Props.fullRollCells, Props.needsBuiltFloor, out _) is { } strip
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
        if (state is FlightState.Hover) state = FlightState.Moving;
    }

    // ---- Each tick in the air ----

    public override void CompTick()
    {
        base.CompTick();
        VehiclePawn v = Vehicle;
        if (!v.Spawned || !Airborne)
        {
            StopTicking();
            FlightRegistry.Remove(this);
            return;
        }
        int now = Find.TickManager.TicksGame;
        float dt = 1f / TicksPerSecond;
        if (v.vehiclePather.Moving) v.vehiclePather.StopDead();
        if ((now + v.thingIDNumber) % IdleResetTicks == 0) v.ResetIdleTicks();
        if (state != FlightState.ComingDown && Engines(v)) Judge(v, now);
        switch (state)
        {
            case FlightState.TakeoffRoll:
                RollStep(dt, now);
                break;
            case FlightState.RollingLanding:
                RollingLandingStep(dt, now);
                break;
            case FlightState.LiftingOff:
                climb = Mathf.Min(1f, climb + dt / Props.climbSeconds);
                if (climb >= 1f) state = HasTarget ? FlightState.Moving : FlightState.Hover;
                break;
            case FlightState.Hover:
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
        if (!v.Drafted)
        {
            // Stopping the engines in the air cannot be refused through Vehicle Framework: they are started again at once.
            v.ignition.Drafted = true;
            if (!redraftTold && v.Faction == Faction.OfPlayer)
            {
                redraftTold = true;
                Messages.Message("PhobosFlight_MsgEnginesKept".Translate(v.LabelShort.Named("VEHICLE")).Resolve(), v, MessageTypeDefOf.RejectInput, historical: false);
            }
        }
        else redraftTold = false;
        return true;
    }

    /// <summary>Judges what the engines leave it able to do, once a second: the engine model's hover margin read three ways.</summary>
    void Judge(VehiclePawn v, int now)
    {
        if ((now + v.thingIDNumber) % JudgeEveryTicks != 0) return;
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
            BeginFall("PhobosFlight_WhyEngines".Translate().Resolve());
            return;
        }
        if (hold == FlightHold.ForwardOnly && state is FlightState.Hover or FlightState.LiftingOff or FlightState.Settling)
        {
            // It cannot hold a hover: it comes down where it is (the engine lost in a hover). Moving, it keeps flying (below).
            BeginFall("PhobosFlight_WhyCannotHover".Translate().Resolve());
        }
        if (margin < 1f && hold == FlightHold.Hover && VehicleSystemsMod.Settings.hoverLimit == VehicleSystemsSettings.HoverLimit.AllowWithDamage
            && CompPhobosVehicleSystems.Of(v) is { } comp)
        {
            // Allowed at a cost: each second past the limit strains the engines as an over-limit lift does.
            HoverLimits.Strain(comp);
        }
    }

    /// <summary>One tick of flight toward the target: turn, speed up or brake, move, and arrive.</summary>
    void Step(float dt)
    {
        if (!HasTarget)
        {
            speed = 0f;
            if (state == FlightState.Moving) state = FlightState.Hover;
            return;
        }
        float dx = targetX - x, dz = targetZ - z;
        float distance = Mathf.Sqrt(dx * dx + dz * dz);
        double wanted = FlightPathRule.HeadingTo(dx, dz, heading);
        heading = (float)FlightPathRule.TurnToward(heading, wanted, Props.turnDegreesPerSecond * dt);
        double share = FlightPathRule.TurnSpeedShare(FlightPathRule.Turn(heading, wanted));
        bool mustKeepMoving = hold == FlightHold.ForwardOnly && state == FlightState.Moving;
        // An aircraft that cannot hover flies its leg through at speed; it does not brake to stop where it cannot hold.
        speed = (float)FlightPathRule.NextSpeed(speed, mustKeepMoving ? 1e6 : distance, Props.speedCellsPerSecond * share, Props.accelerationCellsPerSecond, dt);
        float move = speed * dt;
        if (move >= distance)
        {
            x = targetX;
            z = targetZ;
            Arrive();
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
        if (state == FlightState.ComingDown) return;
        // Leaving for another place: at the edge it hands over, whether or not it could hover (Framework 0.149.0).
        if (Departing)
        {
            HandOver();
            return;
        }
        // Framework 0.151.0: one that cannot hover lands rolling on a clear strip ahead of its spot; chosen while it could hover, the strip
        // is looked at now.
        if (landCell.IsValid && (landRolling || (hold == FlightHold.ForwardOnly && !float.IsNaN(RollingShare)
            && FlightRules.StripRefusal(Vehicle, landCell, landRot, Props.fullRollCells, Props.needsBuiltFloor, out _) == null)))
        {
            BeginRollingLanding();
            return;
        }
        speed = 0f;
        ClearTarget();
        if (hold == FlightHold.ForwardOnly)
        {
            BeginFall("PhobosFlight_WhyCannotHover".Translate().Resolve());
            return;
        }
        if (landCell.IsValid)
        {
            heading = landRot.AsAngle;
            state = FlightState.Settling;
            SyncPosition();
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
        MoveAlongHeading(speed * dt);
        float rolled = Rolled();
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
        state = HasTarget ? FlightState.Moving : FlightState.Hover;
        Seen.Note("aircraft.flight-roll", Vehicle, variant: "lifted-off");
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
        rollCells = Props.fullRollCells;
        IntVec3 end = landCell + landRot.FacingCell * rollCells;
        Vector3 c = Ext_Vehicles.TrueCenter(end, new Rot8(landRot), v.VehicleDef.Size, 0f);
        (targetX, targetZ) = (c.x, c.z);
        speed = Mathf.Max(speed, Props.speedCellsPerSecond * Props.liftOffSpeedShare);
        state = FlightState.RollingLanding;
        SyncPosition();
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
        MoveAlongHeading(move);
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
        if (!VehicleSystemsMod.Settings.showOnly)
        {
            CompPhobosVehicleSystems.Of(v)?.WearLanding(hard ? (float)FlightPathRule.ForcedLandingShare(m) : LandingCheck.RunOnWearShare);
        }
        Seen.Note("aircraft.flight-landed", v, variant: hard ? "rolled-short" : "rolled");
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
        if (FlightRules.SpotRefusal(v, landCell, landRot, forced: false) is { } blocked)
        {
            Messages.Message("PhobosFlight_MsgSpotBlocked".Translate(v.LabelShort.Named("VEHICLE"), blocked.Named("WHY")).Resolve(), v, MessageTypeDefOf.RejectInput);
            landCell = IntVec3.Invalid;
            state = FlightState.LiftingOff;
            Seen.Note("aircraft.flight-landed", v, variant: "climbed-back");
            return;
        }
        PutDown(landCell, landRot);
        Seen.Note("aircraft.flight-landed", v, variant: "landed");
    }

    /// <summary>It cannot stay up: it comes down where it is, carrying on a little way if it was moving.</summary>
    void BeginFall(string why)
    {
        if (state == FlightState.ComingDown) return;
        VehiclePawn v = Vehicle;
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
        float m = float.IsNaN(margin) ? 0f : margin;
        HurtAboard(v, (float)FlightPathRule.ForcedLandingHarm(m));
        string? wreck = FlightRules.SpotRefusal(v, cell, rot, forced: true);
        NamedArgument who = v.LabelShort.Named("VEHICLE");
        if (wreck != null)
        {
            Seen.Note("aircraft.flight-forced", v, wreck, "wrecked");
            if (v.Faction == Faction.OfPlayer) Messages.Message("PhobosFlight_MsgWrecked".Translate(who, wreck.Named("WHY")).Resolve(), new TargetInfo(cell, v.Map), MessageTypeDefOf.NegativeEvent);
            state = FlightState.Ground;
            FlightRegistry.Remove(this);
            StopTicking();
            v.Destroy(DestroyMode.KillFinalize);
            return;
        }
        PutDown(cell, rot);
        CompPhobosVehicleSystems? comp = CompPhobosVehicleSystems.Of(v);
        if (comp != null && !VehicleSystemsMod.Settings.showOnly)
        {
            float share = (float)FlightPathRule.ForcedLandingShare(m);
            comp.WearLanding(share);
            comp.WearLanding(share);
        }
        Seen.Note("aircraft.flight-forced", v, variant: "hard-landing");
        if (v.Faction == Faction.OfPlayer) Messages.Message("PhobosFlight_MsgHardLanding".Translate(who).Resolve(), v, MessageTypeDefOf.NegativeEvent);
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

    /// <summary>On the ground at this cell and heading: back to Vehicle Framework's own driving and drawing.</summary>
    void PutDown(IntVec3 cell, Rot4 rot)
    {
        VehiclePawn v = Vehicle;
        state = FlightState.Ground;
        climb = 0f;
        speed = 0f;
        ClearTarget();
        landCell = IntVec3.Invalid;
        landRolling = false;
        v.Transform.rotation = 0f;
        v.FullRotation = new Rot8(rot);
        if (v.Position != cell) v.Position = cell;
        Vector3 c = v.TrueCenter();
        x = c.x;
        z = c.z;
        heading = rot.AsAngle;
        v.vehiclePather.ResetToCurrentPosition();
        v.DrawTracker.tweener.ResetTweenedPosToRoot();
        FlightRegistry.Remove(this);
        StopTicking();
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
        Rot8 rot = FlightRules.RotFor(FlightPathRule.NearestOfEight(heading));
        if (v.FullRotation != rot) v.FullRotation = rot;
        IntVec3 cell = FlightRules.CellFor(v, x, z, v.FullRotation);
        if (cell != v.Position && cell.InBounds(v.Map))
        {
            v.Position = cell;
            v.vehiclePather.ResetToCurrentPosition();
        }
        ApplyTurn();
    }

    /// <summary>The picture's angle away from the heading Vehicle Framework draws (read back, so four headings work as eight do).</summary>
    void ApplyTurn() => Vehicle.Transform.rotation = (float)FlightPathRule.Turn(Vehicle.FullRotation.AsAngle, heading);

    bool FootprintRoofed(IntVec3 cell, Rot4 rot)
    {
        Map map = Vehicle.Map;
        foreach (IntVec3 c in GenAdj.OccupiedRect(cell, rot, Vehicle.VehicleDef.Size))
        {
            if (c.InBounds(map) && c.Roofed(map)) return true;
        }
        return false;
    }

    // ---- What it shows ----

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        if (!Vehicle.Spawned || Vehicle.Faction != Faction.OfPlayer) yield break;
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
        if (Airborne) FlightDrawing.Shadow(this);
        else if (Vehicle.Spawned && Find.Selector.IsSelected(Vehicle)) FlightDrawing.Strip(this);
    }

    /// <summary>How far forward the nacelles of a tiltrotor should lean now, 0 upright to 1 fully forward, from its speed over the map
    /// (for D'thorga Vehicles' tiltrotor renderer; 0 on the ground and in a hover).</summary>
    public float ForwardTiltShare => state == FlightState.Moving ? Mathf.Clamp01(speed / Props.speedCellsPerSecond) * 0.5f : 0f;

    /// <summary><see cref="ForwardTiltShare"/> for any vehicle; 0 for one without our flight.</summary>
    public static float ForwardTiltShareOf(VehiclePawn vehicle) => FlightRegistry.Get(vehicle)?.ForwardTiltShare ?? 0f;
}
