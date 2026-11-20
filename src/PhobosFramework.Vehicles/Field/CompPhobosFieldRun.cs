using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Bulk;
using Phobos.Framework.Environment;
using Phobos.Framework.Rules;
using RimWorld;
using SmashTools;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>A vehicle's field run, configured in its Def (Framework 0.182.0; docs/development/farming-batch-one.md).</summary>
public class CompProperties_PhobosFieldRun : VehicleCompProperties
{
    /// <summary>How long a drive may stand still before it counts as stuck, in ticks. Authored, as the gathering run's.</summary>
    public int stuckTicks = 600;
    /// <summary>How many rows in a row it fails to reach before it gives the run up. Authored.</summary>
    public int failedRowsToGiveUp = 4;
    /// <summary>Whether enemies threatening the map end the run.</summary>
    public bool stopForEnemies = true;
    /// <summary>How many bins it tries, nearest first, when its hold is full. Authored.</summary>
    public int holdsToTry = 4;

    public CompProperties_PhobosFieldRun() => compClass = typeof(CompPhobosFieldRun);
}

/// <summary>What a field run is doing. Saved by name: append only.</summary>
public enum FieldRunPhase
{
    Idle,
    /// <summary>Driving to the start of the next row, facing along it.</summary>
    ToRowStart,
    /// <summary>Driving a row, its implement working the strip at each cell.</summary>
    Row,
    /// <summary>Full, driving to a bin or cart to pour.</summary>
    ToHold,
    /// <summary>Pouring into a bin or cart.</summary>
    Pouring,
}

/// <summary>Why a field run ended. Saved by name: append only.</summary>
public enum FieldRunEnd
{
    None,
    Done,
    Full,
    NoHold,
    NoWay,
    Orders,
    EngineOff,
    Threat,
    Stopped,
    Fault,
    OutOfSupply,
    AreaGone,
}

/// <summary>
/// A vehicle working an area row by row by itself (Framework 0.182.0; docs/development/farming-batch-one.md, "Harvest this paddock"): the
/// row planner lays the rows (RowPlanRule), the run drives to each row's start facing along it and then drives the row, and its implement
/// (IVehicleFieldWorker) works the line of cells its strip covers each time the vehicle enters a cell (Vehicle Framework's position event).
/// At each row's end the implement settles its work; a full hold takes the vehicle to the nearest bin or cart with room to pour, then back
/// to the next row. Shares its driving with the gathering run (CompPhobosVehicleRun). Nothing ticks without a run.
/// </summary>
public class CompPhobosFieldRun : CompPhobosVehicleRun
{
    const string SavePrefix = "PhobosFieldRun";

    Zone? zone;
    FieldRunPhase phase;
    int rowIndex;
    IntVec3 planFrom = IntVec3.Invalid;
    bool alongX = true;
    /// <summary>
    /// Nought for the first pass; one for the sweep that drives every row with work left the other way round (the fault-read of 20 Nov
    /// 2026): a row whose start was moved in from a wall, or whose rest a full tank left standing, is finished from its far end.
    /// </summary>
    int pass;
    bool waiting;
    Zone? waitingZone;
    ThingWithComps? holdTarget;

    // Derived, never saved.
    List<FieldRow>? rows;
    HashSet<IntVec3>? zoneCells;
    IVehicleFieldWorker? worker;
    bool registered;
    readonly List<IntVec3> lineCells = new();
    readonly List<ThingWithComps> holdsTried = new();

    public CompProperties_PhobosFieldRun Props => (CompProperties_PhobosFieldRun)props;

    public override bool Running => phase != FieldRunPhase.Idle;

    /// <summary>Whether it drives a row now, its implement working: the implement's work load on the engine (When_FieldWorking).</summary>
    public bool WorkingRow => phase == FieldRunPhase.Row;

    /// <summary>Whether this run lowers that implement now: it drives a row, and the implement is the one working it (Framework 0.193.0: in a
    /// chain, a cart or a second implement behind the worker stays lifted and drags nothing).</summary>
    public bool Lowers(VehiclePawn implement) => phase == FieldRunPhase.Row && Worker != null && workerVehicle == implement;

    protected override bool WaitingForDriver => waiting;

