using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Interface;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using SmashTools;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

public sealed class CompProperties_PhobosHitch : CompProperties
{
    public CompProperties_PhobosHitch() => compClass = typeof(CompPhobosHitch);
}

/// <summary>Where a vehicle's hitch sits, for a vehicle whose picture needs it somewhere other than just behind its rear edge.</summary>
public sealed class PhobosHitchPointExtension : DefModExtension
{
    /// <summary>From the middle of the vehicle's footprint back to the hitch, in cells.</summary>
    public float behindCentre = -1f;
}

/// <summary>
/// A vehicle's hitch (Framework 0.101.0, slice 2 of docs/development/vehicle-hitch-design.md). The trailer stays a vehicle on the map, so it
/// blocks and is blocked and can be shot (owner, 11 Nov 2026). While the vehicle towing it moves, the trailer's axle is dragged after the
/// hitch by the game-free <see cref="HitchRule"/>; when the axle's pose puts the trailer's footprint on new cells, the trailer is moved there
/// as Vehicle Framework moves a vehicle (its Position and Rotation, which Vehicle Framework's own patches follow with its regions), and the
/// part of a turn the four facings cannot show is drawn through its public Transform. No Harmony. Added at start-up to land vehicles with a
/// loadout. Ticks only while hitched (Vehicle Framework's tick on request), and works only while the vehicle moves or the trailer settles.
/// </summary>
public sealed class CompPhobosHitch : VehicleComp
{
    /// <summary>Ticks the trailer keeps following after the vehicle stops, while the picture of the vehicle settles in its cell.</summary>
    const int SettleTicks = 90;
    /// <summary>How often the towed weight is counted again while hitched (cargo and fuel change slowly).</summary>
    const int WeightTicks = 250;
    /// <summary>The least time between two "cannot follow" messages.</summary>
    const int BlockedMessageTicks = 600;

    VehiclePawn? trailer;
    VehiclePawn? pending;
    float axleX, axleZ, heading;
    int settle;
    int weightTick = -1;
    double towedKg;
    float plainCap = 1f;
    int lastBlockedMessage = -BlockedMessageTicks;
    bool needsLook;

    static PhobosVehicleRulesDef Rules => PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();

    public override bool TickByRequest => true;

    /// <summary>The trailer this vehicle tows, or null.</summary>
    public VehiclePawn? Trailer => trailer;
    /// <summary>The trailer chosen for hitching, waiting for a colonist.</summary>
    public VehiclePawn? Pending => pending;

    CompPhobosVehicleSystems? Systems => Vehicle.GetComp<CompPhobosVehicleSystems>();

    static CompProperties_PhobosTrailer? TrailerProps(VehiclePawn vehicle) => vehicle.GetComp<CompPhobosTrailer>()?.Props;

    // ---- Who can tow, and what ----

    /// <summary>Whether this vehicle's drivetrain model works out the tow (a ground drivetrain of ours, on the full model).</summary>
    public bool FullModel => Systems?.Powertrain is { IsAircraft: false, Ground: true, Full: true };

    bool HasDrivetrain => Systems?.Powertrain is { IsAircraft: false, Ground: true };

    /// <summary>Why this vehicle cannot hitch anything now, or null.</summary>
    public string? Refusal()
    {
        if (!HasDrivetrain && !VehicleSystemsMod.Settings.hitchOthersPlainRule) return "PhobosHitch_NotFitted".Translate(Vehicle.LabelShort.Named("TRACTOR")).Resolve();
        if (trailer != null) return "PhobosHitch_AlreadyTowing".Translate(Vehicle.LabelShort.Named("TRACTOR"), trailer.LabelShort.Named("TRAILER")).Resolve();
        return null;
    }

