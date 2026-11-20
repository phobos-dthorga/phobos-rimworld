using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Phobos.Framework.Rules;
using Phobos.Framework.Upgrades;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>What one service yard's last look found: its equipment, allowance, vehicles and its dyno line. Derived, never saved.</summary>
public sealed class YardState
{
    public readonly Zone_PhobosServiceYard Yard;
    public int Cells;
    public int Allowance;
    public bool HasDyno;
    public bool Works;
    public readonly List<Thing> Equipment = new();
    public readonly List<CompPhobosTuningRig> Dynos = new();
    public readonly List<CompPhobosFuelPump> Pumps = new();
    /// <summary>The player's vehicles standing in the yard (their centre on its cells).</summary>
    public readonly List<VehiclePawn> Vehicles = new();
    /// <summary>The dyno line in turn order: the first <see cref="Turns"/> are on the dyno.</summary>
    public readonly List<VehiclePawn> Line = new();
    public int Turns;

    public YardState(Zone_PhobosServiceYard yard) => Yard = yard;

    public int Overflow => ServiceYardRule.Overflow(Cells, Allowance);
    public int OnDyno => System.Math.Min(Turns, Line.Count);
    public int Waiting => System.Math.Max(0, Line.Count - Turns);
}

/// <summary>
/// The service yards of one map (Framework 0.210.0; docs/development/vehicle-service-yard.md): every few seconds it looks at each yard's
/// equipment, cells and vehicles, lines up the vehicles waiting for its dyno bays (each bay one at a time, two with a second roller set,
/// "with no moving": the vehicle whose turn it is counts as at the dyno where it stands), gives each pump the next vehicle to fill, and
/// notes how long a vehicle has stood idle, for making room. Only when the player marks a yard, a Move up, and when each vehicle first asked
/// or fell idle are saved; everything else is worked out again.
/// </summary>
public sealed class MapComponent_PhobosServiceYards : MapComponent
{
    static readonly ConditionalWeakTable<Map, MapComponent_PhobosServiceYards> ByMap = new();

    /// <summary>When each vehicle (by ID) joined a dyno line, for the order of asking.</summary>
    Dictionary<int, int> asked = new();
    /// <summary>The player's Move up for a vehicle (by ID), the tick it was given; it lasts while the vehicle stays in the line.</summary>
    Dictionary<int, int> movedUp = new();
    /// <summary>When each vehicle (by ID) in a yard was first seen with nothing left to do there.</summary>
    Dictionary<int, int> idleSince = new();

    readonly Dictionary<Zone_PhobosServiceYard, YardState> states = new();
    readonly Dictionary<Thing, YardState> ownerOf = new();
    readonly Dictionary<VehiclePawn, YardState> lineOf = new();
    readonly HashSet<VehiclePawn> withTurn = new();
    bool dirty = true;
    int nextLook;

    static PhobosVehicleRulesDef Rules => PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();

    public MapComponent_PhobosServiceYards(Map map)
        : base(map)
    {
        ByMap.Remove(map);
        ByMap.Add(map, this);
    }

    public static MapComponent_PhobosServiceYards? Of(Map? map) =>
        map == null ? null : ByMap.TryGetValue(map, out MapComponent_PhobosServiceYards? found) ? found : map.GetComponent<MapComponent_PhobosServiceYards>();

    public IEnumerable<YardState> States => states.Values;
    public bool Any => states.Count > 0;

    public YardState? StateOf(Zone_PhobosServiceYard yard) => states.TryGetValue(yard, out YardState? state) ? state : null;
    public YardState? OwnerOf(Thing equipment) => ownerOf.TryGetValue(equipment, out YardState? state) ? state : null;
    public YardState? LineOf(VehiclePawn vehicle) => lineOf.TryGetValue(vehicle, out YardState? state) ? state : null;
    public bool HasTurn(VehiclePawn vehicle) => withTurn.Contains(vehicle);
    public int IdleSince(VehiclePawn vehicle) => idleSince.TryGetValue(vehicle.thingIDNumber, out int tick) ? tick : int.MaxValue;
    public bool MovedUp(VehiclePawn vehicle) => movedUp.ContainsKey(vehicle.thingIDNumber);