    protected override int StuckTicks => Props.stuckTicks;

    VehiclePawn? workerVehicle;

    /// <summary>
    /// The implement that works the cells: on the vehicle itself (a combine), or on the implement its hitch holds (a plough, a drill:
    /// docs/development/farming-batch-two.md). Found again whenever what the hitch holds changes.
    /// </summary>
    IVehicleFieldWorker? Worker
    {
        get
        {
            if (worker != null && (workerVehicle == Vehicle || (workerVehicle != null && CompPhobosHitch.IsAheadOf(Vehicle, workerVehicle)))) return worker;
            worker = Vehicle.AllComps.OfType<IVehicleFieldWorker>().FirstOrDefault();
            workerVehicle = worker != null ? Vehicle : null;
            if (worker != null) return worker;
            // Framework 0.193.0: the nearest worker along the chain (a plough on the linkage, ahead of the cart it pulls).
            foreach (VehiclePawn behind in CompPhobosHitch.Behind(Vehicle))
            {
                worker = behind.AllComps.OfType<IVehicleFieldWorker>().FirstOrDefault();
                if (worker == null) continue;
                workerVehicle = behind;
                break;
            }
            return worker;
        }
    }

    /// <summary>The vehicle whose strip is worked: the implement behind, or the vehicle itself.</summary>
    VehiclePawn WorkerVehicle => Worker != null && workerVehicle != null ? workerVehicle : Vehicle;

    /// <summary>How far the implement behind stands from the vehicle's centre, in cells, held straight by the linkage; nought for none.</summary>
    int ImplementGap()
    {
        VehiclePawn implement = WorkerVehicle;
        if (implement == Vehicle || !implement.Spawned || !Vehicle.Spawned) return 0;
        IntVec3 d = implement.Position - Vehicle.Position;
        return (int)System.Math.Round(System.Math.Sqrt(d.x * d.x + d.z * d.z));
    }

    CompPhobosBulkHold? OwnHold => Vehicle.GetComp<CompPhobosBulkHold>();

    /// <summary>The area worked, or null: one deleted, emptied or on another map is dropped.</summary>
    Zone? Area
    {
        get
        {
            Map? map = Vehicle.MapHeld;
            if (zone != null && (map == null || zone.Map != map || !map.zoneManager.AllZones.Contains(zone) || zone.Cells.Count == 0)) zone = null;
            return zone;
        }
    }

    HashSet<IntVec3> ZoneCells => zoneCells ??= Area != null ? new HashSet<IntVec3>(Area.Cells) : new HashSet<IntVec3>();

    List<FieldRow> Rows
    {
        get
        {
            if (rows != null) return rows;
            IVehicleFieldWorker? w = Worker;
            if (Area == null || w == null) return rows = new List<FieldRow>();
            var cells = Area.Cells.Select(c => new FieldCell(c.x, c.z)).ToList();
            IntVec3 from = planFrom.IsValid ? planFrom : Vehicle.Position;
            rows = RowPlanRule.Plan(cells, System.Math.Max(1, w.StripWidth), alongX, new FieldCell(from.x, from.z));
            if (pass == 1)
            {
                // The sweep: the rows in the other order, each driven from its far end.
                rows.Reverse();
                for (int i = 0; i < rows.Count; i++) rows[i] = new FieldRow(rows[i].AlongX, rows[i].CrossMin, rows[i].CrossMax, rows[i].To, rows[i].From);
            }
            return rows;
        }
    }

    // ---- Starting ----

    /// <summary>Why it cannot work this area now, or null; with <paramref name="needsDriver"/> false, as if a driver were aboard.</summary>
    public string? CannotStart(Zone area, bool needsDriver)
    {
        NamedArgument name = Vehicle.LabelShort.Named("VEHICLE");
        IVehicleFieldWorker? w = Worker;
        if (w == null) return "PhobosFieldRun_NoWorker".Translate(name).Resolve();
        if (!w.Accepts(area)) return "PhobosFieldRun_NotThisArea".Translate(name, area.label.Named("AREA")).Resolve();
        if (w.CannotWork(area) is { } cannot) return cannot;
        if (Props.stopForEnemies && Vehicle.Spawned && GenHostility.AnyHostileActiveThreatToPlayer(Vehicle.Map)) return "PhobosGather_EnemiesNear".Translate().Resolve();
        if (OwnHold is { HasRoom: false } && !FindHolds().Any()) return "PhobosFieldRun_FullNoBin".Translate(name).Resolve();
        return VehicleAutoDrive.CannotDrive(Vehicle, needsDriver);
    }