    /// <summary>Why this trailer cannot be hitched to this vehicle now, or null.</summary>
    public string? RefusalFor(VehiclePawn candidate)
    {
        if (candidate == Vehicle) return "PhobosHitch_NotItself".Translate().Resolve();
        if (TrailerProps(candidate) == null) return "PhobosHitch_NotATrailer".Translate(candidate.LabelShort.Named("TRAILER")).Resolve();
        if (candidate.Faction != Faction.OfPlayer) return "PhobosHitch_NotOurs".Translate(candidate.LabelShort.Named("TRAILER")).Resolve();
        if (Hitches.Towing(candidate) is { } other && other != this)
        {
            return "PhobosHitch_TowedByOther".Translate(candidate.LabelShort.Named("TRAILER"), other.Vehicle.LabelShort.Named("TRACTOR")).Resolve();
        }
        if (!candidate.Spawned || candidate.Map != Vehicle.Map) return "PhobosHitch_NotHere".Translate(candidate.LabelShort.Named("TRAILER")).Resolve();
        if (candidate.GetComp<CompPhobosTrailer>()?.DrawbarBroken == true) return "PhobosHitch_DrawbarBroken".Translate(candidate.LabelShort.Named("TRAILER")).Resolve();
        if (candidate.Position.DistanceTo(Vehicle.Position) > Rules.hitchReachCells)
        {
            return "PhobosHitch_TooFar".Translate(candidate.LabelShort.Named("TRAILER"), Rules.hitchReachCells.Named("CELLS")).Resolve();
        }
        if (Vehicle.vehiclePather is { Moving: true }) return "PhobosHitch_Moving".Translate(Vehicle.LabelShort.Named("TRACTOR")).Resolve();
        return null;
    }

    // ---- What the player orders ----

