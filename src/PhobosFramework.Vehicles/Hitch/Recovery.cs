using System.Collections.Generic;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>How a towed vehicle follows its hitch: from the hitch to its axle, from the axle to the middle of its picture, and how far it may
/// swing from the vehicle towing it, in cells and degrees.</summary>
public readonly record struct TowGeometry(double HitchToAxle, double AxleToCentre, double JackKnife);

/// <summary>
/// Recovery (Framework 0.103.0; the recovery family of docs/development/vehicle-upgrade-families.md, built as one piece with the hitch's
/// recovery tow, owner, 12 Nov 2026). A vehicle with the recovery winch fitted can:
/// <list type="bullet">
/// <item><b>Winch</b> a stuck, broken or empty vehicle towards itself, a cell at a time at winch pace, while someone aboard works it, if
/// <see cref="RecoveryRule"/> says the winch (held by this vehicle's brakes) can overcome the stuck vehicle's weight on its ground.</item>
/// <item><b>Tow</b> an ordinary land vehicle, not only a trailer, on the hitch it already has: the towed vehicle follows on a tow bar by the
/// same follow rule, and its weight reaches the drivetrain as the towed mass. Whoever is aboard it stays aboard and cannot drive it off.</item>
/// </list>
/// Moves are whole cells, as the hitch's are, set the way Vehicle Framework moves a vehicle (its Position). Nothing runs each tick unless a
/// winch is working or a tow is moving. No Harmony.
/// </summary>
public sealed partial class CompPhobosHitch
{
    /// <summary>The ability the recovery winch fitting grants.</summary>
    public const string RecoveryAbility = "RecoveryWinch";

    VehiclePawn? winchTarget;
    int winchWait;
    double winchRequired = -1, winchAvailable = -1;

    /// <summary>Whether this vehicle has a working recovery winch (the fitting, on a mount not destroyed).</summary>
    public bool CanWinch => Systems?.Fittings.Grants(RecoveryAbility) == true;

    /// <summary>Whether this vehicle may take an ordinary vehicle on its hitch: the winch's tow bar, and a hitch that can tow at all.</summary>
    bool CanRecover => CanWinch;

    /// <summary>The vehicle the winch is pulling, or null.</summary>
    public VehiclePawn? Winching => winchTarget;

    /// <summary>A vehicle the recovery tow can take: a land vehicle that is not a trailer and not an aircraft.</summary>
    static bool IsRecoveryCandidate(VehiclePawn vehicle) =>
        vehicle.VehicleDef.type == VehicleType.Land && vehicle.GetComp<CompVehicleLauncher>() == null && vehicle.GetComp<CompPhobosTrailer>() == null;

    /// <summary>A towed vehicle's front on the tow bar, its rear wheels the axle that trails, a quarter of its length behind its middle
    /// (authored: most vehicles carry their rear axle about there).</summary>
    static TowGeometry RecoveryGeometry(VehiclePawn towed)
    {
        PhobosVehicleRulesDef rules = Rules;
        double length = towed.def.Size.z;
        return new TowGeometry(rules.recoveryTowBarCells + length * 0.75, length * 0.25, rules.recoveryJackKnifeDegrees);
    }

