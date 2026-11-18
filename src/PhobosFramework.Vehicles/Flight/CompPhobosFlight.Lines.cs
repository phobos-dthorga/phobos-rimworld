using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>What an aircraft is doing at its lines (Framework 0.155.0): putting people down the ropes, or working the hoist one way.</summary>
public enum LineJob
{
    None,
    RopeDown,
    HoistUp,
    HoistDown,
}

/// <summary>
/// Ropes and a hoist (D'thorga Framework 0.155.0; docs/development/vehicle-flight-over-the-map.md, slice 6, with the owner's four answers of
/// 19 Nov 2026). From a steady hover the aircraft comes down to its work height, puts people down its fast ropes two at a time or works its
/// rescue hoist one person at a time, then climbs back. Whoever is on a line is held here, off the map and out of the seats; if the aircraft
/// must come down while anyone is, they fall from the height they were at (the owner's "real consequences").
/// </summary>
public partial class CompPhobosFlight : IThingHolder
{
    /// <summary>The abilities the two fittings grant.</summary>
    public const string FastRopeAbility = "FastRope";
    public const string RescueHoistAbility = "RescueHoist";

    const int LineCheckTicks = 30;

    // Saved.
    LineJob lineJob;
    bool lineWorking, lineStopping;
    ThingOwner<Pawn> lineHeld;
    // The metres each person on a rope still has to slide, in the order they are held.
    List<float> ropeLeft = new();
    // The hoist: cable paid out (m), the hook-up's seconds left (NaN when not hooking), whoever it is sent for.
    float cableOut, hookLeft = float.NaN;
    Pawn? hoistFor;
    bool hoistRaising, hoistUnpoweredTold;

    public CompPhobosFlight() => lineHeld = new ThingOwner<Pawn>(this);

    public ThingOwner GetDirectlyHeldThings() => lineHeld;

