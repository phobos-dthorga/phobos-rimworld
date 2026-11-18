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
        if (!RaiderFlight.FindZone(v, raider!.mode, out IntVec3 cell, out Rot4 rot)
            && !RaiderFlight.FindZone(v, raider.mode = RaidLanding.Closest, out cell, out rot)
            && !RaiderFlight.FindZone(v, raider.mode = RaidLanding.RopeClose, out cell, out rot))
        {
            raider.tries = RaiderTries;
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
                else if (plan.mode == RaidLanding.RopeClose || plan.tries >= RaiderTries)
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
        Seen.Note("aircraft.raider", v, variant: "left");
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
        Messages.Message("PhobosFlight_MsgRaiderScuttled".Translate(v.LabelShort.Named("VEHICLE"), (v.Faction?.Name ?? "").Named("FACTION")).Resolve(), new TargetInfo(v.Position, v.Map), MessageTypeDefOf.PositiveEvent);
        StopTicking();
        v.Destroy(DestroyMode.KillFinalize);
    }
}