    public void SetDirty() => dirty = true;

    internal void MoveUp(VehiclePawn vehicle)
    {
        movedUp[vehicle.thingIDNumber] = Find.TickManager.TicksGame;
        dirty = true;
    }

    public void EnsureFresh()
    {
        if (dirty) Look();
    }

    public override void MapComponentTick()
    {
        int now = Find.TickManager.TicksGame;
        if (now < nextLook && !dirty) return;
        nextLook = now + System.Math.Max(60, Rules.serviceYardLookTicks);
        Look();
    }

    /// <summary>Works out every yard afresh: its equipment and allowance, its vehicles, its dyno line and its pumps' next vehicles.</summary>
    void Look()
    {
        dirty = false;
        int now = Find.TickManager.TicksGame;
        foreach (YardState old in states.Values) old.Yard.State = null;
        states.Clear();
        ownerOf.Clear();
        lineOf.Clear();
        withTurn.Clear();
        foreach (Zone zone in map.zoneManager.AllZones)
        {
            if (zone is Zone_PhobosServiceYard yard) states[yard] = yard.State = new YardState(yard) { Cells = yard.Cells.Count };
        }
        if (states.Count == 0)
        {
            ClearPumps();
            asked.Clear();
            movedUp.Clear();
            idleSince.Clear();
            return;
        }
        FindEquipment();
        foreach (YardState state in states.Values)
        {
            state.Allowance = ServiceYardRule.Allowance(state.Equipment.Select(e => (e.def.GetModExtension<PhobosYardEquipmentExtension>()!.cells,
                e.def.GetModExtension<PhobosYardEquipmentExtension>()!.required)));
            state.HasDyno = state.Equipment.Any(e => e.def.GetModExtension<PhobosYardEquipmentExtension>()!.required);
            state.Works = ServiceYardRule.Works(state.Cells, state.Allowance, state.HasDyno);
            if (state.Works && Seen.Wants("vehicles.service-yard.works", null)) Seen.Note("vehicles.service-yard.works", null, state.Cells + " of " + state.Allowance + " cells");
        }
        List<VehiclePawn> vehicles = PlayersVehicles();
        foreach (VehiclePawn vehicle in vehicles)
        {
            if (map.zoneManager.ZoneAt(vehicle.Position) is Zone_PhobosServiceYard yard && states.TryGetValue(yard, out YardState? state)) state.Vehicles.Add(vehicle);
        }
        var inAnyLine = new HashSet<int>();
        foreach (YardState state in states.Values)
        {
            if (!state.Works || state.Dynos.Count == 0) continue;
            BuildLine(state, vehicles, now, inAnyLine);
        }
        asked.RemoveAll(pair => !inAnyLine.Contains(pair.Key));
        movedUp.RemoveAll(pair => !inAnyLine.Contains(pair.Key));
        NoteIdle(now);
        AssignPumps();
    }

