using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A raiders' aircraft (D'thorga Framework 0.157.0; docs/development/vehicle-flight-over-the-map.md, slice 6's raider half; the owner's answers
/// of 19 Nov 2026). It flies in, puts its raiders down by its plan's way, and leaves the way it came. Its orders are given here, inside the
/// flight, not through the player's buttons, so the player's refusals (fog, roofs it cannot see) are not its own. Shot down it is wrecked,
/// and with its crew lost on the ground its raiders wreck it: never a prize (the owner's choice).
/// </summary>
public partial class CompPhobosFlight
{
    const int RaiderThinkTicks = 30;
    /// <summary>Tries at a landing spot before it puts its people down the ropes where it is (authored).</summary>
    const int RaiderTries = 3;

    RaiderPlan? raider;

    /// <summary>Whether this is a raiders' aircraft on a raid: never claimable, wrecked rather than set down hard.</summary>
    public bool IsRaider => raider != null;

    /// <summary>The spot a raiders' aircraft is making for, or stands on (Framework 0.174.0): where it lands, the point it hovers over for its
    /// ropes, or, on the ground, where it stands. Invalid for any other aircraft, or one with no spot yet.</summary>
    public IntVec3 RaidSpot
    {
        get
        {
            if (raider == null || !Vehicle.Spawned) return IntVec3.Invalid;
            if (!Airborne) return Vehicle.Position;
            if (landCell.IsValid) return landCell;
            return HasTarget ? new IntVec3(Mathf.FloorToInt(targetX), 0, Mathf.FloorToInt(targetZ)) : IntVec3.Invalid;
        }
    }

    void ExposeRaider() => Scribe_Deep.Look(ref raider, "PhobosFlight_Raider");

    /// <summary>Starts a raid: just come in over the edge (FlightArrival.EnterAt), it keeps that point to leave by and goes for its spot.</summary>
    public void BeginRaid(RaidLanding mode)
    {
        raider = new RaiderPlan { mode = mode, stage = RaiderStage.Inbound, exitX = x, exitZ = z };
        Seen.Note("aircraft.raider", Vehicle, variant: "arrived");
        PlanApproach();
    }

    /// <summary>Sends it to its spot: to land there, or to hover over it for the ropes. Where none is found, the boldest way; then the ropes
    /// where it is.</summary>
    void PlanApproach()
    {
        VehiclePawn v = Vehicle;
        // Framework 0.174.0 (the audit of 19 Nov 2026, finding 6): the spots the raid's other aircraft already hold are left to them.
        List<IntVec3> taken = RaiderFlight.SpotsTaken(v);
        if (!RaiderFlight.FindZone(v, raider!.mode, out IntVec3 cell, out Rot4 rot, taken)
            && !RaiderFlight.FindZone(v, raider.mode = RaidLanding.Closest, out cell, out rot, taken)
            && !RaiderFlight.FindZone(v, raider.mode = RaidLanding.RopeClose, out cell, out rot, taken))
        {
            raider.tries = RaiderTries;
            return;
        }
        if (taken.Count > 0 && Seen.On) Seen.Note("aircraft.raid-spot-apart", v, taken.Count + " taken");
        // Framework 0.288.0: by parachute, a drop pass upwind of open ground beyond the turrets; it flies on out of the far edge.
        if (raider.mode == RaidLanding.Parachute)
        {
            BeginArrivalPass(cell, null, null, raid: true);
            Seen.Note("aircraft.raider", v, variant: "parachute");
            return;
        }
        orbitX = orbitZ = float.NaN;
        if (raider.mode == RaidLanding.RopeClose)
        {
            landCell = IntVec3.Invalid;
            Vector3 over = cell.ToVector3Shifted();
            (targetX, targetZ) = Clamped(over.x, over.z);
        }
        else
        {
            landCell = cell;
            landRot = rot;
            landRolling = false;
            Vector3 c = Ext_Vehicles.TrueCenter(cell, new Rot8(rot), v.VehicleDef.Size, 0f);
            (targetX, targetZ) = (c.x, c.z);
        }
        if (state is FlightState.Hover or FlightState.Orbit) state = FlightState.Moving;
    }

