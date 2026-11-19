using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Phobos.Framework.Bulk;

/// <summary>A building's bulk store, configured in its Def (Framework 0.182.0): a field bin, a silo.</summary>
public class CompProperties_PhobosBulkStore : CompProperties
{
    /// <summary>How much it holds, kilograms. Authored.</summary>
    public float capacityKg = 25000f;
    /// <summary>Whether it takes several products at once.</summary>
    public bool mixesProducts = true;
    /// <summary>How fast its spout or auger pours, kilograms a second. Authored.</summary>
    public float pourKgPerSecond = 60f;
    /// <summary>How far its auger reaches beyond its footprint, cells.</summary>
    public int reachCells = 1;
    /// <summary>
    /// The share of the normal spoiling rate while its power is on: air blown through grain keeps it cool and dry (an aired silo).
    /// One for a store with no fan. Authored [A].
    /// </summary>
    public float airedSpoilFactor = 1f;

    public CompProperties_PhobosBulkStore() => compClass = typeof(CompPhobosBulkStore);
}

/// <summary>
/// A building that holds loose goods in bulk (Framework 0.182.0; docs/development/farming-batch-one.md, "The bulk grain hold"): a field
/// bin, a silo. It is filled by pouring from a hold in reach (a combine's tank, a chaser bin), and pours into another hold or onto the
/// ground beside it. Its contents age only when read, so it costs nothing per tick while nothing happens; a pour under way steps once a
/// rare tick.
/// </summary>
public class CompPhobosBulkStore : ThingComp, IBulkHold
{
    BulkContents contents = new();
    BulkPouring pouring = new();

    public CompProperties_PhobosBulkStore Props => (CompProperties_PhobosBulkStore)props;

    public ThingWithComps HoldThing => parent;
    public BulkContents Contents => contents;
    public float CapacityKg => Props.capacityKg;
    public bool MixesProducts => Props.mixesProducts;
    public float PourKgPerSecond => Props.pourKgPerSecond;
    public int ReachCells => Props.reachCells;

    /// <summary>Aired while its power is on, if it has a fan.</summary>
    public float StoreFactor => Props.airedSpoilFactor < 1f && parent.TryGetComp<CompPowerTrader>() is { PowerOn: true } ? Props.airedSpoilFactor : 1f;

    public string? CannotPour() => null;

    public string? CannotReceive() => null;

    public void ContentsChanged()
    {
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        BulkPour.Register(this, parent.Map);
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        BulkPour.Unregister(this, map);
        base.PostDeSpawn(map, mode);
    }

    /// <summary>Destroyed or deconstructed, it spills what it held onto the ground, rather than losing it silently.</summary>
    public override void PostDestroy(DestroyMode mode, Map? previousMap)
    {
        base.PostDestroy(mode, previousMap);
        if (previousMap == null || contents.Empty) return;
        foreach (BulkEntry entry in new List<BulkEntry>(contents.Entries))
        {
            ThingDef def = entry.def!;
            while (contents.CountOf(def) > 0)
            {
                Thing? stack = contents.MakeStack(def, contents.CountOf(def));
                if (stack == null || !GenPlace.TryPlaceThing(stack, parent.Position, previousMap, ThingPlaceMode.Near)) break;
            }
        }
    }

    public override void CompTickRare()
    {
        base.CompTickRare();
        if (!pouring.active) return;
        BulkPour.Age(this);
        BulkPour.Step(this, pouring);
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        if (parent.Faction != Faction.OfPlayer) yield break;
        foreach (Gizmo gizmo in BulkPour.Gizmos(this, pouring)) yield return gizmo;
    }

    public override string CompInspectStringExtra()
    {
        BulkPour.Age(this);
        string line = BulkPour.InspectLine(this, pouring);
        if (Props.airedSpoilFactor < 1f)
        {
            line += "\n" + (StoreFactor < 1f ? "PhobosBulk_LineAired" : "PhobosBulk_LineNotAired").Translate().Resolve();
        }
        return line;
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Deep.Look(ref contents, "PhobosBulkContents");
        Scribe_Deep.Look(ref pouring, "PhobosBulkPouring");
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            contents ??= new BulkContents();
            pouring ??= new BulkPouring();
        }
    }
}