    /// <summary>Each piece of equipment of the player's that stands in a yard or touches its edge, given to one yard only.</summary>
    void FindEquipment()
    {
        var touching = new HashSet<Zone_PhobosServiceYard>();
        foreach (ThingDef def in ServiceYards.EquipmentDefs)
        {
            List<Thing> things = map.listerThings.ThingsOfDef(def);
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (!thing.Spawned || thing.Faction != Faction.OfPlayer) continue;
                touching.Clear();
                foreach (IntVec3 c in thing.OccupiedRect().ExpandedBy(1))
                {
                    if (c.InBounds(map) && map.zoneManager.ZoneAt(c) is Zone_PhobosServiceYard yard) touching.Add(yard);
                }
                if (touching.Count == 0) continue;
                int owner = ServiceYardRule.Owner(touching.Select(y => y.ID));
                YardState state = states[touching.First(y => y.ID == owner)];
                state.Equipment.Add(thing);
                ownerOf[thing] = state;
                if (thing.TryGetComp<CompPhobosTuningRig>() is { } rig) state.Dynos.Add(rig);
                if (thing.TryGetComp<CompPhobosFuelPump>() is { } pump) state.Pumps.Add(pump);
            }
        }
    }

    List<VehiclePawn> PlayersVehicles()
    {
        var found = new List<VehiclePawn>();
        foreach (Pawn pawn in map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer))
        {
            if (pawn is VehiclePawn vehicle) found.Add(vehicle);
        }
        return found;
    }

    /// <summary>
    /// The yard's dyno line: its own vehicles that wait for dyno work, and any of the player's standing in reach of one of its dyno bays
    /// outside it ("a vehicle standing in a dyno bay's own reach outside the yard takes turns in the same line"). The first in the order of
    /// asking, the player's Move up first, take the bays' turns.
    /// </summary>
    void BuildLine(YardState state, List<VehiclePawn> vehicles, int now, HashSet<int> inAnyLine)
    {
        var waiting = new List<VehiclePawn>();
        foreach (VehiclePawn vehicle in vehicles)
        {
            if (lineOf.ContainsKey(vehicle)) continue;
            bool inYard = state.Vehicles.Contains(vehicle);
            if (!inYard)
            {
                if (map.zoneManager.ZoneAt(vehicle.Position) is Zone_PhobosServiceYard) continue;
                CellRect area = VehicleFittings.DrawnArea(vehicle);
                if (!state.Dynos.Any(rig => rig.Reaches(area))) continue;
            }
            if (vehicle.GetComp<CompPhobosVehicleSystems>() is not { Usable: true } systems || !ServiceYards.WantsDyno(systems)) continue;
            waiting.Add(vehicle);
        }
        foreach (VehiclePawn vehicle in waiting)
        {
            if (!asked.ContainsKey(vehicle.thingIDNumber)) asked[vehicle.thingIDNumber] = now;
            inAnyLine.Add(vehicle.thingIDNumber);
            lineOf[vehicle] = state;
        }
        state.Line.AddRange(ServiceYardRule.Line(waiting, v => asked[v.thingIDNumber],
            v => movedUp.TryGetValue(v.thingIDNumber, out int tick) ? tick : -1, v => v.thingIDNumber));
        state.Turns = ServiceYardRule.Turns(state.Dynos.Where(rig => rig.Working).Select(ServiceYards.TurnsOf));
        for (int i = 0; i < state.Line.Count && i < state.Turns; i++)
        {
            withTurn.Add(state.Line[i]);
            string? rollers = state.Turns > state.Dynos.Count ? "second rollers" : null;
            if (Seen.Wants("vehicles.service-yard.turn", state.Line[i], rollers)) Seen.Note("vehicles.service-yard.turn", state.Line[i], null, rollers);
        }
    }

    /// <summary>When each vehicle in a working yard first had nothing left to do there; forgotten once it has work again or leaves.</summary>
    void NoteIdle(int now)
    {
        var seen = new HashSet<int>();
        foreach (YardState state in states.Values)
        {
            if (!state.Works) continue;
            foreach (VehiclePawn vehicle in state.Vehicles)
            {
                if (!ServiceYards.Idle(vehicle)) continue;
                seen.Add(vehicle.thingIDNumber);
                if (!idleSince.ContainsKey(vehicle.thingIDNumber)) idleSince[vehicle.thingIDNumber] = now;
            }
        }
        idleSince.RemoveAll(pair => !seen.Contains(pair.Key));
    }

    /// <summary>
    /// Gives each working pump of a working yard the next vehicle to fill, the emptiest first, and, with its charging cable, the next to
    /// charge; two pumps never take the same vehicle. A vehicle of a fuel no pump holds is named on the first pump, so the player hears why.
    /// </summary>
    void AssignPumps()
    {
        ClearPumps();
        foreach (YardState state in states.Values)
        {
            if (!state.Works) continue;
            var filling = new HashSet<VehiclePawn>();
            var charging = new HashSet<VehiclePawn>();
            foreach (CompPhobosFuelPump pump in state.Pumps.OrderBy(p => p.parent.thingIDNumber))
            {
                if (!pump.Working) continue;
                ThingDef? fuel = pump.Fuel;
                pump.FuelTarget = ServiceYardRule.NextToFill(state.Vehicles.Where(v => !filling.Contains(v) && Burns(v, fuel)),
                    v => v.CompFueledTravel!.FuelPercent, v => v.thingIDNumber);
                if (pump.FuelTarget != null) filling.Add(pump.FuelTarget);
                pump.WrongFuel ??= state.Vehicles.FirstOrDefault(v => v.CompFueledTravel is { Props.electricPowered: false } travel
                    && travel.FuelPercent < 0.995f && travel.Props.fuelType != fuel && !state.Pumps.Any(other => other.Fuel == travel.Props.fuelType));
                if (!pump.Charges) continue;
                pump.ChargeTarget = ServiceYardRule.NextToFill(state.Vehicles.Where(v => !charging.Contains(v) && v.CompFueledTravel is { Props.electricPowered: true }),
                    v => v.CompFueledTravel!.FuelPercent, v => v.thingIDNumber);
                if (pump.ChargeTarget != null) charging.Add(pump.ChargeTarget);
            }
        }
    }

    static bool Burns(VehiclePawn vehicle, ThingDef? fuel) =>
        fuel != null && vehicle.CompFueledTravel is { Props.electricPowered: false } travel && travel.Props.fuelType == fuel;

    void ClearPumps()
    {
        foreach (CompPhobosFuelPump pump in CompPhobosFuelPump.On(map))
        {
            pump.FuelTarget = null;
            pump.ChargeTarget = null;
            pump.WrongFuel = null;
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref asked, "PhobosServiceYardAsked", LookMode.Value, LookMode.Value);
        Scribe_Collections.Look(ref movedUp, "PhobosServiceYardMovedUp", LookMode.Value, LookMode.Value);
        Scribe_Collections.Look(ref idleSince, "PhobosServiceYardIdleSince", LookMode.Value, LookMode.Value);
        if (Scribe.mode != LoadSaveMode.PostLoadInit) return;
        asked ??= new Dictionary<int, int>();
        movedUp ??= new Dictionary<int, int>();
        idleSince ??= new Dictionary<int, int>();
        dirty = true;
    }
}

