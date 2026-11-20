using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// What a fall does to a dropped sling load (Framework 0.101.0, sling slice 2). Each part loses a share of its health set by the height of
/// the fall (SlingRule.DropShare, authored), varied part by part; people riding in it are hurt. A gameplay simplification of a heavy impact:
/// every part keeps a sliver of health, so the fall alone never destroys the vehicle outright and there is always a wreck to recover.
/// </summary>
public static class SlingDrops
{
    /// <summary>How much the share varies from part to part (authored): from this much less to this much more.</summary>
    const float Spread = 0.4f;
    /// <summary>The health every part keeps after a fall.</summary>
    const float Sliver = 1f;
    /// <summary>Hits each rider takes, sharing the damage.</summary>
    const int RiderHits = 3;

    static PhobosVehicleRulesDef Rules => PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();

    /// <summary>Applies a fall of this height to a spawned vehicle and the people in it. Returns a short phrase for the message.</summary>
    public static string Fall(VehiclePawn load, float metres)
    {
        PhobosVehicleRulesDef rules = Rules;
        double share = SlingRule.DropShare(metres, rules.slingDropSafeMetres, rules.slingDropRuinMetres);
        if (share <= 0) return "PhobosSling_HarmNone".Translate().Resolve();
        foreach (VehicleComponent part in load.statHandler.components.ToList())
        {
            if (load.Destroyed) break;
            float amount = (float)(share * part.MaxHealth * Rand.Range(1f - Spread, 1f + Spread));
            amount = System.Math.Min(amount, part.Health - Sliver);
            if (amount <= 0f) continue;
            part.TakeDamage(load, new DamageInfo(DamageDefOf.Blunt, amount, 999f), ignoreArmor: true);
        }
        float riderDamage = (float)(share * 10.0 * rules.slingDropPersonDamage);
        foreach (Pawn rider in load.AllPawnsAboard.ToList())
        {
            for (int i = 0; i < RiderHits && !rider.Dead; i++) rider.TakeDamage(new DamageInfo(DamageDefOf.Blunt, riderDamage / RiderHits));
        }
        load.GetComp<CompPhobosVehicleSystems>()?.UpdateNow();
        return "PhobosSling_HarmShare".Translate(((float)share).ToStringPercent().Named("SHARE")).Resolve();
    }

    /// <summary>The same for any slung load (Framework 0.197.0): a vehicle as above; a hull, being a building, loses that share of its
    /// full hit points, so one already near nothing breaks up.</summary>
    public static string FallOf(Thing load, float metres)
    {
        if (load is VehiclePawn vehicle) return Fall(vehicle, metres);
        PhobosVehicleRulesDef rules = Rules;
        double share = SlingRule.DropShare(metres, rules.slingDropSafeMetres, rules.slingDropRuinMetres);
        if (share <= 0) return "PhobosSling_HarmNone".Translate().Resolve();
        load.TakeDamage(new DamageInfo(DamageDefOf.Blunt, (float)(share * load.MaxHitPoints)));
        return "PhobosSling_HarmShare".Translate(((float)share).ToStringPercent().Named("SHARE")).Resolve();
    }
}

/// <summary>
/// Vehicles left lying on the world map, to be recovered: a sling load let go in flight (Framework 0.101.0; owner, 12 Nov 2026: a wreck a
/// caravan can recover), and since Framework 0.187.0 a convoy whose last colonist died (docs/development/vehicle-emergent-play.md, EM2: what
/// is lost stays in the world). The vehicles lie held here, with whoever rode in them, until a caravan reaches the tile; then a small map is
/// made, the vehicles are put on it (a dropped load takes the fall's damage), and the caravan enters. When nobody of ours is left there, the
/// map goes: a vehicle still lying on it is taken back to wait for the next visit, and the place goes once every vehicle has been driven,
/// towed or lifted away. Older saves held one vehicle and load as before.
/// </summary>
public class PhobosSlingWreck : MapParent, IThingHolder, IPhobosVehicleHolder
{
    /// <summary>What waits here: vehicles, and since Framework 0.199.0 a depot's items too (docs/development/vehicle-depots.md).</summary>
    ThingOwner<Thing> held;
    List<VehiclePawn> onMap = new();
    /// <summary>Items put on the site's map, taken back when the map goes (Framework 0.199.0).</summary>
    List<Thing> onMapItems = new();
    float fallMetres;
    /// <summary>A depot's place in words, kept from when it was laid: the world map asks for its label every frame.</summary>
    string placeWords = "";