    /// <summary>Why this vehicle cannot take <paramref name="candidate"/>, an ordinary vehicle, on the recovery tow, or null.</summary>
    string? RecoveryTowRefusal(VehiclePawn candidate)
    {
        if (!CanRecover) return "PhobosRecovery_NotATrailerNoWinch".Translate(candidate.LabelShort.Named("TOWED")).Resolve();
        if (candidate.GetComp<CompVehicleLauncher>() != null) return "PhobosRecovery_NoAircraft".Translate(candidate.LabelShort.Named("TOWED")).Resolve();
        if (candidate.VehicleDef.type != VehicleType.Land) return "PhobosRecovery_LandOnly".Translate(candidate.LabelShort.Named("TOWED")).Resolve();
        if (candidate.Drafted) return "PhobosRecovery_EngineOn".Translate(candidate.LabelShort.Named("TOWED")).Resolve();
        if (candidate.GetComp<CompPhobosHitch>() is { } theirs && (theirs.Trailer != null || theirs.Winching != null))
        {
            return "PhobosRecovery_TowingSomething".Translate(candidate.LabelShort.Named("TOWED")).Resolve();
        }
        double tractorKg = CompPhobosSling.LoadKilograms(Vehicle), towedKg = CompPhobosSling.LoadKilograms(candidate);
        if (!RecoveryRule.CanTow(tractorKg, towedKg, Rules.recoveryMaxTowRatio))
        {
            return "PhobosRecovery_TooHeavyToTow".Translate(candidate.LabelShort.Named("TOWED"), towedKg.ToString("N0").Named("WEIGHT"),
                (tractorKg * Rules.recoveryMaxTowRatio).ToString("N0").Named("LIMIT"), Vehicle.LabelShort.Named("TRACTOR")).Resolve();
        }
        return null;
    }

    // ---- The winch's figures ----

    /// <summary>Whether a vehicle's wheels or tracks turn, so it rolls rather than being dragged: Vehicle Framework gives a vehicle whose
    /// running gear is destroyed no speed.</summary>
    static bool Rolls(VehiclePawn vehicle) => vehicle.GetStatValue(VehicleStatDefOf.MoveSpeed) > 0.1f;

    static (double Rolling, double Grip) GroundAt(Map map, IntVec3 cell)
    {
        double cost = Powertrain.PathCostAt(map, cell);
        return (DrivetrainModel.RollingResistanceForPathCost(cost), DrivetrainModel.GripForPathCost(cost));
    }

    /// <summary>The pull it takes to move <paramref name="stuck"/> where it stands, in kilograms-force.</summary>
    double RequiredFor(VehiclePawn stuck)
    {
        (double rolling, double grip) = GroundAt(stuck.Map, stuck.Position);
        return RecoveryRule.Required(CompPhobosSling.LoadKilograms(stuck), RecoveryRule.Resistance(rolling, grip, Rolls(stuck), Rules.winchMireAdd));
    }

    /// <summary>The pull this vehicle's winch can give where it stands, in kilograms-force.</summary>
    double Available()
    {
        (_, double grip) = GroundAt(Vehicle.Map, Vehicle.Position);
        return RecoveryRule.Available(Rules.winchRatedKgf, CompPhobosSling.LoadKilograms(Vehicle), grip);
    }

    /// <summary>The empty cells between this vehicle's footprint and another's, the larger of the two axes.</summary>
    int GapTo(VehiclePawn other)
    {
        CellRect a = Vehicle.OccupiedRect(), b = other.OccupiedRect();
        int gapX = Mathf.Max(0, Mathf.Max(a.minX - b.maxX, b.minX - a.maxX) - 1);
        int gapZ = Mathf.Max(0, Mathf.Max(a.minZ - b.maxZ, b.minZ - a.maxZ) - 1);
        return Mathf.Max(gapX, gapZ);
    }

    /// <summary>Close enough that the winch has done its work: at most one cell between the two.</summary>
    bool Beside(VehiclePawn other) => GapTo(other) <= 1;

    // ---- What the player orders ----

    /// <summary>Why this vehicle cannot work its winch now, or null.</summary>
    public string? WinchRefusal()
    {
        if (!CanWinch) return "PhobosRecovery_NoWinch".Translate(Vehicle.LabelShort.Named("TRACTOR")).Resolve();
        if (trailer != null) return "PhobosRecovery_UnhitchFirst".Translate(Vehicle.LabelShort.Named("TRACTOR"), trailer.LabelShort.Named("TOWED")).Resolve();
        if (pending != null) return "PhobosRecovery_WaitingToHitch".Translate(Vehicle.LabelShort.Named("TRACTOR"), pending.LabelShort.Named("TOWED")).Resolve();
        if (Hitches.Towing(Vehicle) is { } by) return "PhobosRecovery_BeingTowed".Translate(Vehicle.LabelShort.Named("TRACTOR"), by.Vehicle.LabelShort.Named("TOW")).Resolve();
        if (Vehicle.AllPawnsAboard.Count == 0) return "PhobosRecovery_NobodyAboard".Translate(Vehicle.LabelShort.Named("TRACTOR")).Resolve();
        if (Vehicle.vehiclePather is { Moving: true }) return "PhobosHitch_Moving".Translate(Vehicle.LabelShort.Named("TRACTOR")).Resolve();
        return null;
    }