/// <summary>
/// The service behind service yards (Framework 0.210.0; docs/development/vehicle-service-yard.md): which buildings are yard equipment,
/// how big a yard may be drawn, whether a vehicle stands in a working yard or has its turn on the dyno, what it waits for, and the player's
/// orders from the Fleet board. The yard, its tool and the Fleet board read this; the state lives in <see cref="MapComponent_PhobosServiceYards"/>.
/// </summary>
public static class ServiceYards
{
    /// <summary>The ability a fuel pump's charging cable gives (its upgrade's "grants").</summary>
    public const string ChargesVehicles = "PhobosChargesVehicles";
    /// <summary>The figure a dyno bay's second roller set sets: how many vehicles it takes at once in its yard's line.</summary>
    public const string DynoTurns = "dynoTurns";
    /// <summary>The figure a depot bay's faster pump sets: fuel moved in an hour.</summary>
    public const string PumpPerHour = "pumpPerHour";

    static List<ThingDef>? equipmentDefs;

    /// <summary>Every building marked as yard equipment, found once.</summary>
    public static List<ThingDef> EquipmentDefs => equipmentDefs ??= DefDatabase<ThingDef>.AllDefsListForReading
        .Where(d => d.GetModExtension<PhobosYardEquipmentExtension>() != null).ToList();

    public static MapComponent_PhobosServiceYards? For(Map? map) => MapComponent_PhobosServiceYards.Of(map);

    /// <summary>Something the yard depends on changed (its cells, its equipment): it looks again at the next tick.</summary>
    public static void Dirty(Map? map) => For(map)?.SetDirty();

    /// <summary>How many vehicles a dyno bay takes at once in its yard's line: one, or what an upgrade sets.</summary>
    public static int TurnsOf(CompPhobosTuningRig rig) =>
        UnityEngine.Mathf.Max(1, UnityEngine.Mathf.RoundToInt(rig.parent.TryGetComp<CompPhobosBuildingUpgrades>()?.Figure(DynoTurns, 1f) ?? 1f));

    /// <summary>The yard the vehicle stands in, working or not, or null.</summary>
    public static Zone_PhobosServiceYard? YardOf(VehiclePawn vehicle) =>
        vehicle.Spawned ? vehicle.Map.zoneManager.ZoneAt(vehicle.Position) as Zone_PhobosServiceYard : null;