    /// <summary>The order: at once with a driver aboard; otherwise, if nothing else stands in the way, it calls for one.</summary>
    public void Order(Zone area)
    {
        if (Running) return;
        if (Vehicle.HasEnoughOperators)
        {
            Start(area);
            return;
        }
        if (CannotStart(area, needsDriver: false) is { } refused)
        {
            Messages.Message(refused, Vehicle, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        waiting = true;
        waitingZone = area;
        CallForDriver(Worker!.OrderLabel, "field");
    }

    public void Start(Zone area)
    {
        if (Running) return;
        StopWaiting(false);
        if (CannotStart(area, needsDriver: true) is { } refused)
        {
            Messages.Message(refused, Vehicle, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        // Vehicle Framework says why if it will not start.
        if (!TakeOver()) return;
        zone = area;
        zoneCells = null;
        rows = null;
        planFrom = Vehicle.Position;
        alongX = RowPlanRule.LongerAlongX(area.Cells.Select(c => new FieldCell(c.x, c.z)).ToList());
        rowIndex = 0;
        pass = 0;
        failedDrives = 0;
        holdTarget = null;
        holdsTried.Clear();
        // Framework 0.193.0: the run takes over from any implement lowered by hand.
        Implements.RunStarted(Vehicle);
        Worker!.BeginRun(area);
        phase = FieldRunPhase.ToRowStart;
        Messages.Message("PhobosFieldRun_Started".Translate(Vehicle.LabelShort.Named("VEHICLE"), area.label.Named("AREA"), Rows.Count.Named("ROWS")).Resolve(),
            Vehicle, MessageTypeDefOf.SilentInput, historical: false);
        if (Seen.Wants("vehicles.field-run.start", Vehicle))
        {
            Seen.Note("vehicles.field-run.start", Vehicle, area.label, values: new List<KeyValuePair<string, double>> { new("rows", Rows.Count), new("cells", area.Cells.Count) });
        }
        NextRow();
    }

    void StopWaiting(bool say)
    {
        if (!waiting) return;
        waiting = false;
        waitingZone = null;
        CallOffDriver(say);
    }

    public void Stop() => End(FieldRunEnd.Stopped);

    // ---- The rows ----

    static Rot4 Facing(FieldRow row) => row.AlongX ? (row.Step > 0 ? Rot4.East : Rot4.West) : (row.Step > 0 ? Rot4.North : Rot4.South);

    /// <summary>
    /// The cell the body's centre stands on for its strip to lie across this row at this place along it (StripRule.BodyCrossMin and the
    /// body's own footprint facing along the row).
    /// </summary>
    IntVec3 CentreFor(FieldRow row, int stripAlong)
    {
        IVehicleFieldWorker w = Worker!;
        Rot4 rot = Facing(row);
        CellRect body = WorkerVehicle.VehicleRect(IntVec3.Zero, rot);
        int frontOffset = row.AlongX ? (row.Step > 0 ? body.maxX : body.minX) : (row.Step > 0 ? body.maxZ : body.minZ);
        int lowOffset = row.AlongX ? body.minZ : body.minX;
        int bodyWidth = row.AlongX ? body.Height : body.Width;
        int along = stripAlong - w.StripAhead * row.Step - frontOffset + ImplementGap() * row.Step;
        int cross = StripRule.BodyCrossMin(row.CrossMin, bodyWidth, System.Math.Max(1, w.StripWidth), row.AlongX, row.Step) - lowOffset;
        return row.AlongX ? new IntVec3(along, 0, cross) : new IntVec3(cross, 0, along);
    }

    /// <summary>Whether the vehicle, and the implement behind it, fit on drivable ground with the vehicle's centre here, facing this way.</summary>
    bool BodiesFit(IntVec3 centre, Rot4 rot)
    {
        Map map = Vehicle.Map;
        foreach (IntVec3 cell in Vehicle.VehicleRect(centre, rot))
        {
            if (!cell.InBounds(map) || !Vehicle.Drivable(cell)) return false;
        }
        VehiclePawn implement = WorkerVehicle;
        if (implement == Vehicle) return true;
        IntVec3 behind = centre - rot.FacingCell * ImplementGap();
        foreach (IntVec3 cell in implement.VehicleRect(behind, rot))
        {
            if (!cell.InBounds(map) || !implement.Drivable(cell)) return false;
        }
        return true;
    }

    /// <summary>
    /// Where to start a row: with the strip on its first cell if the body fits there, else moved in along the row until it does (a wall,
    /// a river or the map's edge at the row's start: the fault-read of 20 Nov 2026). The cells passed over are finished by the sweep, which
    /// drives the row from its other end. Invalid if the body fits nowhere along it.
    /// </summary>
    IntVec3 StartFor(FieldRow row)
    {
        Rot4 rot = Facing(row);
        for (int k = 0; k < row.Length; k++)
        {
            IntVec3 centre = CentreFor(row, row.From + k * row.Step);
            if (centre.InBounds(Vehicle.Map) && BodiesFit(centre, rot)) return centre;
        }
        return IntVec3.Invalid;
    }

    List<IntVec3> RowCells(FieldRow row)
    {
        var cells = new List<IntVec3>();
        HashSet<IntVec3> inZone = ZoneCells;
        for (int along = row.From; along != row.To + row.Step; along += row.Step)
        {
            for (int cross = row.CrossMin; cross <= row.CrossMax; cross++)
            {
                FieldCell cell = row.Cell(along, cross);
                var c = new IntVec3(cell.X, 0, cell.Z);
                if (inZone.Contains(c)) cells.Add(c);
            }
        }
        return cells;
    }

    /// <summary>Sets off for the next row with work in it; with none left the run is done.</summary>
    void NextRow()
    {
        if (Area == null)
        {
            End(FieldRunEnd.AreaGone);
            return;
        }
        List<FieldRow> plan = Rows;
        IVehicleFieldWorker w = Worker!;
        while (rowIndex < plan.Count && !w.HasWork(RowCells(plan[rowIndex]))) rowIndex++;
        if (rowIndex >= plan.Count)
        {
            if (pass == 0)
            {
                // The sweep: every row with work left, from its other end.
                pass = 1;
                rows = null;
                rowIndex = 0;
                failedDrives = 0;
                NextRow();
                return;
            }
            End(FieldRunEnd.Done);
            return;
        }
        FieldRow row = plan[rowIndex];
        IntVec3 start = StartFor(row);
        if (!start.IsValid || !BeginDrive(start, Facing(row)))
        {
            FailedRow();
            return;
        }
        phase = FieldRunPhase.ToRowStart;
    }

    void FailedRow()
    {
        failedDrives++;
        rowIndex++;
        if (VehicleRunRule.GiveUp(failedDrives, Props.failedRowsToGiveUp))
        {
            End(FieldRunEnd.NoWay);
            return;
        }
        NextRow();
    }

    /// <summary>At a row's start: faces along it, works the first line, and drives the row.</summary>
    void BeginRow()
    {
        FieldRow row = Rows[rowIndex];
        VehicleAutoDrive.Face(Vehicle, Facing(row));
        WorkLine();
        IntVec3 end = CentreFor(row, row.To);
        if (end == Vehicle.Position)
        {
            AfterRow();
            return;
        }
        if (!end.InBounds(Vehicle.Map) || !BeginDrive(end, Facing(row)))
        {
            FailedRow();
            return;
        }
        phase = FieldRunPhase.Row;
    }

    /// <summary>A row ended: the implement settles it; the run goes on, takes a full hold to a bin, or ends.</summary>
    void AfterRow()
    {
        failedDrives = 0;
        rowIndex++;
        FieldWorkStop stop = Worker!.RowDone(out string? reason);
        switch (stop)
        {
            case FieldWorkStop.None:
                NextRow();
                return;
            case FieldWorkStop.Full:
                holdsTried.Clear();
                GoToHold();
                return;
            case FieldWorkStop.OutOfSupply:
                End(FieldRunEnd.OutOfSupply, reason);
                return;
            default:
                End(FieldRunEnd.Fault, reason);
                return;
        }
    }

    /// <summary>The cells of the strip at the vehicle's front, inside the area, handed to the implement. Cheap: one line of cells.</summary>
    void WorkLine()
    {
        IVehicleFieldWorker? w = Worker;
        VehiclePawn body = WorkerVehicle;
        if (w == null || !body.Spawned) return;
        CellRect rect = body.OccupiedRect();
        IntVec3 facing = body.Rotation.FacingCell;
        StripLine line = StripRule.Front(rect.minX, rect.minZ, rect.maxX, rect.maxZ, facing.x, facing.z, System.Math.Max(1, w.StripWidth), w.StripAhead);
        lineCells.Clear();
        HashSet<IntVec3> inZone = ZoneCells;
        for (int cross = line.CrossMin; cross <= line.CrossMax; cross++)
        {
            FieldCell cell = line.Cell(cross);
            var c = new IntVec3(cell.X, 0, cell.Z);
            if (inZone.Contains(c)) lineCells.Add(c);
        }
        if (lineCells.Count > 0) w.WorkCells(lineCells);
    }

    /// <summary>Vehicle Framework's event each time the vehicle enters a cell: only a row being driven does anything.</summary>
    void OnMoved()
    {
        if (phase != FieldRunPhase.Row) return;
        WorkLine();
    }

    public override void EventRegistration()
    {
        base.EventRegistration();
        if (registered) return;
        registered = true;
        Vehicle.AddEvent(VehicleEventDefOf.PositionChanged, OnMoved);
    }

    // ---- A full hold ----

    /// <summary>The holds that could take what this vehicle holds, nearest first, other than its own and those already tried.</summary>
    IEnumerable<IBulkHold> FindHolds()
    {
        CompPhobosBulkHold? own = OwnHold;
        if (own == null) yield break;
        IntVec3 at = Vehicle.Position;
        foreach (IBulkHold hold in MapRegistry<IBulkHold>.On(Vehicle.MapHeld).OrderBy(h => h.HoldThing.Position.DistanceToSquared(at)))
        {
            if (hold == own || holdsTried.Contains(hold.HoldThing) || hold.HoldThing.Faction != Faction.OfPlayer) continue;
            // Never into another field machine's own tank (the fault-read of 20 Nov 2026: one combine chased another).
            if (hold.HoldThing.TryGetComp<CompPhobosFieldRun>() != null) continue;
            if (!own.Contents.Entries.Any(e => e.def != null && hold.Contents.Takes(e.def, hold.MixesProducts) && hold.Contents.RoomFor(e.def, hold.CapacityKg) > 0)) continue;
            yield return hold;
        }
    }

    /// <summary>Drives to the nearest bin or cart with room, or pours at once if one is in reach; with none, the run ends full.</summary>
    void GoToHold()
    {
        CompPhobosBulkHold? own = OwnHold;
        if (own == null)
        {
            End(FieldRunEnd.Full);
            return;
        }
        foreach (IBulkHold hold in FindHolds().Take(System.Math.Max(1, Props.holdsToTry - holdsTried.Count)))
        {
            holdsTried.Add(hold.HoldThing);
            holdTarget = hold.HoldThing;
            if (BulkPour.InReach(own, hold))
            {
                BeginPour(own, hold);
                return;
            }
            IntVec2 size = hold.HoldThing.def.Size;
            int ring = System.Math.Max(size.x, size.z) / 2 + System.Math.Min(Vehicle.def.Size.x, Vehicle.def.Size.z) / 2 + 1;
            if (VehicleAutoDrive.TryFindStandCell(Vehicle, hold.HoldThing.Position, ring, out IntVec3 cell) && Broadside(cell, hold, own) is { } facing
                && BeginDrive(cell, facing))
            {
                phase = FieldRunPhase.ToHold;
                if (Seen.Wants("vehicles.field-run.to-hold", Vehicle)) Seen.Note("vehicles.field-run.to-hold", Vehicle, hold.HoldThing.LabelShort);
                return;
            }
        }
        holdTarget = null;
        End(holdsTried.Count == 0 ? FieldRunEnd.Full : FieldRunEnd.NoHold);
    }

    /// <summary>
    /// The facing that puts the vehicle's long side towards the hold from this cell, if its auger then reaches it; null if it would not (the
    /// fault-read of 20 Nov 2026: arriving end-on to a long chaser bin left it out of reach).
    /// </summary>
    Rot4? Broadside(IntVec3 cell, IBulkHold hold, CompPhobosBulkHold own)
    {
        IntVec3 d = hold.HoldThing.Position - cell;
        Rot4 facing = System.Math.Abs(d.x) >= System.Math.Abs(d.z) ? Rot4.North : Rot4.East;
        CellRect body = Vehicle.VehicleRect(cell, facing), other = hold.HoldThing.OccupiedRect();
        return BulkHoldRule.WithinReach(body.minX, body.minZ, body.maxX, body.maxZ, other.minX, other.minZ, other.maxX, other.maxZ, own.ReachCells)
            ? facing : null;
    }

    void BeginPour(CompPhobosBulkHold own, IBulkHold hold)
    {
        own.PourInto(hold);
        if (!own.Pouring)
        {
            GoToHold();
            return;
        }
        phase = FieldRunPhase.Pouring;
    }

    // ---- The rare tick ----

    public override void CompTickRare()
    {
        base.CompTickRare();
        if (!Vehicle.Spawned) return;
        if (phase == FieldRunPhase.Idle)
        {
            WaitTick();
            return;
        }
        if (Vehicle.Faction != Faction.OfPlayer)
        {
            End(FieldRunEnd.Orders);
            return;
        }
        if (Props.stopForEnemies && GenHostility.AnyHostileActiveThreatToPlayer(Vehicle.Map))
        {
            End(FieldRunEnd.Threat);
            return;
        }
        switch (phase)
        {
            case FieldRunPhase.ToRowStart:
                ToRowStartTick();
                break;
            case FieldRunPhase.Row:
                RowTick();
                break;
            case FieldRunPhase.ToHold:
                ToHoldTick();
                break;
            case FieldRunPhase.Pouring:
                PouringTick();
                break;
        }
    }

    void WaitTick()
    {
        if (!waiting || !Vehicle.HasEnoughOperators) return;
        Zone? area = waitingZone;
        StopWaiting(false);
        if (area != null && Vehicle.MapHeld?.zoneManager.AllZones.Contains(area) == true) Start(area);
    }

    /// <summary>Ends the run if a look at its drive found the engine off or another order; says whether it did.</summary>
    bool DriveEnded(DriveWatch drive)
    {
        if (drive == DriveWatch.EngineOff) End(FieldRunEnd.EngineOff);
        else if (drive == DriveWatch.OtherOrders) End(FieldRunEnd.Orders);
        else return false;
        return true;
    }

    void ToRowStartTick()
    {
        DriveWatch drive = WatchDrive();
        if (DriveEnded(drive) || drive == DriveWatch.OnWay) return;
        if (drive == DriveWatch.Arrived)
        {
            BeginRow();
            return;
        }
        HaltDrive();
        FailedRow();
    }

    void RowTick()
    {
        DriveWatch drive = WatchDrive();
        if (DriveEnded(drive) || drive == DriveWatch.OnWay) return;
        // Stuck or stopped short partway: what was worked is settled, and the run moves on.
        if (drive == DriveWatch.Stuck) HaltDrive();
        AfterRow();
    }

    void ToHoldTick()
    {
        DriveWatch drive = WatchDrive();
        if (DriveEnded(drive) || drive == DriveWatch.OnWay) return;
        if (drive == DriveWatch.Stuck) HaltDrive();
        CompPhobosBulkHold? own = OwnHold;
        IBulkHold? hold = BulkPour.HoldOf(holdTarget);
        if (own != null && hold != null && BulkPour.InReach(own, hold)) BeginPour(own, hold);
        else GoToHold();
    }

    void PouringTick()
    {
        CompPhobosBulkHold? own = OwnHold;
        if (own == null)
        {
            End(FieldRunEnd.Fault);
            return;
        }
        if (own.Pouring) return;
        holdTarget = null;
        // Room again: back to the rows. Still full (the bin filled): the next bin.
        if (own.HasRoom)
        {
            holdsTried.Clear();
            NextRow();
        }
        else GoToHold();
    }

    // ---- Ending ----

    void End(FieldRunEnd why, string? reason = null)
    {
        if (phase == FieldRunPhase.Idle) return;
        FieldRunPhase was = phase;
        phase = FieldRunPhase.Idle;
        if (was is FieldRunPhase.ToRowStart or FieldRunPhase.Row or FieldRunPhase.ToHold && standCell.IsValid && Vehicle.Spawned) HaltDrive();
        HandBack(why is not (FieldRunEnd.Orders or FieldRunEnd.EngineOff or FieldRunEnd.Threat));
        string done = Worker?.EndRun(why == FieldRunEnd.Done) ?? "";
        NamedArgument name = Vehicle.LabelShort.Named("VEHICLE");
        string because = reason ?? WhyLine(why, name);
        string head = "PhobosFieldRun_Ended".Translate(name, (zone?.label ?? "").Named("AREA")).Resolve();
        string text = string.Join(" ", new[] { head, done, because }.Where(s => !s.NullOrEmpty()));
        Messages.Message(text, Vehicle, why is FieldRunEnd.Done or FieldRunEnd.Stopped ? MessageTypeDefOf.TaskCompletion : MessageTypeDefOf.CautionInput);
        if (Seen.Wants("vehicles.field-run.end", Vehicle, why.ToString()))
        {
            Seen.Note("vehicles.field-run.end", Vehicle, variant: why.ToString(), values: new List<KeyValuePair<string, double>> { new("rows", rowIndex) });
        }
        zone = null;
        zoneCells = null;
        rows = null;
        holdTarget = null;
        holdsTried.Clear();
        planFrom = IntVec3.Invalid;
        failedDrives = 0;
    }

    static string WhyLine(FieldRunEnd why, NamedArgument name) => why switch
    {
        FieldRunEnd.Done => "PhobosFieldRun_EndDone".Translate().Resolve(),
        FieldRunEnd.Full => "PhobosFieldRun_EndFull".Translate(name).Resolve(),
        FieldRunEnd.NoHold => "PhobosFieldRun_EndNoHold".Translate(name).Resolve(),
        FieldRunEnd.NoWay => "PhobosFieldRun_EndNoWay".Translate().Resolve(),
        FieldRunEnd.Orders => "PhobosGather_EndOrders".Translate(name).Resolve(),
        FieldRunEnd.EngineOff => "PhobosGather_EndEngineOff".Translate().Resolve(),
        FieldRunEnd.Threat => "PhobosGather_EndThreat".Translate().Resolve(),
        FieldRunEnd.Stopped => "PhobosGather_EndStopped".Translate().Resolve(),
        FieldRunEnd.AreaGone => "PhobosFieldRun_EndAreaGone".Translate().Resolve(),
        FieldRunEnd.Fault => "PhobosGather_EndFault".Translate().Resolve(),
        FieldRunEnd.OutOfSupply => "PhobosFieldRun_EndOutOfSupply".Translate(name).Resolve(),
        _ => "",
    };

    // ---- The map, the save and the screen ----

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        // Leaving the map (a caravan, a launch) ends a run where it is, and a wait for a driver: the player sent it.
        DriverCalls.Call(map, this, false);
        waiting = false;
        waitingZone = null;
        phase = FieldRunPhase.Idle;
        engineStarted = false;
        zone = null;
        zoneCells = null;
        rows = null;
        holdTarget = null;
        base.PostDeSpawn(map, mode);
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        // The call for a driver is not saved with the map: one waiting when the game was saved calls again.
        if (waiting) DriverCalls.Call(Vehicle.Map, this, true);
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        bool saving = Scribe.mode == LoadSaveMode.Saving;
        if (saving)
        {
            _ = Area;
            if (holdTarget is { Destroyed: true }) holdTarget = null;
            // A paddock deleted while the vehicle waited for a driver is dropped, not saved as a dead reference (the fault-read).
            if (waitingZone != null && (waitingZone.Map == null || !waitingZone.Map.zoneManager.AllZones.Contains(waitingZone)))
            {
                waitingZone = null;
                waiting = false;
            }
        }
        ExposeRun(SavePrefix);
        Scribe_Values.Look(ref phase, SavePrefix + "Phase", FieldRunPhase.Idle);
        if (!saving || zone != null) Scribe_References.Look(ref zone, SavePrefix + "Zone");
        Scribe_Values.Look(ref rowIndex, SavePrefix + "Row");
        Scribe_Values.Look(ref planFrom, SavePrefix + "PlanFrom", IntVec3.Invalid);
        Scribe_Values.Look(ref alongX, SavePrefix + "AlongX", true);
        Scribe_Values.Look(ref pass, SavePrefix + "Pass");
        Scribe_Values.Look(ref waiting, SavePrefix + "Waiting");
        if (!saving || waitingZone != null) Scribe_References.Look(ref waitingZone, SavePrefix + "WaitingZone");
        if (!saving || holdTarget != null) Scribe_References.Look(ref holdTarget, SavePrefix + "Hold");
        if (Scribe.mode == LoadSaveMode.PostLoadInit && phase != FieldRunPhase.Idle && zone == null) phase = FieldRunPhase.Idle;
    }

    public override string? CompInspectStringExtra()
    {
        NamedArgument area = (zone?.label ?? "").Named("AREA");
        return phase switch
        {
            FieldRunPhase.ToRowStart or FieldRunPhase.Row => "PhobosFieldRun_LineRow".Translate(area, (rowIndex + 1).Named("ROW"), Rows.Count.Named("ROWS")).Resolve(),
            FieldRunPhase.ToHold => "PhobosFieldRun_LineToHold".Translate((holdTarget?.LabelShort ?? "").Named("HOLD")).Resolve(),
            FieldRunPhase.Pouring => "PhobosFieldRun_LinePouring".Translate((holdTarget?.LabelShort ?? "").Named("HOLD")).Resolve(),
            _ => waiting ? "PhobosDriverCall_Line".Translate((Worker?.OrderLabel ?? "").Named("WHAT")).Resolve() : null,
        };
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        IVehicleFieldWorker? w = Worker;
        if (w == null || Vehicle.Faction != Faction.OfPlayer) yield break;
        NamedArgument name = Vehicle.LabelShort.Named("VEHICLE");
        var order = new Command_Action
        {
            defaultLabel = Running ? "PhobosFieldRun_GizmoStop".Translate().Resolve() : waiting ? "PhobosDriverCall_StopWaiting".Translate().Resolve() : w.OrderLabel,
            defaultDesc = w.OrderDescription + "\n\n" + DriverCalls.WaitsLine,
            icon = SystemIcons.For(w.OrderIconPath),
            action = () =>
            {
                if (Running) Stop();
                else if (waiting) StopWaiting(true);
                else Find.WindowStack.Add(new FloatMenu(AreaOptions()));
            },
        };
        if (!Running && !waiting && Vehicle.MapHeld is { } map && !map.zoneManager.AllZones.Any(w.Accepts))
        {
            order.Disable("PhobosFieldRun_NoArea".Translate(name).Resolve());
        }
        yield return order;
    }

    List<FloatMenuOption> AreaOptions()
    {
        var options = new List<FloatMenuOption>();
        Map? map = Vehicle.MapHeld;
        IVehicleFieldWorker? w = Worker;
        if (map == null || w == null) return options;
        IntVec3 from = Vehicle.PositionHeld;
        foreach (Zone area in map.zoneManager.AllZones.Where(w.Accepts).OrderBy(z => z.Cells.Count == 0 ? 0 : z.Cells[0].DistanceToSquared(from)))
        {
            string? refused = CannotStart(area, needsDriver: false);
            string label = "PhobosFieldRun_AreaOption".Translate(area.label.Named("AREA"), area.Cells.Count.Named("CELLS")).Resolve();
            options.Add(refused == null
                ? new FloatMenuOption(label, () => Order(area))
                : new FloatMenuOption(label + " (" + refused + ")", null));
        }
        return options;
    }
}