    /// <summary>Why this vehicle's winch cannot pull <paramref name="stuck"/> now, or null.</summary>
    public string? WinchRefusalFor(VehiclePawn stuck)
    {
        if (stuck == Vehicle) return "PhobosRecovery_NotItself".Translate().Resolve();
        if (stuck.Faction != Faction.OfPlayer) return "PhobosRecovery_NotOurs".Translate(stuck.LabelShort.Named("TOWED")).Resolve();
        if (!stuck.Spawned || stuck.Map != Vehicle.Map) return "PhobosRecovery_NotHere".Translate(stuck.LabelShort.Named("TOWED")).Resolve();
        if (stuck.GetComp<CompVehicleLauncher>() != null) return "PhobosRecovery_NoAircraft".Translate(stuck.LabelShort.Named("TOWED")).Resolve();
        if (stuck.VehicleDef.type != VehicleType.Land) return "PhobosRecovery_LandOnly".Translate(stuck.LabelShort.Named("TOWED")).Resolve();
        if (Hitches.Towing(stuck) is { } other) return "PhobosRecovery_AlreadyTowed".Translate(stuck.LabelShort.Named("TOWED"), other.Vehicle.LabelShort.Named("TOW")).Resolve();
        if (GapTo(stuck) > Rules.winchReachCells) return "PhobosRecovery_TooFar".Translate(stuck.LabelShort.Named("TOWED"), Rules.winchReachCells.Named("CELLS")).Resolve();
        if (Beside(stuck)) return "PhobosRecovery_AlreadyBeside".Translate(stuck.LabelShort.Named("TOWED"), Vehicle.LabelShort.Named("TRACTOR")).Resolve();
        if (stuck.vehiclePather is { Moving: true }) return "PhobosRecovery_TowedMoving".Translate(stuck.LabelShort.Named("TOWED")).Resolve();
        double required = RequiredFor(stuck), available = Available();
        if (!RecoveryRule.CanPull(required, available))
        {
            return "PhobosRecovery_TooHeavyToPull".Translate(stuck.LabelShort.Named("TOWED"), required.ToString("N0").Named("REQUIRED"),
                available.ToString("N0").Named("AVAILABLE"), Vehicle.LabelShort.Named("TRACTOR")).Resolve();
        }
        return null;
    }