    public void Choose(VehiclePawn candidate)
    {
        if ((Refusal() ?? RefusalFor(candidate)) is { } refusal)
        {
            Say(refusal, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        pending = candidate;
        Hitches.NoteWaiting(this, true);
        Say("PhobosHitch_Chosen".Translate(Vehicle.LabelShort.Named("TRACTOR"), candidate.LabelShort.Named("TRAILER")).Resolve(),
            new LookTargets(Vehicle, candidate), MessageTypeDefOf.TaskCompletion, historical: false);
    }

    public void CancelPending()
    {
        if (pending == null) return;
        Say("PhobosHitch_Cancelled".Translate(pending.LabelShort.Named("TRAILER")).Resolve(), Vehicle, MessageTypeDefOf.SilentInput, historical: false);
        pending = null;
        Hitches.NoteWaiting(this, false);
    }

    /// <summary>A colonist has coupled the chosen trailer (the hitch-up job): it is put behind the vehicle, if there is room.</summary>
    public void CoupledBy(Pawn crew)
    {
        VehiclePawn? candidate = pending;
        pending = null;
        Hitches.NoteWaiting(this, false);
        if (candidate == null) return;
        if ((Refusal() ?? RefusalFor(candidate)) is { } refusal)
        {
            Say(refusal, candidate, MessageTypeDefOf.RejectInput);
            return;
        }
        if (!Couple(candidate))
        {
            Say("PhobosHitch_NoRoom".Translate(Vehicle.LabelShort.Named("TRACTOR"), candidate.LabelShort.Named("TRAILER")).Resolve(), candidate, MessageTypeDefOf.RejectInput);
            return;
        }
        Say("PhobosHitch_Hitched".Translate(crew.LabelShort.Named("CREW"), candidate.LabelShort.Named("TRAILER"), Vehicle.LabelShort.Named("TRACTOR")).Resolve(),
            new LookTargets(Vehicle, candidate), MessageTypeDefOf.TaskCompletion);
    }

    /// <summary>Puts the trailer straight behind the vehicle and holds it there. False, with nothing changed, when its place is blocked.
    /// Also the self-test's way in.</summary>
    internal bool Couple(VehiclePawn candidate)
    {
        CompProperties_PhobosTrailer? props = TrailerProps(candidate);
        if (props == null || !Vehicle.Spawned) return false;
        (double hx, double hz) = HitchPoint();
        TrailerPose pose = HitchRule.AxleFor(hx, hz, VehicleHeading(Vehicle), props.hitchToAxle);
        trailer = candidate;
        if (!TryPlace(pose, props))
        {
            trailer = null;
            return false;
        }
        Remember(pose);
        Hitches.Note(this, candidate);
        weightTick = -1;
        RefreshWeight();
        Systems?.Powertrain?.MarkMassDirty();
        Vehicle.RequestTickStart(this);
        return true;
    }

    /// <summary>Lets the trailer go where it stands. Says so unless <paramref name="quiet"/>.</summary>
    public void Unhitch(bool quiet = false)
    {
        VehiclePawn? was = trailer;
        if (was == null) return;
        trailer = null;
        towedKg = 0;
        plainCap = 1f;
        Hitches.Forget(was);
        // The facings show it square again: the extra turn is a picture of the tow only, and is not saved by Vehicle Framework.
        was.Transform.rotation = 0f;
        Systems?.Powertrain?.MarkMassDirty();
        Vehicle.RequestTickStop(this);
        if (!quiet) Say("PhobosHitch_Unhitched".Translate(was.LabelShort.Named("TRAILER"), Vehicle.LabelShort.Named("TRACTOR")).Resolve(), was, MessageTypeDefOf.SilentInput, historical: false);
    }

    // ---- The tow ----

    /// <summary>The weight behind the hitch, in kilograms, for the drivetrain model: the trailer empty and all it carries.</summary>
    public double TowedKilograms => trailer == null ? 0 : towedKg;

    /// <summary>The share of its speed a vehicle keeps on the plain rule (no drivetrain model working out the tow); 1 otherwise.</summary>
    public float PlainSpeedCap => trailer == null || FullModel ? 1f : plainCap;

    void RefreshWeight()
    {
        if (trailer == null) return;
        int now = Find.TickManager.TicksGame;
        if (weightTick >= 0 && now - weightTick < WeightTicks) return;
        weightTick = now;
        towedKg = CompPhobosSling.LoadKilograms(trailer);
        plainCap = (float)HitchRule.PlainCap(CompPhobosSling.LoadKilograms(Vehicle), towedKg, DrivetrainModel.CrawlFloor);
    }

    public override void CompTick()
    {
        base.CompTick();
        if (trailer == null) return;
        if (!trailer.Spawned || !Vehicle.Spawned || trailer.Map != Vehicle.Map)
        {
            Unhitch(quiet: !trailer.Spawned && trailer.Destroyed);
            return;
        }
        bool moving = Vehicle.vehiclePather is { Moving: true };
        if (moving) settle = SettleTicks;
        else if (settle > 0) settle--;
        else if (!needsLook) return;
        needsLook = false;
        RefreshWeight();
        Follow(moving);
    }

    /// <summary>One step of the tow: the axle dragged after the hitch, held within the jack-knife angle, the trailer moved if its cells change.</summary>
    void Follow(bool moving)
    {
        CompProperties_PhobosTrailer? props = TrailerProps(trailer!);
        if (props == null) return;
        (double hx, double hz) = HitchPoint();
        double vehicleHeading = VehicleHeading(Vehicle);
        TrailerPose pose = HitchRule.Follow(hx, hz, new TrailerPose(axleX, axleZ, heading), props.hitchToAxle);
        pose = HitchRule.Limit(hx, hz, pose, vehicleHeading, props.jackKnifeDegrees, props.hitchToAxle);
        if (TryPlace(pose, props))
        {
            Remember(pose);
            return;
        }
        // Blocked: the vehicle stops where it is, and the player is told why (no path of ours plans for the trailer in this slice).
        if (moving) Vehicle.vehiclePather!.StopDead();
        int now = Find.TickManager.TicksGame;
        if (now - lastBlockedMessage >= BlockedMessageTicks && Vehicle.Faction == Faction.OfPlayer)
        {
            lastBlockedMessage = now;
            Say("PhobosHitch_CannotFollow".Translate(trailer!.LabelShort.Named("TRAILER"), Vehicle.LabelShort.Named("TRACTOR")).Resolve(),
                new LookTargets(Vehicle, trailer), MessageTypeDefOf.RejectInput, historical: false);
        }
    }

    void Remember(TrailerPose pose)
    {
        axleX = (float)pose.AxleX;
        axleZ = (float)pose.AxleZ;
        heading = (float)pose.Heading;
    }

    /// <summary>Moves the trailer to the cells and facing its pose asks for, if they are clear, and turns its picture by the rest.</summary>
    bool TryPlace(TrailerPose pose, CompProperties_PhobosTrailer props)
    {
        VehiclePawn towed = trailer!;
        Map map = Vehicle.Map;
        (double cx, double cz) = HitchRule.CentreOf(pose, props.axleToCentre);
        int facing = HitchRule.Cardinal(pose.Heading);
        IntVec2 size = towed.def.Size;
        (int x, int z) = HitchRule.CellFor(cx, cz, size.x, size.z, facing);
        var cell = new IntVec3(x, 0, z);
        var rot = new Rot4(facing);
        if (cell != towed.Position || rot != towed.Rotation)
        {
            CellRect next = GenAdj.OccupiedRect(cell, rot, size);
            if (!Clear(next, map, towed)) return false;
            CellRect before = towed.OccupiedRect();
            // As Vehicle Framework moves a vehicle a cell: its own patches on Position and Rotation reclaim the cells and its regions.
            towed.Position = cell;
            towed.Rotation = rot;
            foreach (IntVec3 c in before.Cells.Concat(next.Cells).Distinct())
            {
                if (c.InBounds(map)) map.pathing.RecalculatePerceivedPathCostAt(c);
            }
        }
        towed.Transform.rotation = (float)HitchRule.Remainder(pose.Heading);
        return true;
    }

    /// <summary>Whether the trailer can stand on these cells: on the map, on ground a person can walk, and clear of every other vehicle
    /// (the one towing it excepted, which it may touch at the hitch).</summary>
    bool Clear(CellRect rect, Map map, VehiclePawn towed)
    {
        if (!rect.InBounds(map)) return false;
        foreach (IntVec3 c in rect)
        {
            if (!c.Walkable(map)) return false;
            List<Thing> things = c.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i] is VehiclePawn other && other != towed && other != Vehicle) return false;
            }
        }
        return true;
    }

    /// <summary>Where the hitch is now, in map units: behind the middle of the vehicle's picture along its heading.</summary>
    (double X, double Z) HitchPoint()
    {
        Vector3 centre = ((Thing)Vehicle).DrawPos;
        float behind = Vehicle.def.GetModExtension<PhobosHitchPointExtension>() is { behindCentre: >= 0f } point
            ? point.behindCentre
            : Vehicle.def.Size.z / 2f + Rules.hitchGapCells;
        double radians = VehicleHeading(Vehicle) * Mathf.Deg2Rad;
        return (centre.x - System.Math.Sin(radians) * behind, centre.z - System.Math.Cos(radians) * behind);
    }

    /// <summary>The way a vehicle points, in degrees (0 north, 90 east), from the eight ways Vehicle Framework draws it.</summary>
    public static double VehicleHeading(VehiclePawn vehicle)
    {
        Rot8 r = vehicle.FullRotation;
        if (r == Rot8.NorthEast) return 45;
        if (r == Rot8.East) return 90;
        if (r == Rot8.SouthEast) return 135;
        if (r == Rot8.South) return 180;
        if (r == Rot8.SouthWest) return 225;
        if (r == Rot8.West) return 270;
        if (r == Rot8.NorthWest) return 315;
        return 0;
    }

    // ---- Spawning, saving and losing the vehicle ----

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (pending != null) Hitches.NoteWaiting(this, true);
        if (trailer == null) return;
        Hitches.Note(this, trailer);
        needsLook = true;
        Vehicle.RequestTickStart(this);
    }

    public override void OnDeSpawn()
    {
        base.OnDeSpawn();
        Hitches.NoteWaiting(this, false);
        // A vehicle that leaves the map (a journey, a launch, a hold) leaves its trailer where it stood: towing on the world map is not built yet.
        if (trailer is { Spawned: true } left)
        {
            Unhitch(quiet: true);
            if (Vehicle.Faction == Faction.OfPlayer) Say("PhobosHitch_LeftBehind".Translate(left.LabelShort.Named("TRAILER")).Resolve(), left, MessageTypeDefOf.NeutralEvent);
        }
        else if (trailer != null)
        {
            Unhitch(quiet: true);
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        // Destroyed: what it towed stays where it stood, for anyone to hitch up (a raider's load, once its vehicle falls).
        if (trailer != null) Unhitch(quiet: true);
        Hitches.NoteWaiting(this, false);
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_References.Look(ref trailer, "PhobosHitch_Trailer");
        Scribe_References.Look(ref pending, "PhobosHitch_Pending");
        Scribe_Values.Look(ref axleX, "PhobosHitch_AxleX");
        Scribe_Values.Look(ref axleZ, "PhobosHitch_AxleZ");
        Scribe_Values.Look(ref heading, "PhobosHitch_Heading");
    }

    // ---- What the player sees ----

    static void Say(string text, MessageTypeDef type, bool historical = true)
    {
        if (!VehicleNotices.Quiet) Messages.Message(text, type, historical);
    }

    static void Say(string text, LookTargets where, MessageTypeDef type, bool historical = true)
    {
        if (!VehicleNotices.Quiet) Messages.Message(text, where, type, historical);
    }

    internal Command_Action UnhitchGizmo() => new Command_Action
    {
        defaultLabel = "PhobosHitch_UnhitchGizmo".Translate(),
        defaultDesc = "PhobosHitch_UnhitchGizmoDesc".Translate(trailer?.LabelShort.Named("TRAILER") ?? "".Named("TRAILER")),
        icon = SystemIcons.For("Phobos/VehicleSystems/Hitch/Unhitch"),
        action = () => Unhitch(),
    };

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        if (Vehicle.Faction != Faction.OfPlayer || !Vehicle.Spawned) yield break;
        if (trailer != null)
        {
            yield return UnhitchGizmo();
            yield break;
        }
        if (pending != null)
        {
            yield return new Command_Action
            {
                defaultLabel = "PhobosHitch_CancelGizmo".Translate(),
                defaultDesc = "PhobosHitch_CancelGizmoDesc".Translate(pending.LabelShort.Named("TRAILER")),
                icon = SystemIcons.For("Phobos/VehicleSystems/Hitch/Unhitch"),
                action = CancelPending,
            };
            yield break;
        }
        var hitch = new Command_Action
        {
            defaultLabel = "PhobosHitch_HitchGizmo".Translate(),
            defaultDesc = "PhobosHitch_HitchGizmoDesc".Translate(Rules.hitchReachCells.Named("CELLS")),
            icon = SystemIcons.For("Phobos/VehicleSystems/Hitch/Hitch"),
            action = () => Find.Targeter.BeginTargeting(new TargetingParameters
            {
                canTargetPawns = true,
                canTargetBuildings = false,
                validator = target => target.Thing is VehiclePawn candidate && TrailerProps(candidate) != null,
            }, target =>
            {
                if (target.Thing is VehiclePawn candidate) Choose(candidate);
            }),
        };
        if (Refusal() is { } refusal) hitch.Disable(refusal);
        yield return hitch;
    }

    public override string? CompInspectStringExtra()
    {
        if (UiGuard.Resting(GuardKey)) return UiGuard.FallbackText;
        try
        {
            if (trailer != null)
            {
                string line = "PhobosHitch_InspectTowing".Translate(trailer.LabelShort.Named("TRAILER"), TowedKilograms.ToString("N0").Named("WEIGHT")).Resolve();
                if (!FullModel && plainCap < 0.995f) line += " " + "PhobosHitch_InspectPlainCap".Translate(plainCap.ToStringPercent().Named("CAP")).Resolve();
                return line;
            }
            if (pending != null) return "PhobosHitch_InspectWaiting".Translate(pending.LabelShort.Named("TRAILER")).Resolve();
            return null;
        }
        catch (System.Exception e) { UiGuard.Report(GuardKey, e); return null; }
    }

    const string GuardKey = "A vehicle's hitch line";

    /// <summary>A drawbar from the hitch to the front of the trailer's picture, drawn with the vehicle, only while towing. The trailer's
    /// cells snap, and the bar stretches over the difference.</summary>
    public override void PostDraw()
    {
        base.PostDraw();
        if (trailer is not { Spawned: true } || TrailerProps(trailer) == null) return;
        (double hx, double hz) = HitchPoint();
        Vector3 trailerCentre = ((Thing)trailer).DrawPos;
        double radians = (heading) * Mathf.Deg2Rad;
        float half = trailer.def.Size.z / 2f;
        float y = AltitudeLayer.Pawn.AltitudeFor();
        var front = new Vector3(trailerCentre.x + (float)System.Math.Sin(radians) * half, y, trailerCentre.z + (float)System.Math.Cos(radians) * half);
        GenDraw.DrawLineBetween(new Vector3((float)hx, y, (float)hz), front, SimpleColor.White, 0.12f);
    }
}

