using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Environment;
using Phobos.Framework.Rules;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>A parachute drop under way (Framework 0.288.0): what is still to go and when, saved with the aircraft.</summary>
public class AirdropRun : IExposable
{
    /// <summary>From a hover (else a pass along a line).</summary>
    public bool hover;
    /// <summary>A hover drop from where it hovers, below the safe minimum, rather than from its drop height.</summary>
    public bool low;
    /// <summary>The point the player chose.</summary>
    public IntVec3 target;
    /// <summary>A pass: where on its line people are let go (the people's release point), and its heading.</summary>
    public float lineX, lineZ, lineHeading;
    /// <summary>A hover drop: seconds still climbing to its drop height, and whether it is coming back down after.</summary>
    public float climbLeft;
    public bool descending;
    /// <summary>Seconds until the next person, bundle and vehicle may go.</summary>
    public float nextPerson, nextBundle, nextVehicle;
    /// <summary>What has gone so far.</summary>
    public int people, bundles, vehicles, platformsLeft;
    /// <summary>Whether a raid's drop (its people only; no messages to the player).</summary>
    public bool raid;

    public void ExposeData()
    {
        Scribe_Values.Look(ref hover, "hover");
        Scribe_Values.Look(ref low, "low");
        Scribe_Values.Look(ref target, "target");
        Scribe_Values.Look(ref lineX, "lineX");
        Scribe_Values.Look(ref lineZ, "lineZ");
        Scribe_Values.Look(ref lineHeading, "lineHeading");
        Scribe_Values.Look(ref climbLeft, "climbLeft");
        Scribe_Values.Look(ref descending, "descending");
        Scribe_Values.Look(ref nextPerson, "nextPerson");
        Scribe_Values.Look(ref nextBundle, "nextBundle");
        Scribe_Values.Look(ref nextVehicle, "nextVehicle");
        Scribe_Values.Look(ref people, "people");
        Scribe_Values.Look(ref bundles, "bundles");
        Scribe_Values.Look(ref vehicles, "vehicles");
        Scribe_Values.Look(ref platformsLeft, "platformsLeft");
        Scribe_Values.Look(ref raid, "raid");
    }
}

/// <summary>
/// Parachute drops from our own flight (D'thorga Framework 0.288.0; docs/development/airdrop.md, "Dropping from our flight"). A drop pass:
/// the aircraft flies a straight line through the point upwind of the target where its people must be let go, drops people, bundles and
/// vehicles in that order as it crosses, and flies on (out of the far edge and home, after a journey). A hover drop: it climbs to its drop
/// height where it hovers (not drawn higher: our flight draws one height), drops, and comes back down; or drops low from where it is.
/// </summary>
public partial class CompPhobosFlight
{
    /// <summary>Seconds between two vehicles going off the ramp (authored).</summary>
    const float SecondsPerPlatform = 2f;

    AirdropRun? drop;

    /// <summary>Whether a drop is under way.</summary>
    public bool Dropping => drop != null;

    void ExposeDrop() => Scribe_Deep.Look(ref drop, "PhobosFlight_Airdrop");

    /// <summary>
    /// Its map is about to be removed while it is in the air (Framework 0.293.0): on a journey home it hands over at once; a raid's aircraft
    /// goes home; otherwise it comes down on the world map as a caravan at that place, with everyone aboard. A drop under way ends; what has
    /// not gone stays aboard.
    /// </summary>
    public void LeaveBeforeMapGoes()
    {
        VehiclePawn v = Vehicle;
        if (!Airborne || !v.Spawned) return;
        EndDrop(null);
        if (state == FlightState.LineWork) DropLines();
        if (raider != null)
        {
            Vanish();
            return;
        }
        if (Departing)
        {
            HandOver();
            return;
        }
        Map map = v.Map;
        PlanetTile tile = map.Tile;
        state = FlightState.Ground;
        climb = 0f;
        speed = 0f;
        ClearTarget();
        orbitX = orbitZ = float.NaN;
        v.Transform.rotation = 0f;
        FlightRegistry.Remove(this);
        StopTicking();
        EndFlightSound();
        v.GetComp<CompPhobosLitter>()?.HoldThroughDespawn();
        v.DeSpawn(DestroyMode.Vanish);
        map.GetDetachedMapComponent<VehiclePositionManager>()?.ReleaseClaimed(v);
        AerialVehicleInFlight flight = AerialVehicleInFlight.Create(v, tile);
        flight.SwitchToCaravan();
        Seen.Note("aircraft.map-removed", v);
        if (v.Faction == Faction.OfPlayer)
            Messages.Message("PhobosFlight_MsgMapGoneCaravan".Translate(v.LabelShort.Named("VEHICLE")).Resolve(), MessageTypeDefOf.NeutralEvent);
    }