    /// <summary>Whether the vehicle stands in a working yard of the player's: its jobs take the yard's share of the usual work.</summary>
    public static bool InWorkingYard(VehiclePawn vehicle) => vehicle.Faction == Faction.OfPlayer && YardOf(vehicle) is { Works: true };

    /// <summary>Whether this dyno bay's turns are run by a working yard's line, so standing in its reach is not enough by itself.</summary>
    public static bool RunsLine(CompPhobosTuningRig rig) => For(rig.parent.Map)?.OwnerOf(rig.parent) is { Works: true };

    /// <summary>Whether the vehicle has a turn on a yard's dyno now: it counts as at the dyno where it stands.</summary>
    public static bool HasTurn(VehiclePawn vehicle) => vehicle.Spawned && For(vehicle.Map) is { } yards && yards.HasTurn(vehicle);

    /// <summary>
    /// Whether the vehicle waits for work a dyno bay makes possible or better: a repair or service that needs the rig (a tune, a change of
    /// grade, a heavy overhaul), a part to replace (fitted at better health at the rig), or a diagnosis asked for.
    /// </summary>
    public static bool WantsDyno(CompPhobosVehicleSystems systems)
    {
        if (systems.Diagnostics.Requested) return true;
        foreach (RepairWork repair in systems.Repairs.Pending)
        {
            if (repair.NeedsRig || repair.Kind == RepairKind.ReplacePart) return true;
        }
        foreach (SystemState state in systems.DueServices())
        {
            if (state.Def.service is { needsRig: true }) return true;
        }
        return false;
    }

    /// <summary>Whether a vehicle in a yard has nothing left to do there: no work that would send it to a bay, nothing for the dyno, a full tank.</summary>
    public static bool Idle(VehiclePawn vehicle)
    {
        if (vehicle.GetComp<CompPhobosVehicleSystems>() is not { Usable: true } systems) return true;
        if (WantsDyno(systems) || BayTrips.NeedOf(systems).Any || BayTrips.FreeRepairWaits(systems)) return false;
        if (vehicle.GetComp<CompPhobosBayTrip>() is { Stage: not BayTripStage.None }) return false;
        return vehicle.CompFueledTravel is not { } fuel || fuel.FuelPercent >= 0.995f;
    }

    /// <summary>What a vehicle is doing in a yard, in a few words, or null when it stands in none.</summary>
    public static string? DoingLine(VehiclePawn vehicle)
    {
        if (!vehicle.Spawned || For(vehicle.Map) is not { } yards) return null;
        if (yards.LineOf(vehicle) is { } line)
        {
            if (yards.HasTurn(vehicle)) return "PhobosServiceYard_DoingOnDyno".Translate().Resolve();
            int place = line.Line.IndexOf(vehicle) - line.Turns + 1;
            return "PhobosServiceYard_DoingWaitingDyno".Translate(place.Named("PLACE")).Resolve();
        }
        if (YardOf(vehicle) is not { Works: true } yard) return null;
        foreach (CompPhobosFuelPump pump in yard.State!.Pumps)
        {
            if (pump.FuelTarget == vehicle) return "PhobosServiceYard_DoingFuelled".Translate().Resolve();
            if (pump.ChargeTarget == vehicle) return "PhobosServiceYard_DoingCharged".Translate().Resolve();
        }
        return "PhobosServiceYard_DoingParked".Translate().Resolve();
    }

    // ---- Drawing a yard ----