    // A depot's clocks (Framework 0.200.0; docs/development/vehicle-depots.md, batch 2: DepotPressures). Saved.
    /// <summary>When the depot's fuel was, on average, put there; -1 with none yet.</summary>
    internal double fuelClock = -1;
    /// <summary>When the hostile neighbour finds it, or -1; and who.</summary>
    internal int foundAtTick = -1;
    internal Faction? foundBy;
    /// <summary>Fractions of a unit owed to the weather and to staleness, carried day to day instead of rolled for.</summary>
    internal double leakCarry;
    internal Dictionary<string, double> staleCarry = new();

    /// <summary>Once a game day, staggered by depot: what time does to it (Framework 0.200.0). Nothing for a wreck or a lost convoy.</summary>
    protected override void Tick()
    {
        base.Tick();
        if (IsDepot && (Find.TickManager.TicksGame + ID) % GenDate.TicksPerDay == 0) DepotPressures.Daily(this);
    }

    public PhobosSlingWreck() => held = new ThingOwner<Thing>(this, oneStackOnly: false);

    static PhobosVehicleRulesDef Rules => PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();

    /// <summary>The first vehicle waiting here, or lying on the map made for it.</summary>
    public VehiclePawn? Wreck => held.InnerListForReading.OfType<VehiclePawn>().FirstOrDefault() ?? onMap.FirstOrDefault(v => v != null && !v.Destroyed);

    /// <summary>Every vehicle waiting here, or lying on the map made for them.</summary>
    public IEnumerable<VehiclePawn> Vehicles => held.InnerListForReading.OfType<VehiclePawn>().Concat(onMap.Where(v => v != null && !v.Destroyed));

    /// <summary>A depot's items waiting here, not in a vehicle (Framework 0.199.0, DP1(b)).</summary>
    public IEnumerable<Thing> Items => held.InnerListForReading.Where(t => t is not VehiclePawn);

    /// <summary>A depot laid on purpose (Framework 0.199.0), rather than a wreck or a lost convoy.</summary>
    public bool IsDepot => def == PhobosSlingWreckDefOf.PhobosDepot;

    /// <summary>For the fleet board (Framework 0.188.0): what lies here is still the player's to find.</summary>
    public IEnumerable<VehiclePawn> HeldVehicles => Vehicles;

    /// <summary>A convoy left where its last colonist died (Framework 0.187.0), rather than one dropped load.</summary>
    bool LostConvoy => def == PhobosSlingWreckDefOf.PhobosLostConvoy;

    public override string Label
    {
        get
        {
            if (IsDepot) return "PhobosDepot_Label".Translate(placeWords.Named("PLACE")).Resolve();
            if (Wreck is not { } wreck) return base.Label;
            if (!LostConvoy) return "PhobosSling_WreckLabel".Translate(wreck.LabelShort.Named("LOAD")).Resolve();
            return "PhobosVehicleSystems_LostConvoyLabel".Translate(Vehicles.Select(v => v.LabelShort).ToCommaList(useAnd: true).Named("VEHICLES")).Resolve();
        }
    }

    /// <summary>Puts a dropped vehicle on the world map as a wreck at this tile.</summary>
    public static PhobosSlingWreck Make(PlanetTile tile, VehiclePawn load, float metres) =>
        Make(tile, new[] { load }, metres, PhobosSlingWreckDefOf.PhobosSlingWreck);

    /// <summary>Puts vehicles on the world map at this tile, as a wreck or a lost convoy by the Def (Framework 0.187.0).</summary>
    public static PhobosSlingWreck Make(PlanetTile tile, IEnumerable<VehiclePawn> vehicles, float metres, WorldObjectDef def)
    {
        var wreck = (PhobosSlingWreck)WorldObjectMaker.MakeWorldObject(def);
        wreck.Tile = tile;
        wreck.fallMetres = metres;
        foreach (VehiclePawn vehicle in vehicles)
        {
            wreck.held.TryAdd(vehicle, canMergeWithExistingStacks: false);
            // Its regular driver feels the loss, and it is welcomed when brought home (Framework 0.190.0, EM12).
            OwnVehicles.Lost(vehicle, destroyed: false);
        }
        Find.WorldObjects.Add(wreck);
        return wreck;
    }