/// <summary>
/// Which vehicle tows which trailer, and which hitches wait for a colonist: kept by the hitches themselves as they couple and let go, so a
/// trailer's question "who tows me?" and a hauler's search for work read a short list. Emptied when another game is loaded.
/// </summary>
public static class Hitches
{
    static readonly Dictionary<VehiclePawn, CompPhobosHitch> towing = new();
    static readonly HashSet<CompPhobosHitch> waiting = new();
    static Game? game;

    static void Check()
    {
        if (game == Current.Game) return;
        towing.Clear();
        waiting.Clear();
        game = Current.Game;
    }

    public static void Note(CompPhobosHitch hitch, VehiclePawn trailer)
    {
        Check();
        towing[trailer] = hitch;
    }

    public static void Forget(VehiclePawn trailer)
    {
        Check();
        towing.Remove(trailer);
    }

    /// <summary>The hitch towing this trailer now, or null.</summary>
    public static CompPhobosHitch? Towing(VehiclePawn trailer)
    {
        Check();
        return towing.TryGetValue(trailer, out CompPhobosHitch? hitch) && hitch.Trailer == trailer ? hitch : null;
    }

    public static void NoteWaiting(CompPhobosHitch hitch, bool isWaiting)
    {
        Check();
        if (isWaiting) waiting.Add(hitch);
        else waiting.Remove(hitch);
    }

    static bool StillWaiting(CompPhobosHitch hitch) => hitch.Pending is { Spawned: true } && hitch.Vehicle is { Spawned: true };

    public static CompPhobosHitch? WaitingOn(VehiclePawn trailer)
    {
        Check();
        foreach (CompPhobosHitch hitch in waiting)
        {
            if (hitch.Pending == trailer && StillWaiting(hitch)) return hitch;
        }
        return null;
    }

    /// <summary>The trailers on this map that wait for someone to couple them.</summary>
    public static IEnumerable<Thing> TrailersWaiting(Map map)
    {
        Check();
        foreach (CompPhobosHitch hitch in waiting)
        {
            if (StillWaiting(hitch) && hitch.Pending!.Map == map) yield return hitch.Pending;
        }
    }
}
