using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Interface;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using Vehicles;
using Verse;
using Verse.Sound;

namespace Phobos.VehicleSystems;

/// <summary>
/// Set-out pieces (Framework 0.121.0; docs/development/vehicle-trailers.md, B0.4): when a colonist sets a working trailer up, it puts real
/// buildings beside itself, a canvas room among them, so the game's own cooking, washing and storage work unchanged; packing up lifts them.
/// It follows whichever set-up the trailer has (its tank, or a plain set-up) as one of that set-up's parts.
///
/// While packed, the pieces are held by the trailer, so a range keeps its fuel and bills and a cold box its contents, and a save holds each
/// once. Walls, doors and the power point are made new each time: they have nothing to keep. Nothing of the player's is ever deleted: what
/// lies inside a room stays on the ground, a piece's contents stay in it, and a trailer destroyed while set up leaves its pieces standing as
/// ordinary buildings.
/// </summary>
public sealed class CompPhobosSetOut : VehicleComp, ISetUpPart, IThingHolder
{
    ThingOwner<Thing>? packed;
    List<Thing> placed = new();
    List<Thing> structure = new();
    List<IntVec3> roofed = new();
    Building? powerPoint;
    int side = 1;

    public CompProperties_PhobosSetOut Props => (CompProperties_PhobosSetOut)props;

    ThingOwner<Thing> Packed => packed ??= new ThingOwner<Thing>(this, oneStackOnly: false);

    /// <summary>Whether its pieces stand beside it now.</summary>
    public bool IsSetOut => placed.Count > 0 || structure.Count > 0;

    /// <summary>Plugged into a powered grid through its power point.</summary>
    public bool Powered => powerPoint is { Spawned: true } && powerPoint.GetComp<CompPowerTrader>() is { PowerOn: true };

    /// <summary>The pieces standing beside it now.</summary>
    public IReadOnlyList<Thing> Placed => placed;

    /// <summary>The pieces it holds while packed, for the caravan's cooks.</summary>
    public IEnumerable<Thing> PackedPieces => Packed;

    // ---- IThingHolder: the pieces held while packed ----

    public ThingOwner GetDirectlyHeldThings() => Packed;