    /// <summary>
    /// The new cells a drag may add to a yard (or make a new yard of) within the allowance of the equipment touching it, nearest the yard and
    /// its equipment first; the rest are refused, with the reason in the player's words. Equipment another yard already counts is not counted
    /// again.
    /// </summary>
    public static List<IntVec3> Trim(Map map, Zone_PhobosServiceYard? target, List<IntVec3> fresh, out string? refusal)
    {
        refusal = null;
        var union = new HashSet<IntVec3>(fresh);
        if (target != null) union.UnionWith(target.Cells);
        var equipment = new List<Thing>();
        foreach (ThingDef def in EquipmentDefs)
        {
            foreach (Thing thing in map.listerThings.ThingsOfDef(def))
            {
                if (thing.Faction != Faction.OfPlayer || !thing.Spawned) continue;
                bool touches = false, takenElsewhere = false;
                foreach (IntVec3 c in thing.OccupiedRect().ExpandedBy(1))
                {
                    if (!c.InBounds(map)) continue;
                    if (union.Contains(c)) touches = true;
                    if (map.zoneManager.ZoneAt(c) is Zone_PhobosServiceYard other && other != target && (target == null || other.ID < target.ID)) takenElsewhere = true;
                }
                if (touches && !takenElsewhere) equipment.Add(thing);
            }
        }
        int allowance = ServiceYardRule.Allowance(equipment.Select(e => (e.def.GetModExtension<PhobosYardEquipmentExtension>()!.cells,
            e.def.GetModExtension<PhobosYardEquipmentExtension>()!.required)));
        int existing = target?.Cells.Count ?? 0;
        int room = ServiceYardRule.Room(existing, allowance);
        if (!equipment.Any(e => e.def.GetModExtension<PhobosYardEquipmentExtension>()!.required))
        {
            refusal = "PhobosServiceYard_RefuseNoDyno".Translate().Resolve();
            return new List<IntVec3>();
        }
        if (fresh.Count <= room) return fresh;
        var anchors = new List<IntVec3>(target?.Cells ?? new List<IntVec3>());
        foreach (Thing thing in equipment) anchors.AddRange(thing.OccupiedRect().Cells);
        List<IntVec3> kept = fresh.OrderBy(c => anchors.Min(a => a.DistanceToSquared(c))).ThenBy(c => c.z).ThenBy(c => c.x).Take(room).ToList();
        refusal = "PhobosServiceYard_RefuseCapped".Translate((existing + kept.Count).Named("CELLS"), allowance.Named("ALLOWANCE"),
            (fresh.Count - kept.Count).Named("LEFT")).Resolve();
        return kept;
    }

    // ---- Bay trips ----

    /// <summary>
    /// A place for a vehicle wholly inside a working service yard with a working dyno bay, the nearest yard first (Framework 0.210.0): clear
    /// of other vehicles and the places other trips keep, on ground it can drive, with a way there unless it is carried. The yard's first
    /// working dyno bay is the trip's bay. When every working yard is full, the vehicle idle longest in one is sent out to make room, and this
    /// vehicle tries again at its next look.
    /// </summary>
    internal static bool TryFindYardPlace(VehiclePawn vehicle, List<CellRect> kept, ICollection<IntVec3>? avoid, Rot4? onlyFacing, int maxWayTests,
        ref int wayTests, out Thing? bay, out IntVec3 cell, out Rot4 facing)
    {
        bay = null;
        cell = IntVec3.Invalid;
        facing = Rot4.Invalid;
        if (vehicle.Faction != Faction.OfPlayer || For(vehicle.Map) is not { } yards) return false;
        yards.EnsureFresh();
        List<YardState> open = yards.States.Where(s => s.Works && s.Dynos.Any(d => d.Working)).ToList();
        if (open.Count == 0) return false;
        Map map = vehicle.Map;
        foreach (YardState state in open.OrderBy(s => s.Yard.Cells.Count == 0 ? int.MaxValue : s.Yard.Cells[0].DistanceToSquared(vehicle.Position)))
        {
            if (!VehicleParks.TryFindPlaceIn(vehicle, new Zone[] { state.Yard },
                    cells => BayTrips.ClearOfVehicles(vehicle, cells, map, kept),
                    body => BayTrips.BodyFits(vehicle, body, map, kept, null) && (avoid == null || !body.Cells.Any(avoid.Contains)),
                    c => vehicle.CanReachVehicle(c, Verse.AI.PathEndMode.OnCell, Danger.Deadly, TraverseMode.ByPawn),
                    maxWayTests, ref wayTests, onlyFacing, out cell, out facing))
            {
                continue;
            }
            bay = state.Dynos.First(d => d.Working).parent;
            if (Seen.Wants("vehicles.service-yard.trip", vehicle)) Seen.Note("vehicles.service-yard.trip", vehicle, state.Yard.label);
            return true;
        }
        MakeRoom(yards, open, vehicle);
        return false;
    }