    /// <summary>Its raid, every half second: crew, then the stage it is at.</summary>
    void RaiderThink()
    {
        VehiclePawn v = Vehicle;
        RaiderPlan plan = raider!;
        if (!v.HasEnoughOperators)
        {
            // Its crew cannot fly it: in the air it comes down (and is wrecked); on the ground its raiders wreck it.
            if (!Airborne) Scuttle();
            else if (state != FlightState.ComingDown) BeginFall("PhobosFlight_WhyCrewLost".Translate().Resolve());
            return;
        }
        switch (plan.stage)
        {
            case RaiderStage.Inbound when !Airborne:
                plan.stage = RaiderStage.Unloading;
                Seen.Note("aircraft.raider", v, variant: "landed");
                break;
            case RaiderStage.Inbound when state == FlightState.Hover && !HasTarget:
                if (!Riders().Any())
                {
                    Leave();
                }
                else if (plan.mode == RaidLanding.Parachute && Dropping)
                {
                    // Still letting its raiders go.
                }
                else if (plan.mode == RaidLanding.RopeClose || plan.mode == RaidLanding.Parachute || plan.tries >= RaiderTries)
                {
                    if (LandingCell().IsValid)
                    {
                        BeginLines(LineJob.RopeDown, null);
                        Seen.Note("aircraft.raider", v, variant: "roping");
                    }
                    else if (++plan.tries > RaiderTries + 2) Leave();
                    else PlanApproach();
                }
                else
                {
                    // Its landing spot was taken from under it: another.
                    plan.tries++;
                    PlanApproach();
                }
                break;
            case RaiderStage.Inbound when state == FlightState.Orbit:
                // Fault-read R1 (Framework 0.163.0): it cannot hover to put them down here; it goes back to the edge and they walk in from there.
                Leave();
                break;
            case RaiderStage.Unloading:
                Unload();
                break;
            case RaiderStage.Leaving when state is FlightState.Hover or FlightState.Orbit:
                if (Mathf.Abs(x - plan.exitX) + Mathf.Abs(z - plan.exitZ) < 3f) Vanish();
                else
                {
                    (targetX, targetZ) = Clamped(plan.exitX, plan.exitZ);
                    state = FlightState.Moving;
                }
                break;
        }
    }

    /// <summary>On the ground: its raiders step out, two each half second; then it lifts off and leaves.</summary>
    void Unload()
    {
        VehiclePawn v = Vehicle;
        int out_ = 0;
        foreach (Pawn pawn in Riders().ToList())
        {
            v.DisembarkPawn(pawn);
            if (++out_ >= 2) return;
        }
        if (out_ > 0) return;
        BeginTakeOff();
        Leave();
    }

    /// <summary>Back to the edge it came over.</summary>
    void Leave()
    {
        RaiderPlan plan = raider!;
        plan.stage = RaiderStage.Leaving;
        landCell = IntVec3.Invalid;
        (targetX, targetZ) = Clamped(plan.exitX, plan.exitZ);
        if (state is FlightState.Hover or FlightState.Orbit) state = FlightState.Moving;
    }

    /// <summary>Off the map at the edge: its crew, and anyone still aboard, go home with it.</summary>
    void Vanish()
    {
        VehiclePawn v = Vehicle;
        Map map = v.Map;
        Seen.Note("aircraft.raider", v, variant: "left");
        // Fault-read R2 (Framework 0.163.0): raiders still aboard (it gave up) are put down at the edge and stay in their raid, walking in as
        // the game's own walk-in does; only its crew goes home with it.
        IntVec3 edge = new Vector3(raider!.exitX, 0f, raider.exitZ).ToIntVec3().ClampInsideMap(map);
        foreach (Pawn pawn in Riders().ToList())
        {
            v.RemovePawn(pawn);
            GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(edge, map, 8), map);
        }
        foreach (Pawn pawn in v.AllPawnsAboard.ToList())
        {
            v.RemovePawn(pawn);
            if (!pawn.Dead && !pawn.Destroyed) Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Decide);
        }
        raider = null;
        state = FlightState.Ground;
        FlightRegistry.Remove(this);
        StopTicking();
        v.Destroy(DestroyMode.Vanish);
    }

    /// <summary>Its crew lost on the ground: its raiders wreck it rather than leave it to be taken (the owner's choice).</summary>
    void Scuttle()
    {
        VehiclePawn v = Vehicle;
        raider = null;
        Seen.Note("aircraft.raider", v, variant: "scuttled");
        bool hostile = v.Faction != null && v.Faction.HostileTo(Faction.OfPlayer);
        Messages.Message((hostile ? "PhobosFlight_MsgRaiderScuttled" : "PhobosFlight_MsgAllyAircraftWrecked").Translate(v.LabelShort.Named("VEHICLE"),
            (v.Faction?.Name ?? "").Named("FACTION")).Resolve(), new TargetInfo(v.Position, v.Map), hostile ? MessageTypeDefOf.PositiveEvent : MessageTypeDefOf.NegativeEvent);
        StopTicking();
        v.Destroy(DestroyMode.KillFinalize);
    }
}
