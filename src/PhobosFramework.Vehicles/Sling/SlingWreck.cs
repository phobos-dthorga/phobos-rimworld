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
}

/// <summary>
/// A sling load let go over the world map (Framework 0.101.0; owner, 12 Nov 2026: a wreck a caravan can recover). The vehicle lies held here,
/// with whoever rode in it, until a caravan reaches the tile; then a small map is made, the wreck is put on it and takes the fall's damage,
/// and the caravan enters. When nobody of ours is left there, the map goes: a wreck still lying on it is taken back to wait for the next
/// visit, and a wreck that has been driven, towed or lifted away leaves nothing behind.
/// </summary>
public class PhobosSlingWreck : MapParent, IThingHolder
{
    ThingOwner<VehiclePawn> held;
    VehiclePawn? onMap;
    float fallMetres;

    public PhobosSlingWreck() => held = new ThingOwner<VehiclePawn>(this, oneStackOnly: true);

    static PhobosVehicleRulesDef Rules => PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();

    /// <summary>The vehicle waiting here, or lying on the map made for it.</summary>
    public VehiclePawn? Wreck => held.Count > 0 ? held[0] : onMap;

    public override string Label => Wreck is { } wreck ? "PhobosSling_WreckLabel".Translate(wreck.LabelShort.Named("LOAD")).Resolve() : base.Label;

    /// <summary>Puts a dropped vehicle on the world map as a wreck at this tile.</summary>
    public static PhobosSlingWreck Make(PlanetTile tile, VehiclePawn load, float metres)
    {
        var wreck = (PhobosSlingWreck)WorldObjectMaker.MakeWorldObject(PhobosSlingWreckDefOf.PhobosSlingWreck);
        wreck.Tile = tile;
        wreck.fallMetres = metres;
        wreck.held.TryAdd(load, canMergeWithExistingStacks: false);
        Find.WorldObjects.Add(wreck);
        return wreck;
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
        if (Wreck is not { } wreck) return text;
        string line = "PhobosSling_WreckInspect".Translate(wreck.LabelShort.Named("LOAD"), wreck.AllPawnsAboard.Count().Named("PEOPLE")).Resolve();
        return text.NullOrEmpty() ? line : text + "\n" + line;
    }

    public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
    {
        foreach (FloatMenuOption option in base.GetFloatMenuOptions(caravan)) yield return option;
        if (HasMap || Wreck == null) yield break;
        foreach (FloatMenuOption option in CaravanArrivalActionUtility.GetFloatMenuOptions(() => true,
                     () => new CaravanArrivalAction_PhobosRecoverWreck(this), "PhobosSling_WreckApproach".Translate(Label.Named("PLACE")).Resolve(), caravan, Tile, this))
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
        if (onMap != null)
        {
            Messages.Message("PhobosSling_WreckReached".Translate(caravan.Label.Named("CARAVAN"), onMap.LabelShort.Named("LOAD")).Resolve(), onMap, MessageTypeDefOf.NeutralEvent);
        }
    }

    void PlaceOn(Map map)
    {
        VehiclePawn load = held[0];
        if (!SlingPlacement.TryFindClear(load.VehicleDef, Rot4.North, map, map.Center, map.Size.x / 3f, CellRect.Empty, out IntVec3 cell)
            && !CellFinder.TryFindRandomCellNear(map.Center, map, map.Size.x / 2, c => SlingPlacement.Fits(load.VehicleDef, Rot4.North, map, c, CellRect.Empty), out cell))
        {
            cell = map.Center;
        }
        held.Remove(load);
        GenSpawn.Spawn(load, cell, map, Rot4.Random);
        onMap = load;
        if (fallMetres > 0f)
        {
            SlingDrops.Fall(load, fallMetres);
            fallMetres = 0f;
        }
    }

    /// <summary>The map goes once nobody of ours is on it. A wreck still lying there is taken back to wait; one driven, towed or lifted away
    /// (or destroyed) leaves nothing, and the place goes with the map.</summary>
    public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject)
    {
        alsoRemoveWorldObject = false;
        if (!HasMap || Map.mapPawns.AnyPawnBlockingMapRemoval) return false;
        if (onMap != null && !onMap.Destroyed && onMap.Spawned && onMap.Map == Map && !onMap.AllPawnsAboard.Any(p => p.IsColonist))
        {
            onMap.DeSpawn();
            held.TryAdd(onMap, canMergeWithExistingStacks: false);
        }
        onMap = null;
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
        Scribe_References.Look(ref onMap, "PhobosSlingWreck_OnMap");
        Scribe_Values.Look(ref fallMetres, "PhobosSlingWreck_FallMetres");
        if (Scribe.mode == LoadSaveMode.PostLoadInit) held ??= new ThingOwner<VehiclePawn>(this, oneStackOnly: true);
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

    static PhobosSlingWreckDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(PhobosSlingWreckDefOf));
}