    public void GetChildHolders(List<IThingHolder> outChildren) => ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, lineHeld);

    void ExposeLines()
    {
        Scribe_Values.Look(ref lineJob, "PhobosFlight_LineJob");
        Scribe_Values.Look(ref lineWorking, "PhobosFlight_LineWorking");
        Scribe_Values.Look(ref lineStopping, "PhobosFlight_LineStopping");
        Scribe_Deep.Look(ref lineHeld, "PhobosFlight_LineHeld", this);
        Scribe_Collections.Look(ref ropeLeft, "PhobosFlight_RopeLeft", LookMode.Value);
        Scribe_Values.Look(ref cableOut, "PhobosFlight_CableOut");
        Scribe_Values.Look(ref hookLeft, "PhobosFlight_HookLeft", float.NaN);
        Scribe_References.Look(ref hoistFor, "PhobosFlight_HoistFor");
        Scribe_Values.Look(ref hoistRaising, "PhobosFlight_HoistRaising");
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            lineHeld ??= new ThingOwner<Pawn>(this);
            ropeLeft ??= new List<float>();
            while (ropeLeft.Count < lineHeld.Count) ropeLeft.Add(0f);
        }
    }

    CompPhobosVehicleSystems? Systems => CompPhobosVehicleSystems.Of(Vehicle);

    /// <summary>Whether its fast-rope bar is fitted.</summary>
    public bool CanRope => Systems?.Fittings.Grants(FastRopeAbility) == true;

    /// <summary>Whether its rescue hoist is fitted.</summary>
    public bool HasHoist => Systems?.Fittings.Grants(RescueHoistAbility) == true;

    /// <summary>What it is doing at its lines; None when nothing.</summary>
    public LineJob Lines => state == FlightState.LineWork ? lineJob : LineJob.None;

    /// <summary>Whether the hoist's motor is running now: what its electrical load reads.</summary>
    public bool Hoisting => state == FlightState.LineWork && lineWorking && (lineJob is LineJob.HoistUp or LineJob.HoistDown) && float.IsNaN(hookLeft);

    /// <summary>The people on its lines now, for the inspect pane.</summary>
    public IReadOnlyList<Pawn> OnLines => lineHeld.InnerListForReading;

    float HeightMetres => climb * Props.flightHeightMetres;

    // ---- Where the lines meet the ground ----

    /// <summary>The ramp, where the ropes and the hoist hang from: the back of the aircraft along its heading.</summary>
    Vector3 RampPoint()
    {
        double rad = heading * Math.PI / 180;
        float back = Vehicle.VehicleDef.Size.z / 2f;
        return new Vector3(x - (float)Math.Sin(rad) * back, 0f, z - (float)Math.Cos(rad) * back);
    }

    /// <summary>The ground beneath the ramp as it is drawn: where the shadow puts it (the shadow's slide shows the height).</summary>
    public Vector3 FootPoint()
    {
        Vector3 ramp = RampPoint();
        (double ox, double oz) = FlightPathRule.ShadowOffset(climb, HeightCells);
        return new Vector3(ramp.x + (float)ox, 0f, ramp.z + (float)oz);
    }

    /// <summary>The nearest cell to the foot of the lines a person can stand on, open to the sky and outside the aircraft's footprint, within
    /// the hoist's reach; Invalid where there is none.</summary>
    IntVec3 LandingCell()
    {
        Map map = Vehicle.Map;
        IntVec3 foot = FootPoint().ToIntVec3();
        CellRect footprint = Vehicle.OccupiedRect();
        foreach (IntVec3 c in GenRadial.RadialCellsAround(foot, Props.hoistReachCells, useCenter: true))
        {
            if (c.InBounds(map) && !footprint.Contains(c) && c.Standable(map) && !c.Roofed(map) && !c.Fogged(map)) return c;
        }
        return IntVec3.Invalid;
    }

    // ---- Who ----

    /// <summary>People aboard in seats that do nothing (passengers, a crew chief), not downed: who goes down the ropes.</summary>
    IEnumerable<Pawn> Riders()
    {
        foreach (VehicleRoleHandler handler in Vehicle.handlers)
        {
            if (handler.role.HandlingTypes != 0 || handler.thingOwner == null) continue;
            foreach (Pawn pawn in handler.thingOwner.InnerListForReading)
            {
                if (!pawn.Downed && !pawn.Dead) yield return pawn;
            }
        }
    }

    /// <summary>Someone aboard to work the hoist, other than the one going down on it.</summary>
    bool HoistWorked(Pawn? going) => Riders().Any(p => p != going);

    /// <summary>What the hoist would lift: the person and everything they carry, kilograms.</summary>
    static float LoadKg(Pawn pawn) => pawn.GetStatValue(StatDefOf.Mass) + MassUtility.GearAndInventoryMass(pawn);

    /// <summary>People of ours (or downed and not hostile) near the foot of the hoist, whom it could lift.</summary>
    public List<Pawn> HoistCandidates()
    {
        var found = new List<Pawn>();
        if (!Vehicle.Spawned) return found;
        Map map = Vehicle.Map;
        IntVec3 foot = FootPoint().ToIntVec3();
        foreach (Thing thing in GenRadial.RadialDistinctThingsAround(foot, map, Props.hoistReachCells, useCenter: true))
        {
            if (thing is not Pawn pawn || pawn is VehiclePawn || pawn.Dead) continue;
            if (pawn.Faction == Faction.OfPlayer || (pawn.Downed && !pawn.HostileTo(Faction.OfPlayer))) found.Add(pawn);
        }
        return found;
    }

    // ---- Refusals ----

    /// <summary>Why it may not start any line work now, or null: hovering steadily, crewed, not busy.</summary>
    string? LineWorkRefusal()
    {
        NamedArgument who = Vehicle.LabelShort.Named("VEHICLE");
        if (state == FlightState.LineWork) return "PhobosFlight_RefuseLinesBusy".Translate(who).Resolve();
        if (state != FlightState.Hover) return "PhobosFlight_RefuseLinesNotHovering".Translate(who).Resolve();
        if (hold != FlightHold.Hover) return "PhobosFlight_RefuseLinesNotHovering".Translate(who).Resolve();
        if (!Vehicle.HasEnoughOperators) return "PhobosFlight_RefuseCrew".Translate(who).Resolve();
        return null;
    }

    /// <summary>Why it may not put people down the ropes now, or null.</summary>
    public string? RopeRefusal()
    {
        NamedArgument who = Vehicle.LabelShort.Named("VEHICLE");
        if (!CanRope) return "PhobosFlight_RefuseNoRopes".Translate(who).Resolve();
        if (LineWorkRefusal() is { } busy) return busy;
        if (!Riders().Any()) return "PhobosFlight_RefuseNobodyToRope".Translate(who).Resolve();
        if (!LandingCell().IsValid) return "PhobosFlight_RefuseNoGroundBelow".Translate(who).Resolve();
        return null;
    }

    /// <summary>Why the hoist cannot be worked at all now, or null: fitted, powered, someone to work it, and the line refusals.</summary>
    public string? HoistRefusal()
    {
        NamedArgument who = Vehicle.LabelShort.Named("VEHICLE");
        if (!HasHoist) return "PhobosFlight_RefuseNoHoist".Translate(who).Resolve();
        if (LineWorkRefusal() is { } busy) return busy;
        if (Feeds.Refusal(Systems, Feeds.ForAbility(Systems, RescueHoistAbility), "PhobosFlight_HoistName".Translate().Resolve()) is { } unpowered) return unpowered;
        if (!HoistWorked(null)) return "PhobosFlight_RefuseHoistNobodyWorks".Translate(who).Resolve();
        return null;
    }

    /// <summary>Why the hoist cannot lift this person now, or null.</summary>
    public string? HoistUpRefusal(Pawn pawn)
    {
        if (HoistRefusal() is { } refused) return refused;
        NamedArgument who = Vehicle.LabelShort.Named("VEHICLE"), person = pawn.LabelShort.Named("PERSON");
        float kg = LoadKg(pawn);
        if (!LineRule.HoistTakes(kg, Props.hoistRatedKilograms))
            return "PhobosFlight_RefuseHoistTooHeavy".Translate(person, kg.ToString("0").Named("KILOGRAMS"), Props.hoistRatedKilograms.ToString("0").Named("RATED")).Resolve();
        bool litter = Vehicle.GetComp<CompPhobosLitter>() is { } l && l.TakesFromHoist(pawn);
        if (!pawn.Downed && !litter && !Vehicle.handlers.Any(h => h.AreSlotsAvailable)) return "PhobosFlight_RefuseHoistNoRoom".Translate(who).Resolve();
        return null;
    }

    /// <summary>Why the hoist cannot lower this person now, or null.</summary>
    public string? HoistDownRefusal(Pawn pawn)
    {
        if (HoistRefusal() is { } refused) return refused;
        NamedArgument who = Vehicle.LabelShort.Named("VEHICLE");
        if (!HoistWorked(pawn)) return "PhobosFlight_RefuseHoistNobodyWorks".Translate(who).Resolve();
        if (!LandingCell().IsValid) return "PhobosFlight_RefuseNoGroundBelow".Translate(who).Resolve();
        return null;
    }

    /// <summary>Why it may not take another order while at its lines, or null: everyone must be off them first.</summary>
    string? LinesOutRefusal() => state == FlightState.LineWork
        ? "PhobosFlight_RefuseLinesOut".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve() : null;

    // ---- Orders ----

    /// <summary>Comes down to its work height and puts everyone in a passenger's or crew chief's seat down the ropes, two at a time.</summary>
    public void RopeDown()
    {
        if (RopeRefusal() is { } refused)
        {
            Messages.Message(refused, Vehicle, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        BeginLines(LineJob.RopeDown, null);
        Messages.Message("PhobosFlight_MsgRoping".Translate(Vehicle.LabelShort.Named("VEHICLE"), Riders().Count().Named("COUNT")).Resolve(), Vehicle,
            MessageTypeDefOf.SilentInput, historical: false);
    }

    /// <summary>Comes down to its work height, lowers the hook to this person and lifts them aboard.</summary>
    public void HoistUp(Pawn pawn)
    {
        if (HoistUpRefusal(pawn) is { } refused)
        {
            Messages.Message(refused, Vehicle, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        BeginLines(LineJob.HoistUp, pawn);
        Messages.Message("PhobosFlight_MsgHoistingUp".Translate(Vehicle.LabelShort.Named("VEHICLE"), pawn.LabelShort.Named("PERSON")).Resolve(), Vehicle,
            MessageTypeDefOf.SilentInput, historical: false);
    }

    /// <summary>Comes down to its work height and lowers this person on the hoist.</summary>
    public void HoistDown(Pawn pawn)
    {
        if (HoistDownRefusal(pawn) is { } refused)
        {
            Messages.Message(refused, Vehicle, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        BeginLines(LineJob.HoistDown, pawn);
        Messages.Message("PhobosFlight_MsgHoistingDown".Translate(Vehicle.LabelShort.Named("VEHICLE"), pawn.LabelShort.Named("PERSON")).Resolve(), Vehicle,
            MessageTypeDefOf.SilentInput, historical: false);
    }

    /// <summary>Takes nobody more onto the lines: whoever is on one is finished with, then it climbs back.</summary>
    public void StopLines()
    {
        if (state != FlightState.LineWork) return;
        lineStopping = true;
        if (lineJob == LineJob.HoistUp && lineHeld.Count == 0)
        {
            // Nobody hooked on yet: the empty hook comes straight back up.
            hookLeft = float.NaN;
            hoistRaising = true;
        }
        Messages.Message("PhobosFlight_MsgLinesStopping".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(), Vehicle, MessageTypeDefOf.SilentInput, historical: false);
    }

    void BeginLines(LineJob job, Pawn? pawn)
    {
        lineJob = job;
        lineWorking = false;
        lineStopping = false;
        cableOut = 0f;
        hookLeft = float.NaN;
        hoistRaising = false;
        hoistUnpoweredTold = false;
        hoistFor = pawn;
        ropeLeft.Clear();
        speed = 0f;
        ClearTarget();
        state = FlightState.LineWork;
    }

    // ---- Each tick at the lines ----

    void LineStep(float dt, int now)
    {
        float workClimb = (float)LineRule.WorkClimb(Props.workHeightMetres, Props.flightHeightMetres);
        if (!lineWorking)
        {
            if (lineStopping && lineHeld.Count == 0)
            {
                ClimbBack(dt);
                return;
            }
            climb = Mathf.Max(workClimb, climb - dt / Props.descentSeconds);
            if (climb <= workClimb + 0.0001f) lineWorking = true;
            return;
        }
        bool done = lineJob switch
        {
            LineJob.RopeDown => RopeStep(dt),
            LineJob.HoistUp => HoistUpStep(dt, now),
            LineJob.HoistDown => HoistDownStep(dt, now),
            _ => true,
        };
        if (!done) return;
        lineWorking = false;
        lineStopping = true;
        if (lineJob == LineJob.RopeDown && Vehicle.Faction == Faction.OfPlayer)
        {
            Messages.Message("PhobosFlight_MsgRopingDone".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(), Vehicle, MessageTypeDefOf.TaskCompletion, historical: false);
        }
    }

    /// <summary>Back up to its flight height, the lines in, then hovering.</summary>
    void ClimbBack(float dt)
    {
        climb = Mathf.Min(1f, climb + dt / Props.climbSeconds);
        if (climb < 1f) return;
        lineJob = LineJob.None;
        lineStopping = false;
        hoistFor = null;
        state = FlightState.Hover;
    }

    /// <summary>The ropes: each person slides down at the rope's speed; a free rope takes the next rider. True when nobody is left to go.</summary>
    bool RopeStep(float dt)
    {
        for (int i = lineHeld.Count - 1; i >= 0; i--)
        {
            ropeLeft[i] -= Props.ropeSlideMetresPerSecond * dt;
            if (ropeLeft[i] > 0f) continue;
            Pawn pawn = lineHeld.InnerListForReading[i];
            ropeLeft.RemoveAt(i);
            SetDown(pawn, 0f, null);
            Seen.Note("aircraft.flight-rope", Vehicle, variant: "down");
        }
        while (!lineStopping && lineHeld.Count < Props.ropePoints && Riders().FirstOrDefault() is { } next)
        {
            if (!Vehicle.RemovePawn(next) || !lineHeld.TryAdd(next)) break;
            ropeLeft.Add(HeightMetres);
        }
        return lineHeld.Count == 0 && (lineStopping || !Riders().Any());
    }

    /// <summary>Whether the hoist has power now; says so once when it stops for the want of it. The brake holds the cable meanwhile.</summary>
    bool HoistPowered(int now)
    {
        if ((now + Vehicle.thingIDNumber) % LineCheckTicks != 0) return !hoistUnpoweredTold;
        string? unpowered = Feeds.Refusal(Systems, Feeds.ForAbility(Systems, RescueHoistAbility), "PhobosFlight_HoistName".Translate().Resolve());
        if (unpowered == null)
        {
            hoistUnpoweredTold = false;
            return true;
        }
        if (!hoistUnpoweredTold && Vehicle.Faction == Faction.OfPlayer) Messages.Message(unpowered, Vehicle, MessageTypeDefOf.CautionInput, historical: false);
        hoistUnpoweredTold = true;
        return false;
    }

    /// <summary>Lifting someone: the empty hook down, the hook-up, the load up, aboard. True when it is over.</summary>
    bool HoistUpStep(float dt, int now)
    {
        float height = HeightMetres;
        if (!float.IsNaN(hookLeft))
        {
            hookLeft -= dt;
            if (hookLeft > 0f) return false;
            hookLeft = float.NaN;
            TakeOnHook();
            hoistRaising = true;
            return false;
        }
        if (!HoistPowered(now)) return false;
        if (!hoistRaising)
        {
            cableOut = Mathf.Min(height, cableOut + Props.hoistEmptyMetresPerSecond * dt);
            if (cableOut >= height) hookLeft = Props.hoistHookSeconds;
            return false;
        }
        float rate = lineHeld.Count > 0 ? Props.hoistLoadedMetresPerSecond : Props.hoistEmptyMetresPerSecond;
        cableOut = Mathf.Max(0f, cableOut - rate * dt);
        if (cableOut > 0f) return false;
        if (lineHeld.Count > 0) PutAboard(lineHeld.InnerListForReading[0]);
        return true;
    }

    /// <summary>At the bottom: whoever it was sent for, if they are still there, goes on the hook; else the hook comes up empty, and says why.</summary>
    void TakeOnHook()
    {
        Pawn? pawn = hoistFor;
        if (pawn == null || lineStopping) return;
        if (!pawn.Spawned || pawn.Map != Vehicle.Map || !HoistCandidates().Contains(pawn) || HoistUpRefusalAtHook(pawn) is { } why)
        {
            if (Vehicle.Faction == Faction.OfPlayer)
            {
                Messages.Message("PhobosFlight_MsgHookMissed".Translate(Vehicle.LabelShort.Named("VEHICLE"), pawn.LabelShort.Named("PERSON")).Resolve(), Vehicle,
                    MessageTypeDefOf.RejectInput, historical: false);
            }
            return;
        }
        pawn.DeSpawn();
        lineHeld.TryAdd(pawn);
    }

    /// <summary>The refusals that still matter at the hook-up: weight and room (the hover and the crew were asked when it set off).</summary>
    string? HoistUpRefusalAtHook(Pawn pawn)
    {
        if (!LineRule.HoistTakes(LoadKg(pawn), Props.hoistRatedKilograms)) return "weight";
        bool litter = Vehicle.GetComp<CompPhobosLitter>() is { } l && l.TakesFromHoist(pawn);
        if (!pawn.Downed && !litter && !Vehicle.handlers.Any(h => h.AreSlotsAvailable)) return "room";
        return null;
    }

    /// <summary>At the top: onto a litter if downed and one is free, else into a seat, else into the cargo as Vehicle Framework keeps the downed.</summary>
    void PutAboard(Pawn pawn)
    {
        VehiclePawn v = Vehicle;
        bool downed = pawn.Downed;
        VehicleAccess.Lines = v;
        try
        {
            if (v.GetComp<CompPhobosLitter>() is { } litter && litter.LoadFromHoist(pawn, lineHeld)) { }
            else
            {
                lineHeld.Remove(pawn);
                if (!v.TryAddPawn(pawn)) v.AddOrTransfer(pawn);
            }
        }
        finally
        {
            VehicleAccess.Lines = null;
        }
        Seen.Note("aircraft.flight-hoist", v, variant: downed ? "up-downed" : "up");
        if (v.Faction == Faction.OfPlayer)
        {
            Messages.Message("PhobosFlight_MsgHoistedUp".Translate(pawn.LabelShort.Named("PERSON"), v.LabelShort.Named("VEHICLE")).Resolve(), v,
                MessageTypeDefOf.TaskCompletion, historical: false);
        }
    }

    /// <summary>Lowering someone: onto the hook from their seat, down at the loaded speed, set down, the empty hook back up. True when over.</summary>
    bool HoistDownStep(float dt, int now)
    {
        float height = HeightMetres;
        if (!HoistPowered(now)) return false;
        if (!hoistRaising)
        {
            if (lineHeld.Count == 0)
            {
                Pawn? pawn = hoistFor;
                if (pawn == null || lineStopping || !Vehicle.AllPawnsAboard.Contains(pawn) || !Vehicle.RemovePawn(pawn) || !lineHeld.TryAdd(pawn)) return true;
            }
            cableOut = Mathf.Min(height, cableOut + Props.hoistLoadedMetresPerSecond * dt);
            if (cableOut < height) return false;
            Pawn down = lineHeld.InnerListForReading[0];
            SetDown(down, 0f, "PhobosFlight_MsgHoistedDown");
            Seen.Note("aircraft.flight-hoist", Vehicle, variant: "down");
            hoistRaising = true;
            return false;
        }
        cableOut = Mathf.Max(0f, cableOut - Props.hoistEmptyMetresPerSecond * dt);
        return cableOut <= 0f;
    }

    /// <summary>
    /// Puts someone off a line onto the ground near its foot: gently at the bottom, or falling from <paramref name="fallMetres"/>, hurt by the
    /// height (LineRule). Says what happened.
    /// </summary>
    void SetDown(Pawn pawn, float fallMetres, string? messageKey)
    {
        VehiclePawn v = Vehicle;
        Map map = v.MapHeld;
        lineHeld.Remove(pawn);
        IntVec3 cell = LandingCell();
        if (!cell.IsValid) cell = FootPoint().ToIntVec3().ClampInsideMap(map);
        if (!GenPlace.TryPlaceThing(pawn, cell, map, ThingPlaceMode.Near)) GenSpawn.Spawn(pawn, cell, map);
        v.EventRegistry[VehicleEventDefOf.PawnExited].ExecuteEvents();
        double harm = LineRule.FallHarm(fallMetres);
        if (harm > 0 && !VehicleSystemsMod.Settings.showOnly)
        {
            for (int i = 0; i < LineRule.FallBlows; i++)
            {
                if (pawn.Dead) break;
                pawn.TakeDamage(new DamageInfo(DamageDefOf.Blunt, (float)(harm / LineRule.FallBlows)));
            }
        }
        if (v.Faction != Faction.OfPlayer || messageKey == null) return;
        Messages.Message(messageKey.Translate(pawn.LabelShort.Named("PERSON"), v.LabelShort.Named("VEHICLE"), fallMetres.ToString("0").Named("METRES")).Resolve(),
            pawn, harm > 0 ? MessageTypeDefOf.NegativeEvent : MessageTypeDefOf.SilentInput, historical: false);
    }

    /// <summary>
    /// Everyone on a line falls (the owner's "real consequences", 19 Nov 2026): the aircraft is coming down or being destroyed. Each falls from
    /// the height they were at: what they still had to slide, or their height on the cable.
    /// </summary>
    void DropLines()
    {
        if (lineHeld.Count == 0 || !Vehicle.Spawned)
        {
            lineJob = LineJob.None;
            return;
        }
        bool rope = lineJob == LineJob.RopeDown;
        List<Pawn> falling = lineHeld.InnerListForReading.ToList();
        for (int i = 0; i < falling.Count; i++)
        {
            float metres = rope ? (i < ropeLeft.Count ? ropeLeft[i] : HeightMetres) : (float)LineRule.HeightOnCable(HeightMetres, cableOut);
            SetDown(falling[i], metres, "PhobosFlight_MsgLineFall");
        }
        Seen.Note("aircraft.flight-line-fall", Vehicle, variant: rope ? "rope" : "hoist");
        ropeLeft.Clear();
        lineJob = LineJob.None;
        hoistFor = null;
    }

    /// <summary>For Vehicle Framework's destruction: whoever is on a line falls before the aircraft is gone.</summary>
    public static void OnDestroying(VehiclePawn vehicle)
    {
        if (vehicle.GetComp<CompPhobosFlight>() is { state: FlightState.LineWork } flight) flight.DropLines();
    }

    // ---- What it shows ----

    /// <summary>The ropes or the hoist's cable, from the ramp down toward the ground beneath it as it is drawn.</summary>
    void DrawLines()
    {
        if (state != FlightState.LineWork || !lineWorking) return;
        Vector3 ramp = RampPoint(), foot = FootPoint();
        float y = AltitudeLayer.MetaOverlays.AltitudeFor();
        var top = new Vector3(ramp.x, y, ramp.z);
        if (lineJob == LineJob.RopeDown)
        {
            // Two ropes, either side of the ramp's middle.
            double rad = (heading + 90) * Math.PI / 180;
            var side = new Vector3((float)Math.Sin(rad) * 0.4f, 0f, (float)Math.Cos(rad) * 0.4f);
            for (int i = 0; i < Props.ropePoints; i++)
            {
                Vector3 offset = Props.ropePoints > 1 ? side * (i == 0 ? -1 : 1) : Vector3.zero;
                GenDraw.DrawLineBetween(top + offset, new Vector3(foot.x, y, foot.z) + offset, SimpleColor.White, 0.12f);
            }
            return;
        }
        float share = HeightMetres <= 0f ? 0f : Mathf.Clamp01(cableOut / HeightMetres);
        var end = Vector3.Lerp(top, new Vector3(foot.x, y, foot.z), share);
        GenDraw.DrawLineBetween(top, end, SimpleColor.White, 0.08f);
    }
}
