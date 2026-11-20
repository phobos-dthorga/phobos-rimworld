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
/// ground beside it. Its contents age at every rare tick, at the temperature and the airing then (Framework 0.202.0), and a pour under
/// way steps then too; nothing runs between.
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

    /// <summary>Destroyed or deconstructed, it spills what it held onto the ground, as many heaps as will go, and says what was lost.</summary>
    public override void PostDestroy(DestroyMode mode, Map? previousMap)
    {
        base.PostDestroy(mode, previousMap);
        if (previousMap != null) BulkPour.Spill(this, parent.Position, previousMap);
    }

    // Whether the warning has been given for the deconstruction order now standing on it.
    bool warnedOfDeconstruction;

    public override void CompTickRare()
    {
        base.CompTickRare();
        if (contents.Empty)
        {
            warnedOfDeconstruction = false;
            return;
        }
        // Aged at every rare tick, pouring or not (the fault-read of 21 Nov 2026, B1).
        BulkPour.Age(this);
        WarnOfDeconstruction();
        if (pouring.active) BulkPour.Step(this, pouring);
    }

    /// <summary>
    /// A store marked for deconstruction while it holds goods says, once for each order, how little of them would be saved (the owner's
    /// choice of 21 Nov 2026: a warning beforehand, where there had been only the loss named afterwards).
    /// </summary>
    void WarnOfDeconstruction()
    {
        bool marked = parent.Spawned && parent.Map.designationManager.DesignationOn(parent, DesignationDefOf.Deconstruct) != null;
        if (!marked)
        {
            warnedOfDeconstruction = false;
            return;
        }
        if (warnedOfDeconstruction || parent.Faction != Faction.OfPlayer) return;
        warnedOfDeconstruction = true;
        Messages.Message("PhobosBulk_DeconstructWarning".Translate(parent.LabelShort.Named("HOLD"), contents.Summary.Named("WHAT"),
            BulkPour.MostSpilledStacks.Named("HEAPS")).Resolve(), parent, MessageTypeDefOf.CautionInput);
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