    /// <summary>A new, empty depot on this tile (Framework 0.199.0, DP1). Depots.Exchange fills it.</summary>
    public static PhobosSlingWreck MakeDepot(PlanetTile tile)
    {
        var depot = (PhobosSlingWreck)WorldObjectMaker.MakeWorldObject(PhobosSlingWreckDefOf.PhobosDepot);
        depot.Tile = tile;
        depot.placeWords = Phobos.Framework.WorldMap.WorldPlaces.Describe(tile);
        Find.WorldObjects.Add(depot);
        return depot;
    }

    /// <summary>Puts a vehicle or an item into the depot to wait (Framework 0.199.0).</summary>
    public void Keep(Thing thing)
    {
        thing.holdingOwner?.Remove(thing);
        if (!held.TryAdd(thing, canMergeWithExistingStacks: thing is not Pawn)) VehicleSystemsMod.Logger.Warn("The depot could not take " + thing.LabelShort + ".");
    }

    /// <summary>A depot with nothing left in it, and no map open, goes (Framework 0.199.0).</summary>
    public void RemoveIfEmpty()
    {
        if (!HasMap && held.Count == 0 && !Destroyed) Destroy();
    }

    /// <summary>The nearest tile to this one, itself first, where a wreck can lie: open ground with no other place on it.</summary>
    public static PlanetTile FreeTileNear(PlanetTile tile)
    {
        if (!tile.Valid) return PlanetTile.Invalid;
        if (Free(tile)) return tile;
        var neighbours = new List<PlanetTile>();
        Find.WorldGrid.GetTileNeighbors(tile, neighbours);
        foreach (PlanetTile near in neighbours) if (Free(near)) return near;
        return PlanetTile.Invalid;
    }