    /// <summary>The kind of load a drop's line is laid for: the first it carries of people, bundles, vehicles.</summary>
    ParachuteLoad MainLoad()
    {
        if (Airdrop.Jumpers(Vehicle).Any()) return ParachuteLoad.Person;
        if (Airdrop.Cargo(Vehicle).Any()) return ParachuteLoad.Bundle;
        return ParachuteLoad.Vehicle;
    }

    // ---- Refusals ----

    /// <summary>Why it may not make a drop pass over this point now, or null.</summary>
    public string? DropPassRefusal(IntVec3 cell)
    {
        if (LinesOutRefusal() is { } lines) return lines;
        VehiclePawn v = Vehicle;
        NamedArgument who = v.LabelShort.Named("VEHICLE");
        if (Dropping) return "PhobosAirdrop_RefuseBusy".Translate(who).Resolve();
        if (!Airborne || state is FlightState.LiftingOff or FlightState.ComingDown or FlightState.Settling or FlightState.TakeoffRoll or FlightState.RollingLanding)
            return "PhobosFlight_RefuseNotFlying".Translate(who).Resolve();
        if (Airdrop.Refusal(v, out _) is { } why) return why;
        if (cell.IsValid && AirdropTargeter.PointRefusal(v.Map, cell) is { } point) return point;
        return null;
    }

    /// <summary>Why it may not drop from where it hovers now, or null.</summary>
    public string? DropHereRefusal()
    {
        if (LinesOutRefusal() is { } lines) return lines;
        VehiclePawn v = Vehicle;
        NamedArgument who = v.LabelShort.Named("VEHICLE");
        if (Dropping) return "PhobosAirdrop_RefuseBusy".Translate(who).Resolve();
        if (state != FlightState.Hover || hold != FlightHold.Hover) return "PhobosAirdrop_RefuseNotHovering".Translate(who).Resolve();
        if (!v.HasEnoughOperators) return "PhobosFlight_RefuseCrew".Translate(who).Resolve();
        return Airdrop.Refusal(v, out _);
    }

    // ---- Orders ----