    public void StartWinch(VehiclePawn stuck)
    {
        if ((WinchRefusal() ?? WinchRefusalFor(stuck)) is { } refusal)
        {
            Say(refusal, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        winchTarget = stuck;
        winchRequired = RequiredFor(stuck);
        winchAvailable = Available();
        winchWait = RecoveryRule.TicksPerCell(Rules.winchTicksPerCell, winchRequired, winchAvailable);
        Vehicle.RequestTickStart(this);
        Say("PhobosRecovery_WinchStarted".Translate(Vehicle.LabelShort.Named("TRACTOR"), stuck.LabelShort.Named("TOWED"),
            winchRequired.ToString("N0").Named("REQUIRED"), winchAvailable.ToString("N0").Named("AVAILABLE")).Resolve(),
            new LookTargets(Vehicle, stuck), MessageTypeDefOf.TaskCompletion, historical: false);
    }

    /// <summary>Stops the winch, with this message for the player or none.</summary>
    void StopWinch(string? message, MessageTypeDef? type = null)
    {
        VehiclePawn? was = winchTarget;
        winchTarget = null;
        winchWait = 0;
        winchRequired = winchAvailable = -1;
        if (trailer == null) Vehicle.RequestTickStop(this);
        if (message != null && Vehicle.Faction == Faction.OfPlayer)
        {
            Say(message, was != null ? new LookTargets(Vehicle, was) : new LookTargets(Vehicle), type ?? MessageTypeDefOf.NeutralEvent, historical: false);
        }
    }

    /// <summary>One tick of the winch: checks it may go on, and every so often drags the stuck vehicle one cell nearer.</summary>
    void WinchTick()
    {
        VehiclePawn stuck = winchTarget!;
        string tractor = Vehicle.LabelShort, towed = stuck.LabelShort;
        if (!stuck.Spawned || !Vehicle.Spawned || stuck.Map != Vehicle.Map)
        {
            StopWinch(null);
            return;
        }
        if (Vehicle.vehiclePather is { Moving: true })
        {
            StopWinch("PhobosRecovery_StoppedTractorMoved".Translate(tractor.Named("TRACTOR"), towed.Named("TOWED")).Resolve());
            return;
        }
        if (stuck.vehiclePather is { Moving: true })
        {
            StopWinch("PhobosRecovery_StoppedTowedMoved".Translate(tractor.Named("TRACTOR"), towed.Named("TOWED")).Resolve());
            return;
        }
        if (Vehicle.AllPawnsAboard.Count == 0)
        {
            StopWinch("PhobosRecovery_StoppedNobodyAboard".Translate(tractor.Named("TRACTOR"), towed.Named("TOWED")).Resolve());
            return;
        }
        if (--winchWait > 0) return;
        if (Beside(stuck))
        {
            StopWinch("PhobosRecovery_WinchDone".Translate(tractor.Named("TRACTOR"), towed.Named("TOWED")).Resolve(), MessageTypeDefOf.TaskCompletion);
            return;
        }
        winchRequired = RequiredFor(stuck);
        winchAvailable = Available();
        if (!RecoveryRule.CanPull(winchRequired, winchAvailable))
        {
            StopWinch("PhobosRecovery_WinchStalled".Translate(tractor.Named("TRACTOR"), towed.Named("TOWED"), winchRequired.ToString("N0").Named("REQUIRED"),
                winchAvailable.ToString("N0").Named("AVAILABLE")).Resolve(), MessageTypeDefOf.RejectInput);
            return;
        }
        if (!TryDrag(stuck))
        {
            StopWinch("PhobosRecovery_WinchBlocked".Translate(tractor.Named("TRACTOR"), towed.Named("TOWED")).Resolve(), MessageTypeDefOf.RejectInput);
            return;
        }
        if (Beside(stuck))
        {
            StopWinch("PhobosRecovery_WinchDone".Translate(tractor.Named("TRACTOR"), towed.Named("TOWED")).Resolve(), MessageTypeDefOf.TaskCompletion);
            return;
        }
        winchWait = RecoveryRule.TicksPerCell(Rules.winchTicksPerCell, winchRequired, winchAvailable);
    }

    /// <summary>Drags the stuck vehicle one cell towards this one: straight along the line if it can, otherwise along either axis alone.</summary>
    bool TryDrag(VehiclePawn stuck)
    {
        Map map = Vehicle.Map;
        (int dx, int dz) = RecoveryRule.Step(stuck.Position.x, stuck.Position.z, Vehicle.Position.x, Vehicle.Position.z);
        var tries = new List<IntVec3> { new(dx, 0, dz) };
        if (dx != 0 && dz != 0)
        {
            tries.Add(new IntVec3(dx, 0, 0));
            tries.Add(new IntVec3(0, 0, dz));
        }
        foreach (IntVec3 step in tries)
        {
            if (step == IntVec3.Zero) continue;
            IntVec3 cell = stuck.Position + step;
            CellRect next = GenAdj.OccupiedRect(cell, stuck.Rotation, stuck.def.Size);
            if (!ClearFor(next, map, stuck)) continue;
            CellRect before = stuck.OccupiedRect();
            // As Vehicle Framework moves a vehicle a cell (its own patches on Position reclaim the cells and its regions), as the hitch does.
            stuck.Position = cell;
            foreach (IntVec3 c in before.Cells)
            {
                if (c.InBounds(map)) map.pathing.RecalculatePerceivedPathCostAt(c);
            }
            foreach (IntVec3 c in next.Cells)
            {
                if (c.InBounds(map)) map.pathing.RecalculatePerceivedPathCostAt(c);
            }
            return true;
        }
        return false;
    }

    /// <summary>Whether a dragged vehicle can stand on these cells: on the map, on walkable ground, clear of every other vehicle.</summary>
    static bool ClearFor(CellRect rect, Map map, VehiclePawn mover)
    {
        if (!rect.InBounds(map)) return false;
        foreach (IntVec3 c in rect)
        {
            if (!c.Walkable(map)) return false;
            List<Thing> things = c.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i] is VehiclePawn other && other != mover) return false;
            }
        }
        return true;
    }

    // ---- What the player sees ----

    Command_Action WinchGizmo()
    {
        var command = new Command_Action
        {
            defaultLabel = "PhobosRecovery_WinchGizmo".Translate(),
            defaultDesc = "PhobosRecovery_WinchGizmoDesc".Translate(Rules.winchReachCells.Named("CELLS"), Rules.winchRatedKgf.ToString("N0").Named("RATED")),
            icon = SystemIcons.For("Phobos/VehicleSystems/Hitch/Winch"),
            action = () => Find.Targeter.BeginTargeting(new TargetingParameters
            {
                canTargetPawns = true,
                canTargetBuildings = false,
                validator = target => target.Thing is VehiclePawn stuck && stuck != Vehicle,
            }, target =>
            {
                if (target.Thing is VehiclePawn stuck) StartWinch(stuck);
            }),
        };
        if (WinchRefusal() is { } refusal) command.Disable(refusal);
        return command;
    }

    Command_Action StopWinchGizmo() => new()
    {
        defaultLabel = "PhobosRecovery_StopWinchGizmo".Translate(),
        defaultDesc = "PhobosRecovery_StopWinchGizmoDesc".Translate(winchTarget?.LabelShort.Named("TOWED") ?? "".Named("TOWED")),
        icon = SystemIcons.For("Phobos/VehicleSystems/Hitch/StopWinch"),
        action = () => StopWinch("PhobosRecovery_WinchStopped".Translate(Vehicle.LabelShort.Named("TRACTOR"),
            (winchTarget?.LabelShort ?? "").Named("TOWED")).Resolve(), MessageTypeDefOf.SilentInput),
    };

    string WinchInspectLine()
    {
        VehiclePawn stuck = winchTarget!;
        if (winchRequired < 0 && stuck.Spawned && Vehicle.Spawned)
        {
            winchRequired = RequiredFor(stuck);
            winchAvailable = Available();
        }
        return "PhobosRecovery_InspectWinching".Translate(stuck.LabelShort.Named("TOWED"), Mathf.Max(0, GapTo(stuck) - 1).Named("CELLS"),
            System.Math.Max(0, winchRequired).ToString("N0").Named("REQUIRED"), System.Math.Max(0, winchAvailable).ToString("N0").Named("AVAILABLE")).Resolve();
    }

    /// <summary>The winch rope, from this vehicle's front to the vehicle it pulls, drawn only while the winch works.</summary>
    void DrawWinchLine()
    {
        Vector3 centre = ((Thing)Vehicle).DrawPos;
        double radians = VehicleHeading(Vehicle) * Mathf.Deg2Rad;
        float half = Vehicle.def.Size.z / 2f;
        float y = AltitudeLayer.Pawn.AltitudeFor();
        var front = new Vector3(centre.x + (float)System.Math.Sin(radians) * half, y, centre.z + (float)System.Math.Cos(radians) * half);
        Vector3 stuck = ((Thing)winchTarget!).DrawPos;
        GenDraw.DrawLineBetween(front, new Vector3(stuck.x, y, stuck.z), SimpleColor.White, 0.06f);
    }
}