    /// <summary>
    /// Sends out of a full yard the vehicle that has stood there longest with nothing left to do, once it has been idle long enough ([A]:
    /// an hour), so it is not taken out the moment its work ends. Nothing is moved out for any other reason.
    /// </summary>
    static void MakeRoom(MapComponent_PhobosServiceYards yards, List<YardState> open, VehiclePawn forVehicle)
    {
        int now = Find.TickManager.TicksGame;
        int wait = PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>().serviceYardIdleBeforeMoveTicks;
        var idle = new List<VehiclePawn>();
        foreach (YardState state in open)
        {
            foreach (VehiclePawn parked in state.Vehicles)
            {
                if (parked == forVehicle || now - yards.IdleSince(parked) < wait || !Idle(parked)) continue;
                if (parked.GetComp<CompPhobosBayTrip>() is not { Stage: BayTripStage.None, KeptOut: false }) continue;
                idle.Add(parked);
            }
        }
        while (idle.Count > 0)
        {
            VehiclePawn? pick = ServiceYardRule.ToMakeRoom(idle, yards.IdleSince, v => v.thingIDNumber);
            if (pick == null) return;
            if (pick.GetComp<CompPhobosBayTrip>()!.LeaveYardForRoom()) return;
            idle.Remove(pick);
        }
    }

    // ---- The player's orders (the Fleet board) ----

    /// <summary>Puts a vehicle first in its yard's dyno line: the player's word overrides the order of asking.</summary>
    public static void MoveUp(VehiclePawn vehicle)
    {
        if (For(vehicle.Map) is not { } yards || yards.LineOf(vehicle) == null)
        {
            Messages.Message("PhobosServiceYard_NotInLine".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve(), vehicle, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        yards.MoveUp(vehicle);
        Messages.Message("PhobosServiceYard_MovedUp".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve(), vehicle, MessageTypeDefOf.SilentInput, historical: false);
        Seen.Note("vehicles.service-yard.move-up", vehicle);
    }

    /// <summary>The working yards on the vehicle's map a Send to yard may choose from.</summary>
    public static List<Zone_PhobosServiceYard> WorkingYards(Map map)
    {
        For(map)?.EnsureFresh();
        return map.zoneManager.AllZones.OfType<Zone_PhobosServiceYard>().Where(y => y.Works).ToList();
    }

    /// <summary>The player's order: drive this vehicle into the yard and park it there, by bay trip. Says what happened or why not.</summary>
    public static void SendTo(VehiclePawn vehicle, Zone_PhobosServiceYard yard)
    {
        string? refused = vehicle.GetComp<CompPhobosBayTrip>() is { } trip
            ? trip.SendToYard(yard)
            : "PhobosServiceYard_CannotSendBusy".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve();
        Messages.Message(refused ?? "PhobosServiceYard_Sent".Translate(vehicle.LabelShort.Named("VEHICLE"), yard.label.Named("YARD")).Resolve(), vehicle,
            refused != null ? MessageTypeDefOf.RejectInput : MessageTypeDefOf.SilentInput, historical: false);
    }

    /// <summary>The player's order: drive this vehicle out of its yard now, to its depot bay, the vehicle park or nearby.</summary>
    public static void TakeOut(VehiclePawn vehicle)
    {
        string? refused = vehicle.GetComp<CompPhobosBayTrip>() is { } trip
            ? trip.TakeOutOfYard(byPlayer: true)
            : "PhobosServiceYard_CannotLeaveBusy".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve();
        Messages.Message(refused ?? "PhobosServiceYard_TakenOut".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve(), vehicle,
            refused != null ? MessageTypeDefOf.RejectInput : MessageTypeDefOf.SilentInput, historical: false);
    }

    /// <summary>Goes to the yard on the map and selects it, closing the Fleet board.</summary>
    public static void JumpTo(Zone_PhobosServiceYard yard)
    {
        if (yard.Cells.Count == 0) return;
        CameraJumper.TryJump(yard.Cells[yard.Cells.Count / 2], yard.Map);
        Find.Selector.ClearSelection();
        Find.Selector.Select(yard);
        Find.MainTabsRoot.EscapeCurrentTab();
    }
}
