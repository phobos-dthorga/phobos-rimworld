using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Interface;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The camp order (Framework 0.212.0; docs/development/vehicle-accessories.md, Y3, Y8, Y9 and B1): a parked land vehicle's camp, set out beside it
/// by a colonist and packed away again. The awning (a fitting on its side rail) roofs two rows along one side; the roof tent (a fitting on its roof
/// mount) is pitched at the rear of that side as a two-person bed under its own canvas; swags and bedrolls carried in its cargo are laid out in
/// the cells left. The layout is shown on the button before anything is done, and a piece moved by hand is put back where it was moved next time.
///
/// Packing never deletes anything: a bed in use waits for its sleeper, and a piece the player has moved away from the camp is left as theirs.
/// The vehicle will not drive off with its camp out. If it is lost, the camp stays as ordinary things. If it leaves the map with its camp out
/// (in a caravan), what can be packed goes with it and what cannot is left standing. A vehicle of another faction at its own base may be found
/// with its camp out, pitched once when the map is made (owner, 13 Nov 2026: other factions' camps with the camp batch).
/// </summary>
public sealed class CompPhobosCamp : VehicleComp, IThingHolder
{
    internal const string RoofTentAbility = "RoofTent";
    internal const string AwningAbility = "Awning";
    const string TentSlot = "Tent";
    const string Icons = "Phobos/VehicleSystems/Camp/";

    /// <summary>The roof tent while it is packed. Saved; a save holds it once.</summary>
    ThingOwner<Thing>? held;
    /// <summary>The tent and gear standing beside it now, and the place in the layout each came from, in step.</summary>
    List<Thing> placed = new();
    List<string> placedSlots = new();
    /// <summary>The awning's poles standing now.</summary>
    List<Thing> poles = new();
    /// <summary>The side the player wants the camp (1 left, −1 right), and the side it stands now.</summary>
    int preferredSide = CampLayoutRule.Left;
    int pitchedSide = CampLayoutRule.Left;
    /// <summary>The work waiting for a colonist. Saved.</summary>
    CampOrder order;
    /// <summary>Where each piece stood when last packed, in the camp's frame: a piece moved by hand goes back there (the record's Y3).</summary>
    List<string> spotSlots = new();
    List<IntVec3> spotCells = new();
    List<int> spotDirections = new();
    /// <summary>A camp to pitch at the next tick: another faction's vehicle at its own base. Never pitched during spawning (the sling's fault).</summary>
    bool campAtBase;

    static PhobosVehicleRulesDef Rules => PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();

    ThingOwner<Thing> Held => held ??= new ThingOwner<Thing>(this, oneStackOnly: false);

    CompPhobosVehicleSystems? Systems => Vehicle.GetComp<CompPhobosVehicleSystems>();

    public override bool TickByRequest => true;

    public bool TentFitted => Systems is { Usable: true } systems && systems.Fittings.Grants(RoofTentAbility);
    public bool AwningFitted => Systems is { Usable: true } systems && systems.Fittings.Grants(AwningAbility);

    /// <summary>Whether its camp stands beside it now.</summary>
    public bool IsPitched => placed.Count > 0 || poles.Count > 0;

    public CampOrder Order => order;

    // ---- IThingHolder: the roof tent while packed ----

    public ThingOwner GetDirectlyHeldThings() => Held;