    /// <summary>
    /// The tile under a point on the globe, found by walking from a nearby tile to whichever neighbour is closer until none is (an aircraft's
    /// own tile can be where its leg of the flight began). Vehicle Framework's own finder reads every tile on the planet, and is not used.
    /// </summary>
    public static PlanetTile TileUnder(PlanetTile start, UnityEngine.Vector3 point)
    {
        if (!start.Valid) return start;
        WorldGrid grid = Find.WorldGrid;
        var neighbours = new List<PlanetTile>();
        PlanetTile best = start;
        float bestDistance = (grid.GetTileCenter(best) - point).sqrMagnitude;
        for (int step = 0; step < MaxSteps; step++)
        {
            neighbours.Clear();
            grid.GetTileNeighbors(best, neighbours);
            PlanetTile next = best;
            foreach (PlanetTile near in neighbours)
            {
                float distance = (grid.GetTileCenter(near) - point).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    next = near;
                }
            }
            if (next == best) break;
            best = next;
        }
        return best;
    }

    /// <summary>The most tiles the walk takes: more than any one leg of a flight crosses.</summary>
    const int MaxSteps = 2000;

    static bool Free(PlanetTile tile) => tile.Valid && !Find.World.Impassable(tile) && !Find.WorldObjects.AnyWorldObjectAt(tile);

    public override string GetInspectString()
    {
        string text = base.GetInspectString();
        if (IsDepot)
        {
            string summary = Depots.Summary(this);
            return text.NullOrEmpty() ? summary : text + "\n" + summary;
        }
        if (Wreck is not { } wreck) return text;
        string line = LostConvoy
            ? "PhobosVehicleSystems_LostConvoyInspect".Translate(Vehicles.Count().Named("COUNT"), Vehicles.Sum(v => v.AllPawnsAboard.Count).Named("PEOPLE")).Resolve()
            : "PhobosSling_WreckInspect".Translate(wreck.LabelShort.Named("LOAD"), wreck.AllPawnsAboard.Count().Named("PEOPLE")).Resolve();
        return text.NullOrEmpty() ? line : text + "\n" + line;
    }

    public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
    {
        foreach (FloatMenuOption option in base.GetFloatMenuOptions(caravan)) yield return option;
        if (HasMap || (Wreck == null && held.Count == 0)) yield break;
        // Framework 0.199.0: a depot is visited, on its map, to drive its vehicles away and pick up what was left (DP6).
        string approach = (IsDepot ? "PhobosDepot_Visit" : "PhobosSling_WreckApproach").Translate(Label.Named("PLACE")).Resolve();
        foreach (FloatMenuOption option in CaravanArrivalActionUtility.GetFloatMenuOptions(() => true,
                     () => new CaravanArrivalAction_PhobosRecoverWreck(this), approach, caravan, Tile, this))
        {
            yield return option;
        }
    }

    /// <summary>A caravan has reached the wreck: the map is made, the wreck put on it and given its fall, and the caravan enters.</summary>
    public void Enter(Caravan caravan)
    {
        bool made = !HasMap;
        int size = Rules.slingWreckMapSize;
        Map map = GetOrGenerateMapUtility.GetOrGenerateMap(Tile, new IntVec3(size, 1, size), def);
        if (made && held.Count > 0) PlaceOn(map);
        CaravanEnterMapUtility.Enter(caravan, map, CaravanEnterMode.Edge, CaravanDropInventoryMode.DoNotDrop, draftColonists: false);
        if (IsDepot)
        {
            Messages.Message("PhobosDepot_Reached".Translate(caravan.Label.Named("CARAVAN")).Resolve(), MessageTypeDefOf.NeutralEvent);
            if (onMap.FirstOrDefault(v => v != null && v.Spawned) is { } vehicle) Seen.Note("vehicles.depot", vehicle, variant: "visited");
        }
        else if (onMap.FirstOrDefault(v => v != null && v.Spawned) is { } first)
        {
            Messages.Message("PhobosSling_WreckReached".Translate(caravan.Label.Named("CARAVAN"), first.LabelShort.Named("LOAD")).Resolve(), first, MessageTypeDefOf.NeutralEvent);
            if (LostConvoy) Seen.Note("vehicles.lost-convoy-reached", first);
        }
    }

    void PlaceOn(Map map)
    {
        foreach (Thing thing in held.InnerListForReading.ToList())
        {
            if (thing is not VehiclePawn load)
            {
                // A depot's items (Framework 0.199.0): put down near the middle, where the caravan comes in to pick them up.
                held.Remove(thing);
                if (GenPlace.TryPlaceThing(thing, map.Center, map, ThingPlaceMode.Near)) onMapItems.Add(thing);
                else held.TryAdd(thing, canMergeWithExistingStacks: true);
                continue;
            }
            if (!SlingPlacement.TryFindClear(load.VehicleDef, Rot4.North, map, map.Center, map.Size.x / 3f, CellRect.Empty, out IntVec3 cell)
                && !CellFinder.TryFindRandomCellNear(map.Center, map, map.Size.x / 2, c => SlingPlacement.Fits(load.VehicleDef, Rot4.North, map, c, CellRect.Empty), out cell))
            {
                cell = map.Center;
            }
            held.Remove(load);
            GenSpawn.Spawn(load, cell, map, Rot4.Random);
            onMap.Add(load);
            if (fallMetres > 0f) SlingDrops.Fall(load, fallMetres);
        }
        fallMetres = 0f;
    }

    /// <summary>The map goes once nobody of ours is on it. A wreck still lying there is taken back to wait; one driven, towed or lifted away
    /// (or destroyed) leaves nothing, and the place goes with the map.</summary>
    public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject)
    {
        alsoRemoveWorldObject = false;
        if (!HasMap || Map.mapPawns.AnyPawnBlockingMapRemoval) return false;
        foreach (VehiclePawn left in onMap)
        {
            if (left == null || left.Destroyed || !left.Spawned || left.Map != Map || left.AllPawnsAboard.Any(p => p.IsColonist)) continue;
            left.DeSpawn();
            held.TryAdd(left, canMergeWithExistingStacks: false);
        }
        onMap.Clear();
        // Framework 0.199.0: a depot's items still lying on its map wait for the next visit, and so does anything the caravan put down there.
        foreach (Thing item in onMapItems)
        {
            if (item == null || item.Destroyed || !item.Spawned || item.Map != Map) continue;
            item.DeSpawn();
            held.TryAdd(item, canMergeWithExistingStacks: true);
        }
        onMapItems.Clear();
        if (IsDepot)
        {
            foreach (Thing item in Map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableEver).ToList())
            {
                if (item.Destroyed || !item.Spawned || item is Pawn || item.def.category != ThingCategory.Item || item.def.thingCategories?.Contains(ThingCategoryDefOf.StoneChunks) == true) continue;
                item.DeSpawn();
                held.TryAdd(item, canMergeWithExistingStacks: true);
            }
        }
        alsoRemoveWorldObject = held.Count == 0;
        return true;
    }

    // MapParent holds nothing itself (its GetDirectlyHeldThings is not virtual and returns null); the wreck re-implements the interface so
    // the game finds the vehicle and its riders here while they wait.
    public new ThingOwner GetDirectlyHeldThings() => held;

    public override void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, held);
        if (HasMap) outChildren.Add(Map);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref held, "PhobosSlingWreck_Held", this);
        // Framework 0.187.0: every vehicle lying on the site's map. A save from before held one, under the old key, read once into the list.
        Scribe_Collections.Look(ref onMap, "PhobosSlingWreck_OnMapAll", LookMode.Reference);
        if (Scribe.mode != LoadSaveMode.Saving)
        {
            VehiclePawn? single = null;
            Scribe_References.Look(ref single, "PhobosSlingWreck_OnMap");
            onMap ??= new List<VehiclePawn>();
            if (single != null && !onMap.Contains(single)) onMap.Add(single);
        }
        Scribe_Values.Look(ref fallMetres, "PhobosSlingWreck_FallMetres");
        Scribe_Collections.Look(ref onMapItems, "PhobosSlingWreck_OnMapItems", LookMode.Reference);
        Scribe_Values.Look(ref placeWords, "PhobosSlingWreck_PlaceWords", "");
        Scribe_Values.Look(ref fuelClock, "PhobosDepot_FuelClock", -1);
        Scribe_Values.Look(ref foundAtTick, "PhobosDepot_FoundAt", -1);
        Scribe_References.Look(ref foundBy, "PhobosDepot_FoundBy");
        Scribe_Values.Look(ref leakCarry, "PhobosDepot_LeakCarry");
        Scribe_Collections.Look(ref staleCarry, "PhobosDepot_StaleCarry", LookMode.Value, LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            held ??= new ThingOwner<Thing>(this, oneStackOnly: false);
            onMap ??= new List<VehiclePawn>();
            onMap.RemoveAll(v => v == null);
            onMapItems ??= new List<Thing>();
            onMapItems.RemoveAll(t => t == null);
            placeWords ??= "";
            staleCarry ??= new Dictionary<string, double>();
        }
    }
}