    public void GetChildHolders(List<IThingHolder> outChildren) => ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, Packed);

    // ThingComp's own ParentHolder skips to the trailer's holder; a held piece belongs to the trailer itself.
    IThingHolder IThingHolder.ParentHolder => Vehicle;

    /// <summary>The walls, doors and power point standing now.</summary>
    public IReadOnlyList<Thing> Structure => structure;

    // ---- Where everything goes ----

    /// <summary>A cell in the trailer's own frame on one side, on the map (the geometry is SetOutRule's, game-free and tested).</summary>
    IntVec3 ToMap(IntVec2 local, int onSide)
    {
        (int x, int z) north = SetOutRule.NorthFrame(Vehicle.VehicleDef.Size.x, onSide, local.x, local.z);
        (int x, int z) turned = SetOutRule.Rotate(north.x, north.z, Vehicle.Rotation.AsInt);
        return Vehicle.Position + new IntVec3(turned.x, 0, turned.z);
    }

    /// <summary>The rotation that has the piece worked from the side its facing names.</summary>
    Rot4 ToMap(SetOutFacing facing, int onSide) => new(SetOutRule.Rotation((int)facing, onSide, Vehicle.Rotation.AsInt));

    CellRect ToMap(IntVec2 from, IntVec2 to, int onSide) => CellRect.FromLimits(ToMap(from, onSide), ToMap(to, onSide));

    bool Wanted(string? node) => node.NullOrEmpty() || VehicleUpgrades.Unlocked(Vehicle, node!);

    /// <summary>Everything one side would hold: each piece where the game's footprint rule puts it, each room's walls, door and inside.</summary>
    sealed class Layout
    {
        public readonly List<(SetOutPiece piece, IntVec3 centre, Rot4 rot, CellRect rect)> pieces = new();
        public readonly List<(IntVec3 cell, bool door)> walls = new();
        public readonly List<IntVec3> inside = new();
        public IntVec3 powerPoint = IntVec3.Invalid;

        public IEnumerable<IntVec3> AllCells() =>
            pieces.SelectMany(p => p.rect.Cells).Concat(walls.Select(w => w.cell)).Concat(inside)
                .Concat(powerPoint.IsValid ? new[] { powerPoint } : Enumerable.Empty<IntVec3>()).Distinct();
    }

    Layout LayoutFor(int onSide)
    {
        var layout = new Layout();
        // Rooms may share a wall (the kitchen's annex is pitched against its front): a shared cell is built once, and a door in either room's
        // wall there wins, so the kitchen's flap becomes the door between the two.
        var walls = new Dictionary<IntVec3, bool>();
        foreach (SetOutRoom room in Props.rooms.Where(r => Wanted(r.node)))
        {
            CellRect rect = ToMap(room.from, room.to, onSide);
            IntVec3 door = ToMap(room.door, onSide);
            foreach (IntVec3 cell in rect.EdgeCells) walls[cell] = (walls.TryGetValue(cell, out bool already) && already) || cell == door;
            layout.inside.AddRange(rect.ContractedBy(1).Cells);
        }
        foreach (KeyValuePair<IntVec3, bool> wall in walls) layout.walls.Add((wall.Key, wall.Value));
        foreach (SetOutPiece piece in Props.pieces.Where(p => p.def != null && Wanted(p.node)))
        {
            CellRect rect = ToMap(piece.from, piece.to, onSide);
            Rot4 rot = ToMap(piece.facing, onSide);
            IntVec3 centre = rect.Cells.FirstOrDefault(c => GenAdj.OccupiedRect(c, rot, piece.def!.Size).Equals(rect), IntVec3.Invalid);
            if (!centre.IsValid)
            {
                VehicleSystemsMod.Logger.Warn("A set-out piece of " + Vehicle.def.defName + " (" + piece.def!.defName + ") does not fit the cells its Def gives; it is left out.");
                continue;
            }
            layout.pieces.Add((piece, centre, rot, rect));
        }
        if (Props.powerPointDef != null) layout.powerPoint = ToMap(Props.powerPointCell, onSide);
        return layout;
    }

    /// <summary>Why this cell cannot take a set-out piece, or null. Every thing in it is looked at, not only buildings: another mod's tent is no edifice.</summary>
    string? CellRefusal(IntVec3 cell, Map map, ThingDef? def)
    {
        if (!cell.InBounds(map)) return "PhobosSetOut_BlockedEdge".Translate().Resolve();
        if (!cell.Standable(map) && cell.GetEdifice(map) == null) return "PhobosSetOut_BlockedGround".Translate().Resolve();
        if (def?.terrainAffordanceNeeded is { } needed && !cell.GetTerrain(map).affordances.Contains(needed)) return "PhobosSetOut_BlockedGround".Translate().Resolve();
        foreach (Thing thing in cell.GetThingList(map))
        {
            if (thing is Building) return "PhobosSetOut_BlockedBuilding".Translate(thing.LabelShort.Named("THING")).Resolve();
            if (thing is Blueprint or Frame) return "PhobosSetOut_BlockedBuilding".Translate(thing.LabelShort.Named("THING")).Resolve();
            if (thing is Pawn) return "PhobosSetOut_BlockedPawn".Translate(thing.LabelShort.Named("THING")).Resolve();
            if (thing.def.category == ThingCategory.Item) return "PhobosSetOut_BlockedItem".Translate(thing.LabelShort.Named("THING")).Resolve();
            if (thing is Plant { def.plant.IsTree: true }) return "PhobosSetOut_BlockedTree".Translate().Resolve();
        }
        foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
        {
            if (pawn is VehiclePawn other && other != Vehicle && other.OccupiedRect().Contains(cell)) return "PhobosSetOut_BlockedBuilding".Translate(other.LabelShort.Named("THING")).Resolve();
        }
        return null;
    }

    /// <summary>Why this side cannot take everything, or null.</summary>
    string? SideRefusal(Layout layout, Map map)
    {
        foreach ((SetOutPiece piece, _, _, CellRect rect) in layout.pieces)
        {
            foreach (IntVec3 cell in rect.Cells)
            {
                if (CellRefusal(cell, map, piece.def) is { } refusal) return refusal;
            }
        }
        foreach ((IntVec3 cell, _) in layout.walls)
        {
            if (CellRefusal(cell, map, Props.wallDef) is { } refusal) return refusal;
        }
        foreach (IntVec3 cell in layout.inside)
        {
            if (CellRefusal(cell, map, null) is { } refusal) return refusal;
        }
        return null;
    }

    // ---- ISetUpPart ----

    // Read by the set-up button every frame it shows, so worked out at most once a second (buttons read caches, never compute). Setting out
    // looks again for itself before it places anything, so an answer a second old never puts a piece where it should not go.
    const int RefusalTicks = 60;
    string? cachedRefusal;
    int refusalTick = -RefusalTicks;

    public string? SetUpPartRefusal()
    {
        int now = Find.TickManager.TicksGame;
        if (now - refusalTick < RefusalTicks && now >= refusalTick) return cachedRefusal;
        refusalTick = now;
        return cachedRefusal = ComputeSetUpRefusal();
    }

    string? ComputeSetUpRefusal()
    {
        if (!Vehicle.Spawned || IsSetOut) return null;
        Map map = Vehicle.Map;
        string? first = SideRefusal(LayoutFor(1), map);
        if (first == null) return null;
        string? second = SideRefusal(LayoutFor(-1), map);
        if (second == null) return null;
        return "PhobosSetOut_NoRoom".Translate(Props.setOutLabel.Named("WHAT"), Vehicle.LabelShort.Named("VEHICLE"), first.Named("REASON")).Resolve();
    }

    public string? PackUpPartRefusal()
    {
        if (!Vehicle.Spawned) return null;
        foreach (Thing piece in placed)
        {
            if (piece.Spawned && piece.Map.reservationManager.IsReservedByAnyoneOf(piece, Faction.OfPlayer))
            {
                return "PhobosSetOut_PieceInUse".Translate(piece.LabelShort.Named("PIECE"), Vehicle.LabelShort.Named("VEHICLE")).Resolve();
            }
        }
        return null;
    }

    public void SetUpChanged(bool on)
    {
        if (!Vehicle.Spawned) return;
        if (on) SetOut();
        else PackUp();
    }

    void SetOut()
    {
        if (IsSetOut) return;
        Map map = Vehicle.Map;
        side = SideRefusal(LayoutFor(1), map) == null ? 1 : -1;
        Layout layout = LayoutFor(side);
        if (SideRefusal(layout, map) is { } refusal)
        {
            Messages.Message("PhobosSetOut_NoRoom".Translate(Props.setOutLabel.Named("WHAT"), Vehicle.LabelShort.Named("VEHICLE"), refusal.Named("REASON")).Resolve(), Vehicle, MessageTypeDefOf.RejectInput);
            return;
        }
        Faction? faction = Vehicle.Faction;
        foreach ((IntVec3 cell, bool door) in layout.walls)
        {
            Thing wall = ThingMaker.MakeThing(door ? Props.doorDef : Props.wallDef);
            wall.SetFaction(faction);
            structure.Add(GenSpawn.Spawn(wall, cell, map, WipeMode.Vanish));
        }
        foreach (IntVec3 cell in layout.inside)
        {
            if (map.roofGrid.RoofAt(cell) != null) continue;
            map.roofGrid.SetRoof(cell, Props.roofDef);
            roofed.Add(cell);
            map.areaManager.BuildRoof[cell] = false;
            map.areaManager.NoRoof[cell] = false;
        }
        foreach ((SetOutPiece piece, IntVec3 centre, Rot4 rot, _) in layout.pieces)
        {
            Thing? thing = Packed.FirstOrDefault(t => t.def == piece.def);
            if (thing != null) Packed.Remove(thing);
            else thing = MakePiece(piece);
            if (thing.Faction != faction) thing.SetFaction(faction);
            placed.Add(GenSpawn.Spawn(thing, centre, map, rot, WipeMode.Vanish));
        }
        if (layout.powerPoint.IsValid && CellRefusal(layout.powerPoint, map, Props.powerPointDef) == null)
        {
            Thing point = ThingMaker.MakeThing(Props.powerPointDef);
            point.SetFaction(faction);
            powerPoint = (Building)GenSpawn.Spawn(point, layout.powerPoint, map, WipeMode.Vanish);
        }
        SetOuts.Register(this);
        Props.pitchSound?.PlayOneShot(new TargetInfo(Vehicle.Position, map));
        Messages.Message("PhobosSetOut_Done".Translate(Props.setOutLabel.Named("WHAT"), Vehicle.LabelShort.Named("VEHICLE")).Resolve(), Vehicle, MessageTypeDefOf.SilentInput, historical: false);
        Seen.Note("trailers.set-out", Vehicle, placed.Count + " pieces, " + roofed.Count + " roofed cells");
    }

    Thing MakePiece(SetOutPiece piece)
    {
        ThingDef def = piece.def!;
        ThingDef? stuff = def.MadeFromStuff ? piece.stuff ?? GenStuff.DefaultStuffFor(def) : null;
        Thing thing = ThingMaker.MakeThing(def, stuff);
        if (piece.storeOnly != null && thing is IStoreSettingsParent store && store.GetStoreSettings() is { } settings)
        {
            settings.filter.SetDisallowAll();
            settings.filter.SetAllow(piece.storeOnly, true);
        }
        return thing;
    }

    /// <summary>Takes the canvas roof off first, so it never falls on anyone when its walls go: only cells it laid, still holding its roof.</summary>
    public void LiftRoof()
    {
        Map? map = Vehicle.MapHeld ?? structure.FirstOrDefault(t => t.Spawned)?.Map;
        if (map != null)
        {
            foreach (IntVec3 cell in roofed)
            {
                if (cell.InBounds(map) && map.roofGrid.RoofAt(cell) == Props.roofDef) map.roofGrid.SetRoof(cell, null);
            }
        }
        roofed.Clear();
    }

    void PackUp()
    {
        if (!IsSetOut) return;
        LiftRoof();
        Props.pitchSound?.PlayOneShot(new TargetInfo(Vehicle.Position, Vehicle.Map));
        foreach (Thing piece in placed)
        {
            if (piece.Destroyed) continue;
            if (piece.Spawned) piece.DeSpawn();
            Packed.TryAdd(piece);
        }
        placed.Clear();
        SetOuts.Unregister(this);
        foreach (Thing thing in structure.Append(powerPoint).OfType<Thing>())
        {
            if (!thing.Destroyed) thing.Destroy(DestroyMode.Vanish);
        }
        structure.Clear();
        powerPoint = null;
        Messages.Message("PhobosSetOut_PackedUp".Translate(Props.setOutLabel.Named("WHAT"), Vehicle.LabelShort.Named("VEHICLE")).Resolve(), Vehicle, MessageTypeDefOf.SilentInput, historical: false);
        Seen.Note("trailers.pack-up", Vehicle);
    }

    // ---- Power ----

    public override void CompTickRare()
    {
        base.CompTickRare();
        if (powerPoint is not { Spawned: true } || powerPoint.GetComp<CompPowerTrader>() is not { } trader) return;
        float watts = Props.idleWatts;
        foreach (Thing piece in placed)
        {
            if (piece is ISetOutLoad load) watts += load.WattsNow;
            else if (piece is ThingWithComps withComps)
            {
                foreach (ThingComp comp in withComps.AllComps)
                {
                    if (comp is ISetOutLoad compLoad) watts += compLoad.WattsNow;
                }
            }
        }
        trader.PowerOutput = -System.Math.Max(1f, watts);
    }

    // ---- Life and saving ----

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (IsSetOut) SetOuts.Register(this);
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        SetOuts.Unregister(this);
        if (mode is DestroyMode.KillFinalize or DestroyMode.Deconstruct or DestroyMode.FailConstruction or DestroyMode.Cancel)
        {
            // Destroyed while set up: what stands beside it stays as ordinary buildings, and what it held is set down, never lost.
            placed.Clear();
            structure.Clear();
            roofed.Clear();
            powerPoint = null;
            Packed.TryDropAll(Vehicle.Position, map, ThingPlaceMode.Near);
        }
        base.PostDeSpawn(map, mode);
    }

    public override string? CompInspectStringExtra()
    {
        if (UiGuard.Resting(GuardKey)) return UiGuard.FallbackText;
        try
        {
            if (!IsSetOut) return null;
            if (Props.powerPointDef == null || powerPoint == null) return "PhobosSetOut_InspectSetOut".Translate(Props.setOutLabel.Named("WHAT")).Resolve();
            return Powered
                ? "PhobosSetOut_InspectPowered".Translate(Props.setOutLabel.Named("WHAT"), (-(powerPoint.GetComp<CompPowerTrader>()?.PowerOutput ?? 0f)).ToString("0").Named("WATTS")).Resolve()
                : "PhobosSetOut_InspectUnpowered".Translate(Props.setOutLabel.Named("WHAT")).Resolve();
        }
        catch (System.Exception e) { UiGuard.Report(GuardKey, e); return null; }
    }

    const string GuardKey = "A trailer's set-out line";

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Deep.Look(ref packed, "PhobosSetOut_Packed", this);
        Scribe_Collections.Look(ref placed, "PhobosSetOut_Placed", LookMode.Reference);
        Scribe_Collections.Look(ref structure, "PhobosSetOut_Structure", LookMode.Reference);
        Scribe_Collections.Look(ref roofed, "PhobosSetOut_Roofed", LookMode.Value);
        Scribe_References.Look(ref powerPoint, "PhobosSetOut_PowerPoint");
        Scribe_Values.Look(ref side, "PhobosSetOut_Side", 1);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            placed ??= new List<Thing>();
            structure ??= new List<Thing>();
            roofed ??= new List<IntVec3>();
            placed.RemoveAll(t => t == null);
            structure.RemoveAll(t => t == null);
        }
    }
}

/// <summary>Which trailer each set-out piece belongs to: kept as trailers set out and pack up, so a piece asks in one look. Derived, never saved.</summary>
public static class SetOuts
{
    static readonly Dictionary<Thing, CompPhobosSetOut> owners = new();
    static Game? game;

    static void Check()
    {
        if (game == Current.Game) return;
        owners.Clear();
        game = Current.Game;
    }

    internal static void Register(CompPhobosSetOut setOut)
    {
        Check();
        foreach (Thing piece in setOut.Placed.Concat(setOut.Structure)) owners[piece] = setOut;
    }

    internal static void Unregister(CompPhobosSetOut setOut)
    {
        Check();
        foreach (Thing piece in owners.Where(p => p.Value == setOut).Select(p => p.Key).ToList()) owners.Remove(piece);
    }

    /// <summary>The trailer that set this piece out, or null.</summary>
    public static CompPhobosSetOut? Owner(Thing piece)
    {
        Check();
        return owners.TryGetValue(piece, out CompPhobosSetOut? setOut) ? setOut : null;
    }

    /// <summary>Whether the trailer that set this piece out is plugged in.</summary>
    public static bool Powered(Thing piece) => Owner(piece)?.Powered == true;
}