    public void GetChildHolders(List<IThingHolder> outChildren) => ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, Held);

    IThingHolder IThingHolder.ParentHolder => Vehicle;

    // ---- What it carries ----

    /// <summary>The camp gear in its cargo: packed buildings marked as camp gear (a swag, a bedroll).</summary>
    List<MinifiedThing> CargoGear()
    {
        var gear = new List<MinifiedThing>();
        ThingOwner? cargo = Vehicle.inventory?.innerContainer;
        if (cargo == null) return gear;
        foreach (Thing thing in cargo)
        {
            if (thing is MinifiedThing { InnerThing: { } inner } minified && inner.def.HasModExtension<PhobosCampGearExtension>()) gear.Add(minified);
        }
        return gear;
    }

    /// <summary>Whether there is anything to set out: a roof tent or awning fitted, or camp gear in the cargo. Read by the buttons every frame, so
    /// looked at most once a second.</summary>
    public bool HasCampGear
    {
        get
        {
            int now = Find.TickManager.TicksGame;
            if (now - gearTick >= PlanTicks || now < gearTick)
            {
                gearTick = now;
                hasGear = TentFitted || AwningFitted || CargoGear().Count > 0;
            }
            return hasGear;
        }
    }

    bool hasGear;
    int gearTick = -PlanTicks;

    // ---- The layout ----

    /// <summary>Everything one side would hold. Pieces are found a place on the map, so the plan is the camp as it would stand.</summary>
    internal sealed class Plan
    {
        public int side;
        public CampLayoutRule.Frame frame = null!;
        public readonly List<IntVec3> awning = new();
        public readonly List<IntVec3> awningRows = new();
        public readonly List<IntVec3> poles = new();
        public (IntVec3 centre, Rot4 rot, CellRect rect)? tent;
        public readonly List<(MinifiedThing gear, string slot, IntVec3 centre, Rot4 rot, CellRect rect)> gear = new();
        public int gearLeft;
        public string? refusal;

        public IEnumerable<IntVec3> PieceCells() =>
            (tent.HasValue ? tent.Value.rect.Cells : Enumerable.Empty<IntVec3>()).Concat(gear.SelectMany(g => g.rect.Cells)).Concat(poles);
    }

    CampLayoutRule.Frame FrameFor(int side)
    {
        CellRect rect = Vehicle.OccupiedRect();
        return new CampLayoutRule.Frame(rect.minX, rect.minZ, rect.maxX, rect.maxZ, Vehicle.Rotation.AsInt, side);
    }

    IntVec3 ToMap(CampLayoutRule.Frame frame, int x, int z)
    {
        (int mx, int mz) = frame.ToMap(x, z);
        return new IntVec3(mx, 0, mz);
    }

    CellRect ToMap(CampLayoutRule.Frame frame, int x0, int z0, int x1, int z1) => CellRect.FromLimits(ToMap(frame, x0, z0), ToMap(frame, x1, z1));

    /// <summary>The cell the game places a building from, for the footprint it should cover; invalid if no cell gives it.</summary>
    static IntVec3 CentreFor(CellRect rect, Rot4 rot, IntVec2 size) =>
        rect.Cells.FirstOrDefault(c => GenAdj.OccupiedRect(c, rot, size).Equals(rect), IntVec3.Invalid);

    /// <summary>Why a piece cannot stand on this cell, or null. People and grass are no bar: a bed goes down where someone stands.</summary>
    string? CellRefusal(IntVec3 cell, Map map, HashSet<IntVec3> used)
    {
        if (!cell.InBounds(map)) return "PhobosSetOut_BlockedEdge".Translate().Resolve();
        if (used.Contains(cell)) return "PhobosSetOut_BlockedGround".Translate().Resolve();
        if (!cell.Standable(map) && cell.GetEdifice(map) == null) return "PhobosSetOut_BlockedGround".Translate().Resolve();
        foreach (Thing thing in cell.GetThingList(map))
        {
            if (thing is Building or Blueprint or Frame) return "PhobosSetOut_BlockedBuilding".Translate(thing.LabelShort.Named("THING")).Resolve();
            if (thing.def.category == ThingCategory.Item) return "PhobosSetOut_BlockedItem".Translate(thing.LabelShort.Named("THING")).Resolve();
            if (thing is Plant { def.plant.IsTree: true }) return "PhobosSetOut_BlockedTree".Translate().Resolve();
            if (thing is VehiclePawn other && other != Vehicle) return "PhobosSetOut_BlockedBuilding".Translate(other.LabelShort.Named("THING")).Resolve();
        }
        return null;
    }

    /// <summary>Why the awning cannot go over this cell, or null: only rock, walls and other vehicles stop a roof of canvas.</summary>
    string? OverheadRefusal(IntVec3 cell, Map map)
    {
        if (!cell.InBounds(map)) return "PhobosSetOut_BlockedEdge".Translate().Resolve();
        if (cell.Impassable(map)) return "PhobosSetOut_BlockedGround".Translate().Resolve();
        foreach (Thing thing in cell.GetThingList(map))
        {
            if (thing is VehiclePawn other && other != Vehicle) return "PhobosSetOut_BlockedBuilding".Translate(other.LabelShort.Named("THING")).Resolve();
        }
        return null;
    }

    bool RectFree(CellRect rect, Map map, HashSet<IntVec3> used, out string? refusal)
    {
        foreach (IntVec3 cell in rect.Cells)
        {
            refusal = CellRefusal(cell, map, used);
            if (refusal != null) return false;
        }
        refusal = null;
        return true;
    }

    Plan PlanFor(int side, Map map, bool tent, bool awning, List<MinifiedThing> gear)
    {
        var plan = new Plan { side = side, frame = FrameFor(side) };
        int length = plan.frame.length;
        var used = new HashSet<IntVec3>();
        bool tentHere = tent && CampLayoutRule.TentFits(length);
        if (tentHere)
        {
            CellRect rect = ToMap(plan.frame, 0, 0, CampLayoutRule.TentOut - 1, CampLayoutRule.TentAlong - 1);
            Rot4 rot = new(plan.frame.ToMapRotation(CampLayoutRule.In));
            IntVec3 centre = CentreFor(rect, rot, VehicleCampDefOf.PhobosCamp_RoofTent.Size);
            string? refusal = null;
            if (!centre.IsValid) refusal = "PhobosSetOut_BlockedGround".Translate().Resolve();
            else RectFree(rect, map, used, out refusal);
            if (refusal != null)
            {
                plan.refusal = "PhobosCamp_TentNoRoom".Translate(Vehicle.LabelShort.Named("VEHICLE"), refusal.Named("REASON")).Resolve();
                return plan;
            }
            plan.tent = (centre, rot, rect);
            used.UnionWith(rect.Cells);
        }
        if (awning)
        {
            for (int z = 0; z < length; z++)
            {
                // The canvas is not drawn over the tent, whose own roof is there.
                if (!tentHere || z >= CampLayoutRule.TentAlong) plan.awningRows.Add(ToMap(plan.frame, 0, z));
                for (int x = 0; x < CampLayoutRule.AwningDepth; x++)
                {
                    IntVec3 cell = ToMap(plan.frame, x, z);
                    if (OverheadRefusal(cell, map) is { } refusal)
                    {
                        plan.refusal = "PhobosCamp_AwningNoRoom".Translate(Vehicle.LabelShort.Named("VEHICLE"), refusal.Named("REASON")).Resolve();
                        return plan;
                    }
                    plan.awning.Add(cell);
                }
            }
            foreach (int z in CampLayoutRule.PoleRows(length, tentHere))
            {
                IntVec3 cell = ToMap(plan.frame, CampLayoutRule.AwningDepth - 1, z);
                if (CellRefusal(cell, map, used) is { } refusal)
                {
                    plan.refusal = "PhobosCamp_AwningNoRoom".Translate(Vehicle.LabelShort.Named("VEHICLE"), refusal.Named("REASON")).Resolve();
                    return plan;
                }
                plan.poles.Add(cell);
                used.Add(cell);
            }
        }
        var counts = new Dictionary<ThingDef, int>();
        foreach (MinifiedThing minified in gear)
        {
            ThingDef def = minified.InnerThing.def;
            counts.TryGetValue(def, out int index);
            counts[def] = index + 1;
            string slot = def.defName + "#" + index;
            if (PlaceGear(plan, map, used, def, slot) is { } spot) plan.gear.Add((minified, slot, spot.centre, spot.rot, spot.rect));
            else plan.gearLeft++;
        }
        return plan;
    }

    /// <summary>A place for one piece of gear: where it stood last time if that is free, else the first free slot, head towards the vehicle.</summary>
    (IntVec3 centre, Rot4 rot, CellRect rect)? PlaceGear(Plan plan, Map map, HashSet<IntVec3> used, ThingDef def, string slot)
    {
        int remembered = spotSlots.IndexOf(slot);
        if (remembered >= 0)
        {
            IntVec3 centre = ToMap(plan.frame, spotCells[remembered].x, spotCells[remembered].z);
            Rot4 rot = new(plan.frame.ToMapRotation(spotDirections[remembered]));
            CellRect rect = GenAdj.OccupiedRect(centre, rot, def.Size);
            if (RectFree(rect, map, used, out _))
            {
                used.UnionWith(rect.Cells);
                return (centre, rot, rect);
            }
        }
        Rot4 inward = new(plan.frame.ToMapRotation(CampLayoutRule.In));
        foreach ((int x, int z) in CampLayoutRule.Slots(plan.frame.length, def.Size.x, def.Size.z, Rules.campReachCells))
        {
            CellRect rect = ToMap(plan.frame, x, z, x + def.Size.z - 1, z + def.Size.x - 1);
            IntVec3 centre = CentreFor(rect, inward, def.Size);
            if (!centre.IsValid || !RectFree(rect, map, used, out _)) continue;
            used.UnionWith(rect.Cells);
            return (centre, inward, rect);
        }
        return null;
    }

    /// <summary>The camp as it would stand now: the side the player wants, else the other. Its refusal says why neither will do.</summary>
    Plan MakePlan()
    {
        Map map = Vehicle.Map;
        bool tent = TentFitted, awning = AwningFitted;
        List<MinifiedThing> gear = CargoGear();
        Plan first = PlanFor(preferredSide, map, tent, awning, gear);
        if (first.refusal == null) return first;
        Plan second = PlanFor(-preferredSide, map, tent, awning, gear);
        return second.refusal == null ? second : first;
    }

    // The button and its preview read the plan every frame they show, so it is worked out at most once a second (buttons read caches). Setting
    // up works it out again for itself before anything is placed.
    const int PlanTicks = 60;
    Plan? cachedPlan;
    int planTick = -PlanTicks;

    internal Plan? CurrentPlan
    {
        get
        {
            if (!Vehicle.Spawned || IsPitched) return null;
            int now = Find.TickManager.TicksGame;
            if (cachedPlan != null && now - planTick < PlanTicks && now >= planTick) return cachedPlan;
            planTick = now;
            return cachedPlan = MakePlan();
        }
    }

    void Forget() => cachedPlan = null;

    // ---- Refusals ----

    /// <summary>Why the camp cannot be set up now, or null.</summary>
    public string? SetUpRefusal()
    {
        NamedArgument name = Vehicle.LabelShort.Named("VEHICLE");
        if (!Vehicle.Spawned) return "PhobosCamp_NotHere".Translate(name).Resolve();
        if (IsPitched) return "PhobosCamp_AlreadyUp".Translate(name).Resolve();
        if (Vehicle.vehiclePather is { Moving: true }) return "PhobosCamp_Moving".Translate(name).Resolve();
        if (OnMapFlight.Airborne(Vehicle)) return "PhobosCamp_Moving".Translate(name).Resolve();
        if (Hitches.Towing(Vehicle) is { } towedBy) return "PhobosTrailer_SetUpWhileTowed".Translate(name, towedBy.Vehicle.LabelShort.Named("TRACTOR")).Resolve();
        if (!HasCampGear) return "PhobosCamp_NothingToSetOut".Translate(name).Resolve();
        return CurrentPlan?.refusal;
    }

    /// <summary>Why the camp cannot be packed now, or null: a piece in use waits for whoever is using it.</summary>
    public string? PackUpRefusal()
    {
        if (!IsPitched) return "PhobosCamp_NotUp".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
        foreach (Thing piece in placed)
        {
            if (!piece.Spawned) continue;
            if (piece is Building_Bed { CurOccupants: var sleepers } && sleepers.Any() || piece.Map.reservationManager.IsReservedByAnyoneOf(piece, Faction.OfPlayer))
            {
                return "PhobosCamp_PieceInUse".Translate(piece.LabelShort.Named("PIECE"), Vehicle.LabelShort.Named("VEHICLE")).Resolve();
            }
        }
        return null;
    }

    /// <summary>What it tells a driver who tries to take it away with its camp out, or null.</summary>
    public string? HoldRefusal => IsPitched ? "PhobosCamp_HoldsVehicle".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve() : null;

    public override AcceptanceReport CanMove(FloatMenuContext context) => HoldRefusal is { } held ? held : base.CanMove(context);

    // ---- Orders: a colonist does the work ----

    /// <summary>The work the waiting order takes, at construction speed: each piece's own (authored).</summary>
    public float Work
    {
        get
        {
            PhobosVehicleRulesDef rules = Rules;
            float work = 0f;
            if (order == CampOrder.SetUp)
            {
                if (TentFitted) work += rules.campTentWork;
                if (AwningFitted) work += rules.campAwningWork;
                work += CargoGear().Sum(g => GearWork(g.InnerThing.def));
            }
            else
            {
                if (placed.Any(p => p.def == VehicleCampDefOf.PhobosCamp_RoofTent)) work += rules.campTentWork;
                if (poles.Count > 0) work += rules.campAwningWork;
                work += placed.Where(p => p.def != VehicleCampDefOf.PhobosCamp_RoofTent).Sum(p => GearWork(p.def));
            }
            return Mathf.Max(work, 1f);
        }
    }

    static float GearWork(ThingDef def) => def.GetModExtension<PhobosCampGearExtension>()?.work ?? new PhobosCampGearExtension().work;

    /// <summary>Asks a colonist to set the camp up (<paramref name="on"/>) or pack it. Says what it did, or why not.</summary>
    public void OrderCamp(bool on)
    {
        string? refusal = on ? SetUpRefusal() : PackUpRefusal();
        if (refusal != null)
        {
            Messages.Message(refusal, Vehicle, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        order = on ? CampOrder.SetUp : CampOrder.PackUp;
        CampOrders.Note(this, true);
        Messages.Message((on ? "PhobosCamp_SetUpOrdered" : "PhobosCamp_PackUpOrdered").Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(),
            Vehicle, MessageTypeDefOf.SilentInput, historical: false);
    }

    public void CancelOrder()
    {
        if (order == CampOrder.None) return;
        order = CampOrder.None;
        CampOrders.Note(this, false);
        Messages.Message("PhobosCamp_OrderCancelled".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(), Vehicle, MessageTypeDefOf.SilentInput, historical: false);
    }

    /// <summary>Why a colonist cannot do the waiting order now, or null: read before the job starts and again when the work is done.</summary>
    public string? OrderRefusal() => order switch
    {
        CampOrder.SetUp => SetUpRefusal(),
        CampOrder.PackUp => PackUpRefusal(),
        _ => "PhobosCamp_NoOrder".Translate().Resolve(),
    };

    /// <summary>A colonist has finished the work: the camp goes up or comes down now, if it still may.</summary>
    public void WorkDone()
    {
        CampOrder done = order;
        order = CampOrder.None;
        CampOrders.Note(this, false);
        if (done == CampOrder.None) return;
        Forget();
        if ((done == CampOrder.SetUp ? SetUpRefusal() : PackUpRefusal()) is { } refusal)
        {
            Messages.Message(refusal, Vehicle, MessageTypeDefOf.RejectInput);
            return;
        }
        if (done == CampOrder.SetUp) Pitch(MakePlan(), fromCargo: true);
        else Strike(leaving: false);
    }

    // ---- Pitching and striking ----

    void Pitch(Plan plan, bool fromCargo)
    {
        if (plan.refusal != null || IsPitched) return;
        Map map = Vehicle.Map;
        Faction? faction = Vehicle.Faction;
        pitchedSide = plan.side;
        // The tent first: its own canvas roof over its bed.
        if (plan.tent is { } tent)
        {
            Thing? thing = Held.FirstOrDefault(t => t.def == VehicleCampDefOf.PhobosCamp_RoofTent);
            if (thing != null) Held.Remove(thing);
            else thing = ThingMaker.MakeThing(VehicleCampDefOf.PhobosCamp_RoofTent);
            if (thing.Faction != faction) thing.SetFaction(faction);
            Thing spawned = GenSpawn.Spawn(thing, tent.centre, map, tent.rot, WipeMode.VanishOrMoveAside);
            if (spawned is Building_PhobosRoofTent roofTent) CanvasRoofs.Lay(map, tent.rect.Cells, roofTent.roof);
            placed.Add(spawned);
            placedSlots.Add(TentSlot);
        }
        // The awning: its canvas roof, and poles that hold the list of it and draw it.
        if (plan.poles.Count > 0)
        {
            var roof = new List<IntVec3>();
            CanvasRoofs.Lay(map, plan.awning, roof);
            foreach (IntVec3 cell in plan.poles)
            {
                var pole = (Building_PhobosAwningPole)ThingMaker.MakeThing(VehicleCampDefOf.PhobosCamp_AwningPole);
                pole.SetFaction(faction);
                pole.roof = new List<IntVec3>(roof);
                pole.outRotation = plan.frame.outRotation;
                if (poles.Count == 0) pole.canvasRows = new List<IntVec3>(plan.awningRows);
                poles.Add(GenSpawn.Spawn(pole, cell, map, WipeMode.VanishOrMoveAside));
            }
        }
        // The gear: out of the cargo, or new for a camp pitched at another faction's base.
        foreach ((MinifiedThing minified, string slot, IntVec3 centre, Rot4 rot, _) in plan.gear)
        {
            Thing? inner = TakeFromCargo(minified);
            if (inner == null) continue;
            if (inner.Faction != faction && inner.def.CanHaveFaction) inner.SetFaction(faction);
            placed.Add(GenSpawn.Spawn(inner, centre, map, rot, WipeMode.VanishOrMoveAside));
            placedSlots.Add(slot);
        }
        Forget();
        if (!fromCargo) return;
        NamedArgument name = Vehicle.LabelShort.Named("VEHICLE");
        string message = "PhobosCamp_SetUp".Translate(name).Resolve();
        if (plan.gearLeft > 0) message += " " + "PhobosCamp_GearLeftPacked".Translate(plan.gearLeft.Named("COUNT")).Resolve();
        Messages.Message(message, Vehicle, MessageTypeDefOf.SilentInput, historical: false);
        if (Seen.Wants("vehicles.camp-set-up", Vehicle))
        {
            Seen.Note("vehicles.camp-set-up", Vehicle, (plan.tent.HasValue ? "tent, " : "") + plan.poles.Count + " poles, " + plan.awning.Count + " roofed, " + plan.gear.Count + " gear, "
                + plan.gearLeft + " left packed");
        }
    }

    /// <summary>Takes a packed piece out of the cargo and out of its crate, ready to stand.</summary>
    Thing? TakeFromCargo(MinifiedThing minified)
    {
        if (minified.holdingOwner != null)
        {
            MinifiedThing one = minified.stackCount > 1 ? (MinifiedThing)minified.SplitOff(1) : minified;
            one.holdingOwner?.Remove(one);
            minified = one;
        }
        Thing? inner = minified.InnerThing;
        if (inner == null) return null;
        minified.InnerThing = null;
        if (!minified.Destroyed) minified.Destroy();
        return inner;
    }

    /// <summary>Packs the camp: the tent back to its mount, gear rolled into the cargo where each stood remembered, the awning struck. A piece the
    /// player has taken away is left as theirs. <paramref name="leaving"/>: the vehicle is leaving the map, so the work is not waited for.</summary>
    void Strike(bool leaving)
    {
        if (!IsPitched) return;
        CampLayoutRule.Frame frame = FrameFor(pitchedSide);
        Map? map = Vehicle.MapHeld ?? placed.Concat(poles).FirstOrDefault(t => t.Spawned)?.Map;
        int packed = 0, left = 0;
        for (int index = 0; index < placed.Count; index++)
        {
            Thing piece = placed[index];
            string slot = index < placedSlots.Count ? placedSlots[index] : "";
            if (piece.Destroyed) continue;
            if (!piece.Spawned || piece.Map != map)
            {
                left++;
                continue;
            }
            if (piece.def == VehicleCampDefOf.PhobosCamp_RoofTent)
            {
                piece.DeSpawn();
                // Its kit came back when the tent was taken off its mount: a tent struck with none fitted goes nowhere.
                if (TentFitted) Held.TryAdd(piece);
                else piece.Destroy();
                packed++;
                continue;
            }
            // A bed someone sleeps in stays, if the vehicle must go without it.
            if (leaving && piece is Building_Bed bed && bed.CurOccupants.Any())
            {
                left++;
                continue;
            }
            Remember(frame, slot, piece);
            MinifiedThing minified = piece.MakeMinified();
            if (minified == null || !(Vehicle.inventory?.innerContainer.TryAdd(minified) ?? false))
            {
                if (minified != null && map != null) GenPlace.TryPlaceThing(minified, piece.PositionHeld, map, ThingPlaceMode.Near);
                left++;
                continue;
            }
            packed++;
        }
        placed.Clear();
        placedSlots.Clear();
        foreach (Thing pole in poles)
        {
            if (!pole.Destroyed) pole.Destroy();
        }
        poles.Clear();
        Forget();
        NamedArgument name = Vehicle.LabelShort.Named("VEHICLE");
        string message = (leaving ? "PhobosCamp_PackedLeaving" : "PhobosCamp_PackedUp").Translate(name).Resolve();
        if (left > 0) message += " " + "PhobosCamp_PiecesLeft".Translate(left.Named("COUNT")).Resolve();
        if (Vehicle.Faction == Faction.OfPlayer) Messages.Message(message, new LookTargets(Vehicle), MessageTypeDefOf.SilentInput, historical: false);
        string variant = leaving ? "leaving" : "by hand";
        if (Seen.Wants("vehicles.camp-packed", Vehicle, variant)) Seen.Note("vehicles.camp-packed", Vehicle, packed + " packed, " + left + " left", variant);
    }

    void Remember(CampLayoutRule.Frame frame, string slot, Thing piece)
    {
        if (slot.NullOrEmpty() || slot == TentSlot) return;
        (int x, int z) = frame.FromMap(piece.Position.x, piece.Position.z);
        int direction = frame.FromMapRotation(piece.Rotation.AsInt);
        int index = spotSlots.IndexOf(slot);
        if (index < 0)
        {
            spotSlots.Add(slot);
            spotCells.Add(new IntVec3(x, 0, z));
            spotDirections.Add(direction);
        }
        else
        {
            spotCells[index] = new IntVec3(x, 0, z);
            spotDirections[index] = direction;
        }
    }

    /// <summary>Forgets every remembered spot: the next camp is laid out afresh.</summary>
    void ForgetSpots()
    {
        spotSlots.Clear();
        spotCells.Clear();
        spotDirections.Clear();
        Forget();
    }

    // ---- Another faction's camp at its own base ----

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (order != CampOrder.None) CampOrders.Note(this, true);
        if (!respawningAfterLoad && Vehicle.Faction is { } faction && faction != Faction.OfPlayer && Vehicle.Map?.Parent is Settlement { Faction: var owner } && owner == faction)
        {
            campAtBase = true;
            StartTicking();
        }
    }

    public override void CompTick()
    {
        base.CompTick();
        if (!campAtBase)
        {
            StopTicking();
            return;
        }
        campAtBase = false;
        StopTicking();
        PitchAtBase();
    }

    /// <summary>
    /// By chance, a base's vehicle stands with its camp out: its awning and roof tent if it carries them, and a few swags of its faction's (the
    /// record's "other factions' vehicles"), placed once as plain things. Nothing runs afterwards; a player who takes the vehicle packs them.
    /// </summary>
    void PitchAtBase()
    {
        if (!Vehicle.Spawned || IsPitched || !(TentFitted || AwningFitted) || !Rand.Chance(Rules.campAtBaseChance)) return;
        Map map = Vehicle.Map;
        var gear = new List<MinifiedThing>();
        int swags = Rules.campAtBaseSwags.RandomInRange;
        for (int index = 0; index < swags; index++)
        {
            Thing swag = ThingMaker.MakeThing(VehicleCampDefOf.PhobosCamp_Swag, GenStuff.DefaultStuffFor(VehicleCampDefOf.PhobosCamp_Swag));
            gear.Add(swag.MakeMinified());
        }
        preferredSide = Rand.Bool ? CampLayoutRule.Left : CampLayoutRule.Right;
        Plan plan = PlanFor(preferredSide, map, TentFitted, AwningFitted, gear);
        if (plan.refusal != null) plan = PlanFor(-preferredSide, map, TentFitted, AwningFitted, gear);
        if (plan.refusal != null)
        {
            foreach (MinifiedThing unused in gear) unused.Destroy();
            return;
        }
        Pitch(plan, fromCargo: false);
        foreach (MinifiedThing unused in gear.Where(g => !g.Destroyed)) unused.Destroy();
        if (Seen.Wants("vehicles.camp-at-base", Vehicle)) Seen.Note("vehicles.camp-at-base", Vehicle, Vehicle.Faction?.Name + ": " + placed.Count + " pieces, " + poles.Count + " poles");
    }

    // ---- Leaving, lost ----

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        CampOrders.Note(this, false);
        order = CampOrder.None;
        campAtBase = false;
        if (IsPitched)
        {
            if (mode is DestroyMode.KillFinalize or DestroyMode.Deconstruct or DestroyMode.FailConstruction or DestroyMode.Cancel)
            {
                // Lost: the camp stays as ordinary things; the packed tent went with the roof it was mounted on.
                placed.Clear();
                placedSlots.Clear();
                poles.Clear();
            }
            else
            {
                // Leaving the map (a caravan, a flight): what can be packed goes with it.
                Strike(leaving: true);
            }
        }
        base.PostDeSpawn(map, mode);
    }

    // ---- Buttons ----

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        if (Vehicle.Faction != Faction.OfPlayer || !Vehicle.Spawned) yield break;
        if (order != CampOrder.None)
        {
            yield return new Command_Action
            {
                defaultLabel = (order == CampOrder.SetUp ? "PhobosCamp_GizmoCancelSetUp" : "PhobosCamp_GizmoCancelPackUp").Translate().Resolve(),
                defaultDesc = "PhobosCamp_GizmoCancelDesc".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(),
                icon = SystemIcons.For("UI/Designators/Cancel"),
                action = CancelOrder,
            };
            yield break;
        }
        if (IsPitched)
        {
            var pack = new Command_Action
            {
                defaultLabel = "PhobosCamp_GizmoPackUp".Translate().Resolve(),
                defaultDesc = "PhobosCamp_GizmoPackUpDesc".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(),
                icon = SystemIcons.For(Icons + "PackUpCamp"),
                action = () => OrderCamp(false),
            };
            if (PackUpRefusal() is { } refused) pack.Disable(refused);
            yield return pack;
            yield break;
        }
        if (!HasCampGear) yield break;
        var setUp = new Command_PhobosCampPreview(this)
        {
            defaultLabel = "PhobosCamp_GizmoSetUp".Translate().Resolve(),
            defaultDesc = "PhobosCamp_GizmoSetUpDesc".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(),
            icon = SystemIcons.For(Icons + "SetUpCamp"),
            action = () => OrderCamp(true),
        };
        if (SetUpRefusal() is { } refusal) setUp.Disable(refusal);
        yield return setUp;
        bool left = preferredSide == CampLayoutRule.Left;
        yield return new Command_Action
        {
            defaultLabel = (left ? "PhobosCamp_GizmoSideLeft" : "PhobosCamp_GizmoSideRight").Translate().Resolve(),
            defaultDesc = "PhobosCamp_GizmoSideDesc".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(),
            icon = SystemIcons.For(left ? "UI/Widgets/RotLeft" : "UI/Widgets/RotRight"),
            action = () =>
            {
                preferredSide = -preferredSide;
                Forget();
            },
        };
        if (spotSlots.Count > 0)
        {
            yield return new Command_Action
            {
                defaultLabel = "PhobosCamp_GizmoForget".Translate().Resolve(),
                defaultDesc = "PhobosCamp_GizmoForgetDesc".Translate().Resolve(),
                icon = SystemIcons.For("UI/Buttons/Dev/Reload"),
                action = () =>
                {
                    ForgetSpots();
                    Messages.Message("PhobosCamp_SpotsForgotten".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(), Vehicle, MessageTypeDefOf.SilentInput, historical: false);
                },
            };
        }
    }

    /// <summary>Draws the camp's layout on the map: what the set-up button would do.</summary>
    internal void DrawPreview()
    {
        Plan? plan = CurrentPlan;
        if (plan == null || plan.refusal != null) return;
        if (plan.awning.Count > 0) GenDraw.DrawFieldEdges(plan.awning, new Color(0.85f, 0.78f, 0.55f));
        List<IntVec3> pieces = plan.PieceCells().ToList();
        if (pieces.Count > 0) GenDraw.DrawFieldEdges(pieces, Color.white);
    }

    public override string? CompInspectStringExtra()
    {
        if (UiGuard.Resting(GuardKey)) return UiGuard.FallbackText;
        try
        {
            return order switch
            {
                CampOrder.SetUp => "PhobosCamp_InspectSetUpWaiting".Translate().Resolve(),
                CampOrder.PackUp => "PhobosCamp_InspectPackUpWaiting".Translate().Resolve(),
                _ => IsPitched ? "PhobosCamp_InspectPitched".Translate().Resolve() : null,
            };
        }
        catch (System.Exception e) { UiGuard.Report(GuardKey, e); return null; }
    }

    const string GuardKey = "A vehicle's camp line";

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Deep.Look(ref held, "PhobosCamp_Held", this);
        Scribe_Collections.Look(ref placed, "PhobosCamp_Placed", LookMode.Reference);
        Scribe_Collections.Look(ref placedSlots, "PhobosCamp_PlacedSlots", LookMode.Value);
        Scribe_Collections.Look(ref poles, "PhobosCamp_Poles", LookMode.Reference);
        Scribe_Values.Look(ref preferredSide, "PhobosCamp_Side", CampLayoutRule.Left);
        Scribe_Values.Look(ref pitchedSide, "PhobosCamp_PitchedSide", CampLayoutRule.Left);
        Scribe_Values.Look(ref order, "PhobosCamp_Order", CampOrder.None);
        Scribe_Collections.Look(ref spotSlots, "PhobosCamp_SpotSlots", LookMode.Value);
        Scribe_Collections.Look(ref spotCells, "PhobosCamp_SpotCells", LookMode.Value);
        Scribe_Collections.Look(ref spotDirections, "PhobosCamp_SpotDirections", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            placed ??= new List<Thing>();
            placedSlots ??= new List<string>();
            poles ??= new List<Thing>();
            spotSlots ??= new List<string>();
            spotCells ??= new List<IntVec3>();
            spotDirections ??= new List<int>();
            // A reference lost with its thing leaves a hole: keep the slots in step with the pieces.
            for (int index = placed.Count - 1; index >= 0; index--)
            {
                if (placed[index] != null) continue;
                placed.RemoveAt(index);
                if (index < placedSlots.Count) placedSlots.RemoveAt(index);
            }
            poles.RemoveAll(t => t == null);
            if (spotCells.Count != spotSlots.Count || spotDirections.Count != spotSlots.Count) ForgetSpots();
        }
    }
}

/// <summary>What a colonist has been asked to do at a vehicle's camp. Saved by name: append only.</summary>
public enum CampOrder
{
    None,
    SetUp,
    PackUp,
}

/// <summary>The set-up button, which shows the camp's layout on the map while the pointer is over it (the record's Y3: "previewed first").</summary>
public class Command_PhobosCampPreview : Command_Action
{
    readonly CompPhobosCamp camp;

    public Command_PhobosCampPreview(CompPhobosCamp camp) => this.camp = camp;

    public override void GizmoUpdateOnMouseover()
    {
        base.GizmoUpdateOnMouseover();
        camp.DrawPreview();
    }
}