/// <summary>A caravan's order to go to a sling wreck and enter its map.</summary>
public class CaravanArrivalAction_PhobosRecoverWreck : CaravanArrivalAction
{
    PhobosSlingWreck? wreck;

    public CaravanArrivalAction_PhobosRecoverWreck() { }

    public CaravanArrivalAction_PhobosRecoverWreck(PhobosSlingWreck wreck) => this.wreck = wreck;

    public override string Label => "PhobosSling_WreckApproach".Translate((wreck?.Label ?? string.Empty).Named("PLACE")).Resolve();

    public override string ReportString => "PhobosSling_WreckApproaching".Translate((wreck?.Label ?? string.Empty).Named("PLACE")).Resolve();

    public override FloatMenuAcceptanceReport StillValid(Caravan caravan, PlanetTile destinationTile)
    {
        FloatMenuAcceptanceReport report = base.StillValid(caravan, destinationTile);
        if (!report) return report;
        return wreck != null && !wreck.Destroyed && wreck.Tile == destinationTile;
    }

    public override void Arrived(Caravan caravan)
    {
        if (wreck == null || wreck.Destroyed) return;
        PhobosSlingWreck target = wreck;
        if (target.HasMap) target.Enter(caravan);
        else LongEventHandler.QueueLongEvent(() => target.Enter(caravan), "GeneratingMapForNewEncounter", doAsynchronously: false, null);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref wreck, "PhobosSlingWreck");
    }
}

[DefOf]
public static class PhobosSlingWreckDefOf
{
    public static WorldObjectDef PhobosSlingWreck = null!;
    public static WorldObjectDef PhobosLostConvoy = null!;
    public static WorldObjectDef PhobosDepot = null!;

    static PhobosSlingWreckDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(PhobosSlingWreckDefOf));
}