    /// <summary>Flies a straight pass over this point, letting go upwind so the wind carries everything onto it, and flies on.</summary>
    public void DropPass(IntVec3 cell)
    {
        if (DropPassRefusal(cell) is { } refused)
        {
            Messages.Message(refused, Vehicle, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        CancelDeparture();
        Vector3 release = ReleasePoint(cell);
        double along = FlightPathRule.HeadingTo(release.x - x, release.z - z, heading);
        BeginPass(cell, release, along, raid: false);
        // Over the map with no journey: on past the line, then it holds (or circles) there.
        double rad = along * Math.PI / 180;
        (targetX, targetZ) = Clamped((float)(release.x + Math.Sin(rad) * PassRunOn()), (float)(release.z + Math.Cos(rad) * PassRunOn()));
    }

    /// <summary>
    /// A drop pass begun as it comes in over the edge from another place (the world map's "Airdrop on"): along its inward heading, swung toward
    /// the release point, and out of the far edge on the journey home. Without a journey home it holds past the line.
    /// </summary>
    public void BeginArrivalPass(IntVec3 cell, List<FlightNode>? home, IArrivalAction? homeAction, bool raid = false)
    {
        Vector3 release = ReleasePoint(cell);
        double along = FlightPathRule.HeadingTo(release.x - x, release.z - z, heading);
        BeginPass(cell, release, along, raid);
        (float ex, float ez) = EdgeBounds();
        Map map = Vehicle.Map;
        (double tx, double tz) = FlightPathRule.EdgePoint(release.x, release.z, along, ex, map.Size.x - ex, ez, map.Size.z - ez);
        (targetX, targetZ) = ((float)tx, (float)tz);
        if (home is { Count: > 0 } && homeAction != null)
        {
            departNodes = home;
            departAction = homeAction;
        }
        else if (raid && raider != null)
        {
            // A raid's aircraft leaves by the far edge it flies out over.
            raider.exitX = targetX;
            raider.exitZ = targetZ;
        }
    }

    void BeginPass(IntVec3 cell, Vector3 release, double along, bool raid)
    {
        landCell = IntVec3.Invalid;
        LeaveOrbit();
        if (state == FlightState.Hover) state = FlightState.Moving;
        PhobosAirdropExtension ext = Airdrop.Ext(Vehicle) ?? new PhobosAirdropExtension();
        drop = new AirdropRun { target = cell, lineX = release.x, lineZ = release.z, lineHeading = (float)along, platformsLeft = ext.platforms, raid = raid };
        Seen.Note("airdrop.pass", Vehicle, variant: raid ? "raid" : "ours");
        if (!raid) SayDropStarted();
    }

    /// <summary>Drops from where it hovers: after climbing to its drop height, or at once from here when <paramref name="low"/>.</summary>
    public void DropHere(bool low)
    {
        if (DropHereRefusal() is { } refused)
        {
            Messages.Message(refused, Vehicle, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        PhobosAirdropExtension ext = Airdrop.Ext(Vehicle)!;
        float climbFeet = (float)ParachuteRule.For(MainLoad()).PlannedFeet;
        float rate = Props.flightHeightMetres / Mathf.Max(0.1f, Props.climbSeconds);
        float seconds = low ? 0f : Mathf.Max(0f, (climbFeet * (float)ParachuteRule.MetresPerFoot - HeightMetresNow) / rate);
        drop = new AirdropRun
        {
            hover = true, low = low, target = Vehicle.Position, climbLeft = seconds * (float)ParachuteRule.MapDescentTimeShare,
            platformsLeft = ext.platforms,
        };
        Seen.Note("airdrop.hover", Vehicle, variant: low ? "low" : "climbed");
        SayDropStarted();
    }

    /// <summary>How far past the release point a pass ordered over the map flies on before it holds, cells (authored).</summary>
    float PassRunOn() => Mathf.Max(15f, Props.orbitRadiusCells);

    /// <summary>Where its people (or its main load) must be let go for the wind to carry them onto <paramref name="cell"/>.</summary>
    Vector3 ReleasePoint(IntVec3 cell)
    {
        Map map = Vehicle.Map;
        ParachuteLoad main = MainLoad();
        float drift = PhobosParachuteFaller.DriftCells(map, main, (float)ParachuteRule.For(main).PlannedFeet);
        Vector3 p = cell.ToVector3Shifted() - PhobosWind.Downwind(map) * drift;
        (p.x, p.z) = Clamped(p.x, p.z);
        p.y = 0f;
        return p;
    }

    /// <summary>The drop's opening message: what goes, the wind, and any warning.</summary>
    void SayDropStarted()
    {
        VehiclePawn v = Vehicle;
        if (v.Faction != Faction.OfPlayer) return;
        Airdrop.Refusal(v, out string summary);
        Map map = v.Map;
        string wind = AirdropTargeter.WindWords(map, MainLoad(), drop!.low ? FeetNow() : (float)ParachuteRule.For(MainLoad()).PlannedFeet);
        Messages.Message("PhobosAirdrop_MsgStarted".Translate(v.LabelShort.Named("VEHICLE"), summary.Named("WHAT"), wind.Named("WIND")).Resolve(), v,
            MessageTypeDefOf.NeutralEvent, historical: false);
        foreach (string warning in AirdropTargeter.Warnings(map, MainLoad(), drop.low ? FeetNow() : (float)ParachuteRule.For(MainLoad()).PlannedFeet))
            Messages.Message(warning, v, MessageTypeDefOf.CautionInput, historical: false);
    }

    /// <summary>Its height now in feet: what a low drop falls from.</summary>
    float FeetNow() => HeightMetresNow / (float)ParachuteRule.MetresPerFoot;

    // ---- Each tick of a drop ----

    void DropStep(float dt)
    {
        AirdropRun run = drop!;
        VehiclePawn v = Vehicle;
        if (run.hover)
        {
            if (state != FlightState.Hover)
            {
                EndDrop("PhobosAirdrop_MsgStoppedMoved");
                return;
            }
            if (run.climbLeft > 0f)
            {
                run.climbLeft -= dt;
                return;
            }
            if (run.descending)
            {
                drop = null;
                return;
            }
            if (!ReleaseDue(run, dt)) FinishHover(run);
            return;
        }
        // A pass: how far along its line it is from the release point, in cells; it lets each kind go in a stick centred on its point.
        double rad = run.lineHeading * Math.PI / 180;
        float along = (x - run.lineX) * (float)Math.Sin(rad) + (z - run.lineZ) * (float)Math.Cos(rad);
        float stick = StickHalfLength(v, run);
        bool inWindow = along >= -stick;
        if (!inWindow) return;
        bool more = ReleaseDue(run, dt);
        if (!more || along > stick + 30f) EndDrop(null);
    }

    /// <summary>Half the length of the stick its people make along the line, cells: they leave a door a second at its speed.</summary>
    float StickHalfLength(VehiclePawn v, AirdropRun run)
    {
        PhobosAirdropExtension ext = Airdrop.Ext(v) ?? new PhobosAirdropExtension();
        int people = Mathf.Min(Airdrop.Jumpers(v).Count(), Airdrop.Count(v, AirdropDefOf.Phobos_PersonnelParachute));
        float seconds = people / (float)Mathf.Max(1, ext.jumpDoors);
        return seconds * Mathf.Max(1f, speed) / 2f;
    }

    /// <summary>Lets go whatever is due this tick, in order: people, then bundles, then vehicles. False when nothing is left that can go.</summary>
    bool ReleaseDue(AirdropRun run, float dt)
    {
        VehiclePawn v = Vehicle;
        PhobosAirdropExtension ext = Airdrop.Ext(v) ?? new PhobosAirdropExtension();
        Map map = v.Map;
        Vector3 ramp = RampPoint();
        string from = v.LabelShort;
        run.nextPerson -= dt;
        run.nextBundle -= dt;
        run.nextVehicle -= dt;
        bool peopleLeft = Airdrop.Jumpers(v).Any() && Airdrop.Count(v, AirdropDefOf.Phobos_PersonnelParachute) > 0;
        if (peopleLeft)
        {
            while (run.nextPerson <= 0f && Airdrop.Jumpers(v).FirstOrDefault() is { } jumper && Airdrop.UseOne(v, AirdropDefOf.Phobos_PersonnelParachute, out int packed))
            {
                if (!v.RemovePawn(jumper)) break;
                PhobosParachuteFaller.Release(map, jumper, ParachuteLoad.Person, Spread(ramp, run.people), Feet(run, ParachuteLoad.Person), from, quality: packed);
                run.people++;
                run.nextPerson += 1f / Mathf.Max(1, ext.jumpDoors);
            }
            return true;
        }
        if (run.raid) return false;
        bool bundlesLeft = Airdrop.Cargo(v).Any() && Airdrop.CargoChutes(v) > 0;
        if (bundlesLeft)
        {
            // Framework 0.293.0: a guided canopy first, steering itself onto the point; the packer's quality goes with each.
            if (run.nextBundle <= 0f && Airdrop.NextCargoChute(v) is { } chute && Airdrop.UseOne(v, chute, out int packedBundle) && Airdrop.TakeBundle(v) is { } bundle)
            {
                Vector3? aim = chute == AirdropDefOf.Phobos_GuidedCargoParachute ? run.target.ToVector3Shifted() : null;
                PhobosParachuteFaller faller = PhobosParachuteFaller.Release(map, bundle[0], ParachuteLoad.Bundle, ramp, Feet(run, ParachuteLoad.Bundle), from,
                    quality: packedBundle, guidedTo: aim);
                for (int i = 1; i < bundle.Count; i++) faller.innerContainer.TryAdd(bundle[i], canMergeWithExistingStacks: false);
                run.bundles++;
                run.nextBundle = 1f / Mathf.Max(0.1f, ext.bundlesPerSecond);
            }
            return true;
        }
        if (run.platformsLeft <= 0) return false;
        VehiclePawn? next = DroppableVehicle(v, ext);
        if (next == null || Airdrop.Count(v, AirdropDefOf.Phobos_HeavyDropRig) <= 0) return false;
        if (run.nextVehicle > 0f) return true;
        if (Airdrop.UseOne(v, AirdropDefOf.Phobos_HeavyDropRig, out int packedRig) && Airdrop.TakeHeld(v, next))
        {
            bool crew = next.AllPawnsAboard.Any();
            PhobosParachuteFaller.Release(map, next, ParachuteLoad.Vehicle, ramp, Feet(run, ParachuteLoad.Vehicle), from, withCrew: crew, quality: packedRig);
            run.vehicles++;
            run.platformsLeft--;
            run.nextVehicle = SecondsPerPlatform;
        }
        return run.platformsLeft > 0 && DroppableVehicle(v, ext) != null;
    }

    /// <summary>The next vehicle in its hold it may drop: within its platform's weight, and empty unless the crew-inside setting is on.</summary>
    static VehiclePawn? DroppableVehicle(VehiclePawn v, PhobosAirdropExtension ext) =>
        Airdrop.HeldVehicles(v).FirstOrDefault(h => Airdrop.VehicleKg(h) <= ext.heaviestPlatformKg
            && (VehicleSystemsMod.Settings.airdropCrewInside || !h.AllPawnsAboard.Any()));

    /// <summary>The height a load falls from: its planned height, or from where it hovers on a low drop.</summary>
    float Feet(AirdropRun run, ParachuteLoad kind) => run.low ? FeetNow() : (float)ParachuteRule.For(kind).PlannedFeet;

    /// <summary>Spreads people from the two sides of the ramp, as from two doors.</summary>
    Vector3 Spread(Vector3 ramp, int index)
    {
        double rad = (heading + 90) * Math.PI / 180;
        float side = index % 2 == 0 ? -0.6f : 0.6f;
        return new Vector3(ramp.x + (float)Math.Sin(rad) * side, 0f, ramp.z + (float)Math.Cos(rad) * side);
    }

    /// <summary>A hover drop done: the climb back down to its height (not drawn), then it hovers as before.</summary>
    void FinishHover(AirdropRun run)
    {
        SayDropDone(run);
        run.descending = true;
        float rate = Props.flightHeightMetres / Mathf.Max(0.1f, Props.climbSeconds);
        run.climbLeft = run.low ? 0f : Mathf.Max(0f, ((float)ParachuteRule.Person.PlannedFeet * (float)ParachuteRule.MetresPerFoot - HeightMetresNow) / rate)
            * (float)ParachuteRule.MapDescentTimeShare;
        if (run.climbLeft <= 0f) drop = null;
    }

    /// <summary>Ends the drop under way, saying what went and, when given, why it stopped.</summary>
    void EndDrop(string? whyKey)
    {
        if (drop == null) return;
        AirdropRun run = drop;
        drop = null;
        if (run.raid || Vehicle.Faction != Faction.OfPlayer) return;
        if (whyKey != null) Messages.Message(whyKey.Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(), Vehicle, MessageTypeDefOf.CautionInput, historical: false);
        SayDropDone(run);
    }

    void SayDropDone(AirdropRun run)
    {
        VehiclePawn v = Vehicle;
        if (run.raid || v.Faction != Faction.OfPlayer) return;
        string left = Airdrop.Refusal(v, out string summary) == null ? summary : "";
        Messages.Message("PhobosAirdrop_MsgDone".Translate(v.LabelShort.Named("VEHICLE"), run.people.Named("PEOPLE"), run.bundles.Named("BUNDLES"),
            run.vehicles.Named("VEHICLES")).Resolve() + (left.Length > 0 ? " " + "PhobosAirdrop_MsgStillAboard".Translate(left.Named("WHAT")).Resolve() : ""), v,
            MessageTypeDefOf.TaskCompletion, historical: false);
    }
}
