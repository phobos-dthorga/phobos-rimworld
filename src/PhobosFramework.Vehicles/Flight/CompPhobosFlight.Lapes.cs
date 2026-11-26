using System;
using System.Linq;
using Phobos.Framework.Rules;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A low-altitude parachute extraction (LAPES; D'thorga Framework 0.323.0; docs/development/airdrop.md, "LAPES"; the owner's card of 26 Nov 2026
/// for the C-27J: on by default). The player picks where the platform touches down and the way it slides. The aircraft flies to a point
/// back along that line, turns onto it, comes down to a few feet (LapesRule) and, as its ramp crosses the touchdown point, its extraction chute
/// pulls one vehicle out on its platform (<see cref="PhobosLapesPlatform"/>), which slides to rest along the strip: no drift, no waiting for a
/// canopy. Then it climbs away along the line and holds. Coming in off the line it goes round, twice at most; sent elsewhere, it calls the run
/// off. With the landing risk setting on, the platform lands hard more often than by parachute, and now and then the aircraft strikes the
/// ground. Only the player's aircraft are ordered to do it: raids and allies drop by parachute.
/// </summary>
public partial class CompPhobosFlight
{
    /// <summary>How far off the line's heading it may be as it lets go, degrees (authored).</summary>
    const float LapesLineDegrees = 20f;
    /// <summary>How far to the side of the line its ramp may be as it lets go, cells (authored).</summary>
    const float LapesLineCells = 2f;
    /// <summary>How often it goes round after coming in off the line before it gives the run up (authored).</summary>
    const int LapesMostGoRounds = 2;

    /// <summary>Whether it can pull loads out low at all: its airdrop allows it and the player's setting is on.</summary>
    public bool CanLapes => VehicleSystemsMod.Settings.airdropLapes && Airdrop.Ext(Vehicle) is { lapesKg: > 0f };

    /// <summary>The first vehicle in its hold it may pull out low: within its limit, with nobody inside.</summary>
    VehiclePawn? LapesLoad()
    {
        VehiclePawn v = Vehicle;
        float limit = Airdrop.Ext(v)?.lapesKg ?? 0f;
        return Airdrop.HeldVehicles(v).FirstOrDefault(h => LapesRule.MayExtract(Airdrop.VehicleKg(h), limit, h.AllPawnsAboard.Any()));
    }

    /// <summary>The clear strip it needs ahead of the touchdown point for this load, cells.</summary>
    static int LapesStripCells(VehiclePawn? load) => LapesRule.StripCells(FlightRules.MetresPerCell, load?.VehicleDef.Size.z ?? 1);

    /// <summary>How far back along the line it turns onto it, cells: room to line up.</summary>
    float LapesApproachCells() => 2f * PassRunOn();

    /// <summary>Why it may not pull a load out low onto this point along this way, or null. An invalid cell skips the strip.</summary>
    public string? LapesRefusal(IntVec3 cell, Rot4 rot)
    {
        if (LinesOutRefusal() is { } lines) return lines;
        VehiclePawn v = Vehicle;
        NamedArgument who = v.LabelShort.Named("VEHICLE");
        if (Dropping) return "PhobosAirdrop_RefuseBusy".Translate(who).Resolve();
        if (!Airborne || state is FlightState.LiftingOff or FlightState.ComingDown or FlightState.Settling or FlightState.TakeoffRoll or FlightState.RollingLanding)
            return "PhobosFlight_RefuseNotFlying".Translate(who).Resolve();
        if (!CanLapes) return "PhobosAirdrop_RefuseLapesCannot".Translate(who).Resolve();
        if (!v.HasEnoughOperators) return "PhobosFlight_RefuseCrew".Translate(who).Resolve();
        VehiclePawn? load = LapesLoad();
        if (load == null)
            return "PhobosAirdrop_RefuseLapesNoLoad".Translate(who, (Airdrop.Ext(v)?.lapesKg ?? 0f).ToString("N0").Named("KILOGRAMS")).Resolve();
        if (Airdrop.Count(v, AirdropDefOf.Phobos_HeavyDropRig) <= 0) return "PhobosAirdrop_RefuseLapesNoRig".Translate(who).Resolve();
        if (!cell.IsValid) return null;
        if (!cell.InBounds(v.Map)) return "PhobosAirdrop_RefuseOffMap".Translate().Resolve();
        return FlightRules.StripRefusal(v, cell, rot, LapesStripCells(load), needsFloor: false, out _) is { } strip
            ? "PhobosAirdrop_RefuseLapesStrip".Translate(strip.Named("WHY")).Resolve() : null;
    }

    /// <summary>Flies back along the line, turns onto it, and pulls its load out low so it touches down at <paramref name="cell"/> and slides
    /// along <paramref name="rot"/>.</summary>
    public void LapesPass(IntVec3 cell, Rot4 rot)
    {
        if (LapesRefusal(cell, rot) is { } refused)
        {
            Messages.Message(refused, Vehicle, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        VehiclePawn v = Vehicle;
        CancelDeparture();
        landCell = IntVec3.Invalid;
        landRolling = false;
        LeaveOrbit();
        if (state == FlightState.Hover) state = FlightState.Moving;
        Vector3 start = cell.ToVector3Shifted();
        var run = new AirdropRun { lapes = true, target = cell, lineX = start.x, lineZ = start.z, lineHeading = rot.AsAngle, lapesRot = rot, platformsLeft = 1 };
        drop = run;
        AimAtLapesApproach(run);
        Seen.Note("airdrop.lapes-pass", v, variant: "ordered");
        if (v.Faction == Faction.OfPlayer && LapesLoad() is { } load)
        {
            Messages.Message("PhobosAirdrop_MsgLapesStarted".Translate(v.LabelShort.Named("VEHICLE"), load.LabelShort.Named("LOAD")).Resolve(), v,
                MessageTypeDefOf.NeutralEvent, historical: false);
        }
    }

    /// <summary>Sends it to the point back along the line where it turns onto it.</summary>
    void AimAtLapesApproach(AirdropRun run)
    {
        (float sx, float cz) = LineDirection(run);
        float back = LapesApproachCells();
        AimLapes(run, run.lineX - sx * back, run.lineZ - cz * back);
    }

    void AimLapes(AirdropRun run, float ax, float az)
    {
        (targetX, targetZ) = Clamped(ax, az);
        run.aimX = targetX;
        run.aimZ = targetZ;
    }

    static (float sx, float cz) LineDirection(AirdropRun run)
    {
        double rad = run.lineHeading * Math.PI / 180;
        return ((float)Math.Sin(rad), (float)Math.Cos(rad));
    }

    /// <summary>One tick of a LAPES run: to the approach point, onto the line, down low, and out with the load as the ramp crosses its point.</summary>
    void LapesStep(AirdropRun run, float dt)
    {
        VehiclePawn v = Vehicle;
        // Sent elsewhere by the player (another point, a circle, a hold, a landing): the run is off, the load stays aboard.
        if (!HasTarget || Mathf.Abs(targetX - run.aimX) > 0.01f || Mathf.Abs(targetZ - run.aimZ) > 0.01f)
        {
            drop = null;
            Seen.Note("airdrop.lapes-pass", v, variant: "called-off");
            if (v.Faction == Faction.OfPlayer)
                Messages.Message("PhobosAirdrop_MsgLapesCalledOff".Translate(v.LabelShort.Named("VEHICLE")).Resolve(), v, MessageTypeDefOf.NeutralEvent, historical: false);
            return;
        }
        (float sx, float cz) = LineDirection(run);
        if (!run.linedUp)
        {
            float dx = run.aimX - x, dz = run.aimZ - z;
            // Turned onto the line before it arrives, so it never stops or circles at the approach point.
            if (dx * dx + dz * dz > 4f) return;
            run.linedUp = true;
            float on = LapesStripCells(LapesLoad()) + PassRunOn();
            AimLapes(run, run.lineX + sx * on, run.lineZ + cz * on);
            return;
        }
        // Down to a few feet over the line: against the climb its flight adds while it moves, and down at its descent rate.
        float low = (float)LapesRule.HeightShare(Props.flightHeightMetres);
        climb = Mathf.Max(low, climb - dt / Mathf.Max(0.1f, Props.climbSeconds) - dt / Mathf.Max(0.1f, Props.descentSeconds));
        Vector3 ramp = RampPoint();
        float along = (ramp.x - run.lineX) * sx + (ramp.z - run.lineZ) * cz;
        if (along < 0f) return;
        float side = Mathf.Abs((ramp.x - run.lineX) * cz - (ramp.z - run.lineZ) * sx);
        float off = Mathf.Abs((float)FlightPathRule.Turn(heading, run.lineHeading));
        if (off > LapesLineDegrees || side > LapesLineCells)
        {
            LapesGoRound(run);
            return;
        }
        LapesRelease(run, ramp);
    }

    /// <summary>Came in off the line: round again from the approach point, or the run given up after the most go-rounds.</summary>
    void LapesGoRound(AirdropRun run)
    {
        VehiclePawn v = Vehicle;
        run.goRounds++;
        Seen.Note("airdrop.lapes-pass", v, variant: "go-round");
        if (run.goRounds > LapesMostGoRounds)
        {
            EndDrop("PhobosAirdrop_MsgLapesGaveUp");
            return;
        }
        run.linedUp = false;
        AimAtLapesApproach(run);
        if (v.Faction == Faction.OfPlayer)
            Messages.Message("PhobosAirdrop_MsgLapesGoRound".Translate(v.LabelShort.Named("VEHICLE")).Resolve(), v, MessageTypeDefOf.CautionInput, historical: false);
    }

    /// <summary>Out with the load: its rig used, the platform sliding from the ramp; a hard landing and a ground strike rolled; then it climbs
    /// away along the line.</summary>
    void LapesRelease(AirdropRun run, Vector3 ramp)
    {
        VehiclePawn v = Vehicle;
        Map map = v.Map;
        VehiclePawn? load = LapesLoad();
        if (load == null || Airdrop.Count(v, AirdropDefOf.Phobos_HeavyDropRig) <= 0 || !Airdrop.TakeHeld(v, load))
        {
            EndDrop("PhobosAirdrop_MsgLapesNothing");
            return;
        }
        Airdrop.UseOne(v, AirdropDefOf.Phobos_HeavyDropRig);
        bool risky = ParachuteLanding.Risky;
        bool hard = risky && Rand.Chance((float)LapesRule.HardLandingChance);
        PhobosLapesPlatform.Release(map, load, ramp, run.lapesRot, hard);
        run.vehicles++;
        run.platformsLeft = 0;
        bool struck = risky && Rand.Chance((float)LapesRule.GroundStrikeChance);
        Seen.Note("airdrop.lapes-release", v, variant: struck ? "struck" : "clean");
        drop = null;
        SayDropDone(run);
        if (struck) LapesGroundStrike(v);
    }

    /// <summary>It touched the ground as the load went: its landing gear and belly worn, now and then worse; it flies on.</summary>
    void LapesGroundStrike(VehiclePawn v)
    {
        CompPhobosVehicleSystems.Of(v)?.WearLanding((float)LapesRule.StrikeWearShare);
        string? harm = Rand.Chance((float)LapesRule.SevereStrikeShare) ? SlingDrops.Fall(v, (float)ParachuteRule.HardLandingMetres) : null;
        FleckMaker.ThrowDustPuffThick(RampPoint(), v.Map, 3f, new Color(0.6f, 0.55f, 0.45f));
        if (v.Faction != Faction.OfPlayer) return;
        string text = harm.NullOrEmpty()
            ? "PhobosAirdrop_MsgLapesStrike".Translate(v.LabelShort.Named("VEHICLE")).Resolve()
            : "PhobosAirdrop_MsgLapesStrikeHarm".Translate(v.LabelShort.Named("VEHICLE"), harm!.Named("HARM")).Resolve();
        Messages.Message(text, v, MessageTypeDefOf.NegativeEvent);
    }
}
