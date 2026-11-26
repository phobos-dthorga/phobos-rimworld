using Phobos.Framework.Rules;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A water drop pass from a roll-on fire-fighting tank (Framework 0.329.0; docs/development/vehicle-c27j.md, "The upgrade tree";
/// <see cref="CompPhobosFireTank"/>). The player picks a point; the aircraft flies a straight line through it from where it is and, over a run
/// centred on the point, empties its tank onto a strip a few cells wide, putting out the fires beneath as the helicopter's bucket does
/// (<see cref="WaterDrops.Douse"/>, the same litres to a cell). Then it flies on and holds. The run is as long as the water: a full C-27J tank,
/// 7,950 L at 100 L a cell and 3 cells wide, wets about 26 cells. Sent elsewhere, it calls the pass off with what is left aboard.
/// </summary>
public partial class CompPhobosFlight
{
    CompPhobosFireTank? FireTank => Vehicle.GetComp<CompPhobosFireTank>();

    /// <summary>Whether it carries a fire-fighting tank (fitted by a node).</summary>
    public bool CanWaterPass => FireTank is { Fitted: true };

    static float LitresPerCell => PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>().waterDropLitresPerCell;

    /// <summary>Why it may not make a water drop pass over this point now, or null. An invalid cell skips the point.</summary>
    public string? WaterPassRefusal(IntVec3 cell)
    {
        if (LinesOutRefusal() is { } lines) return lines;
        VehiclePawn v = Vehicle;
        NamedArgument who = v.LabelShort.Named("VEHICLE");
        if (Dropping) return "PhobosAirdrop_RefuseBusy".Translate(who).Resolve();
        if (!Airborne || state is FlightState.LiftingOff or FlightState.ComingDown or FlightState.Settling or FlightState.TakeoffRoll or FlightState.RollingLanding)
            return "PhobosFlight_RefuseNotFlying".Translate(who).Resolve();
        if (FireTank is not { Fitted: true } tank) return "PhobosFireTank_RefuseNone".Translate(who).Resolve();
        if (tank.Litres < LitresPerCell) return "PhobosFireTank_RefuseEmpty".Translate(who).Resolve();
        if (cell.IsValid && !cell.InBounds(v.Map)) return "PhobosAirdrop_RefuseOffMap".Translate().Resolve();
        return null;
    }

    /// <summary>Flies a straight line through the point, emptying its tank over a run centred on it, and flies on.</summary>
    public void WaterPass(IntVec3 cell)
    {
        if (WaterPassRefusal(cell) is { } refused)
        {
            Messages.Message(refused, Vehicle, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        VehiclePawn v = Vehicle;
        CompPhobosFireTank tank = FireTank!;
        CancelDeparture();
        landCell = IntVec3.Invalid;
        landRolling = false;
        LeaveOrbit();
        if (state == FlightState.Hover) state = FlightState.Moving;
        Vector3 c = cell.ToVector3Shifted();
        float along = (float)FlightPathRule.HeadingTo(c.x - x, c.z - z, heading);
        float width = tank.Props.dropWidthCells;
        var run = new AirdropRun
        {
            water = true, linedUp = true, target = cell, lineX = c.x, lineZ = c.z, lineHeading = along,
            waterHalf = Mathf.Max(1f, tank.Litres / (LitresPerCell * width) / 2f),
        };
        drop = run;
        (float sx, float cz) = LineDirection(run);
        float on = run.waterHalf + PassRunOn();
        AimLapes(run, c.x + sx * on, c.z + cz * on);
        Seen.Note("aircraft.water-pass", v, variant: "ordered");
        if (v.Faction == Faction.OfPlayer)
        {
            Messages.Message("PhobosFireTank_MsgPassStarted".Translate(v.LabelShort.Named("VEHICLE"), Mathf.RoundToInt(tank.Litres).ToString("N0").Named("LITRES"),
                Mathf.RoundToInt(run.waterHalf * 2f).Named("CELLS")).Resolve(), v, MessageTypeDefOf.NeutralEvent, historical: false);
        }
    }

    /// <summary>One tick of a water drop pass: each whole cell of the run the spray point has passed is wetted, the tank's water going with it.</summary>
    void WaterStep(AirdropRun run)
    {
        VehiclePawn v = Vehicle;
        // Sent elsewhere by the player: the pass is off, the water stays aboard.
        if (!HasTarget || Mathf.Abs(targetX - run.aimX) > 0.01f || Mathf.Abs(targetZ - run.aimZ) > 0.01f)
        {
            drop = null;
            Seen.Note("aircraft.water-pass", v, variant: "called-off");
            if (v.Faction == Faction.OfPlayer)
                Messages.Message("PhobosFireTank_MsgPassCalledOff".Translate(v.LabelShort.Named("VEHICLE")).Resolve(), v, MessageTypeDefOf.NeutralEvent, historical: false);
            return;
        }
        CompPhobosFireTank? tank = FireTank;
        (float sx, float cz) = LineDirection(run);
        // The spray leaves the aft door: the ground behind the aircraft's middle (the ramp's point).
        Vector3 spray = RampPoint();
        float along = (spray.x - run.lineX) * sx + (spray.z - run.lineZ) * cz;
        if (along < -run.waterHalf) return;
        float from = float.IsNaN(run.wetTo) ? -run.waterHalf : run.wetTo;
        float to = Mathf.Min(along, run.waterHalf);
        float width = tank?.Props.dropWidthCells ?? 3;
        Map map = v.Map;
        for (float s = from; s <= to && tank is { Litres: > 0f }; s += 1f)
        {
            run.litresDropped += tank.Draw(LitresPerCell * width);
            var at = new Vector3(run.lineX + sx * s, 0f, run.lineZ + cz * s);
            run.firesOut += WaterDrops.Douse(map, at.ToIntVec3().ClampInsideMap(map), width / 2f);
            run.wetTo = s + 1f;
        }
        if (along >= run.waterHalf || tank is not { Litres: > 0f }) FinishWaterPass(run);
    }

    /// <summary>The run is wetted or the tank is dry: it says what it did and flies on to hold.</summary>
    void FinishWaterPass(AirdropRun run)
    {
        VehiclePawn v = Vehicle;
        drop = null;
        if (Seen.Wants("aircraft.water-pass", v, "dropped"))
        {
            Seen.Note("aircraft.water-pass", v, variant: "dropped", values: new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, double>>
            {
                new("litres", run.litresDropped), new("fires", run.firesOut),
            });
        }
        if (v.Faction != Faction.OfPlayer) return;
        Messages.Message("PhobosFireTank_MsgPassDone".Translate(v.LabelShort.Named("VEHICLE"), Mathf.RoundToInt(run.litresDropped).ToString("N0").Named("LITRES"),
            run.firesOut.Named("FIRES")).Resolve(), v, MessageTypeDefOf.TaskCompletion, historical: false);
    }
}
