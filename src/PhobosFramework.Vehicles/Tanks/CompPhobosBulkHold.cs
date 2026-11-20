using System.Collections.Generic;
using Phobos.Framework.Bulk;
using Phobos.Framework.Interface;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>An upgrade that changes a bulk hold's figure (its capacity, or its auger's rate): while its node is unlocked, the figure is multiplied by its factor.</summary>
public class PhobosBulkCapacityUpgrade
{
    public string node = "";
    public float factor = 1f;
}

/// <summary>A vehicle's bulk hold, configured in its Def (Framework 0.182.0): a combine's grain tank, a chaser bin.</summary>
public class CompProperties_PhobosBulkHold : VehicleCompProperties
{
    /// <summary>How much it holds, kilograms, before upgrades. Authored.</summary>
    public float capacityKg = 9000f;
    /// <summary>Whether it takes several products at once: a combine's tank keeps to one, a chaser bin need not.</summary>
    public bool mixesProducts;
    /// <summary>How fast its unloading auger pours, kilograms a second. Authored.</summary>
    public float pourKgPerSecond = 100f;
    /// <summary>How far its auger reaches beyond its footprint, cells.</summary>
    public int reachCells = 2;
    /// <summary>Upgrades that enlarge it (the combine's tank extension).</summary>
    public List<PhobosBulkCapacityUpgrade> capacityUpgrades = new();
    /// <summary>Upgrades that make its auger pour faster (the chaser bin's fast auger): the rate is multiplied by each factor fitted.</summary>
    public List<PhobosBulkCapacityUpgrade> pourUpgrades = new();

    public CompProperties_PhobosBulkHold() => compClass = typeof(CompPhobosBulkHold);
}

/// <summary>
/// A vehicle that carries loose goods in bulk (Framework 0.182.0; docs/development/farming-batch-one.md, "The bulk grain hold"): a combine's
/// grain tank, a chaser bin. Amounts by product, never Vehicle Framework's cargo, whose every change works the vehicle's stats out again; its
/// weight loads the engine and the tow as cargo would (IVehicleCarriedLoad). It pours only while standing, into another hold in reach or
/// onto the ground (BulkPour).
/// </summary>
public class CompPhobosBulkHold : VehicleComp, IBulkHold, IVehicleCarriedLoad
{
    BulkContents contents = new();
    BulkPouring pouring = new();

    public CompProperties_PhobosBulkHold Props => (CompProperties_PhobosBulkHold)props;

    public ThingWithComps HoldThing => Vehicle;
    public BulkContents Contents => contents;
    public bool MixesProducts => Props.mixesProducts;
    public float PourKgPerSecond
    {
        get
        {
            float rate = Props.pourKgPerSecond;
            foreach (PhobosBulkCapacityUpgrade upgrade in Props.pourUpgrades)
            {
                if (VehicleUpgrades.Unlocked(Vehicle, upgrade.node)) rate *= upgrade.factor;
            }
            return rate;
        }
    }
    public int ReachCells => Props.reachCells;
    public float StoreFactor => 1f;
    public float CarriedKilograms => contents.Kilograms;

    /// <summary>Its capacity with the upgrades fitted now.</summary>
    public float CapacityKg
    {
        get
        {
            float capacity = Props.capacityKg;
            foreach (PhobosBulkCapacityUpgrade upgrade in Props.capacityUpgrades)
            {
                if (VehicleUpgrades.Unlocked(Vehicle, upgrade.node)) capacity *= upgrade.factor;
            }
            return capacity;
        }
    }

    /// <summary>Whether it is pouring now: a harvester stops filling, and a run waits.</summary>
    public bool Pouring => pouring.active;

    /// <summary>Starts pouring into another hold, as the player's Pour button does; a field run uses it at a bin. Says why not if it cannot.</summary>
    public void PourInto(IBulkHold to) => BulkPour.Start(this, pouring, to);

    /// <summary>Whether it has room for more of what it holds (or anything, empty).</summary>
    public bool HasRoom => contents.Empty || System.Linq.Enumerable.Any(contents.Entries, e => e.def != null && contents.RoomFor(e.def, CapacityKg) > 0);

    public string? CannotPour()
    {
        NamedArgument name = Vehicle.LabelShort.Named("VEHICLE");
        if (!Vehicle.Spawned) return "PhobosBulk_NotOnMap".Translate(name).Resolve();
        if (Vehicle.vehiclePather is { Moving: true }) return "PhobosBulk_StandStill".Translate(name).Resolve();
        // Towed, its own wheels never "move": the hitch sets it down cell by cell behind the vehicle that drives (the fault-read of 21
        // Nov 2026, B4: a chaser bin poured, and laid a trail of heaps, while its tractor drove).
        if (CompPhobosHitch.HeadOf(Vehicle) is { } head && head != Vehicle && head.vehiclePather is { Moving: true })
        {
            return "PhobosBulk_StandStill".Translate(name).Resolve();
        }
        return null;
    }

    public string? CannotReceive() => CannotPour();

    public void ContentsChanged() => CarriedLoads.Changed(Vehicle);

    /// <summary>
    /// Takes what a harvester gathered into the hold. Returns how many went in; the rest did not fit or is another product than a
    /// single-product tank holds.
    /// </summary>
    public int Receive(ThingDef def, int count, float used = 0f)
    {
        BulkPour.Age(this);
        int added = contents.Add(def, count, used, CapacityKg, MixesProducts);
        if (added > 0) ContentsChanged();
        return added;
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        BulkPour.Register(this, Vehicle.Map);
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        BulkPour.Unregister(this, map);
        // Leaving the map ends a pour where it stands.
        if (pouring.active) BulkPour.LeftMap(this, pouring);
        base.PostDeSpawn(map, mode);
    }

    /// <summary>Destroyed with goods aboard, it spills them where it stood and names what was lost, as a bin or a silo does.</summary>
    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
        base.PostDestroy(mode, previousMap);
        if (previousMap != null) BulkPour.Spill(this, Vehicle.Position, previousMap);
    }

    public override void CompTickRare()
    {
        base.CompTickRare();
        if (!Vehicle.Spawned || contents.Empty) return;
        // Aged at every rare tick, pouring or not (the fault-read of 21 Nov 2026, B1).
        BulkPour.Age(this);
        if (pouring.active) BulkPour.Step(this, pouring);
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        if (Vehicle.Faction != Faction.OfPlayer) yield break;
        foreach (Gizmo gizmo in BulkPour.Gizmos(this, pouring)) yield return gizmo;
    }

    public override string? CompInspectStringExtra()
    {
        if (UiGuard.Resting(GuardKey)) return UiGuard.FallbackText;
        try
        {
            BulkPour.Age(this);
            return BulkPour.InspectLine(this, pouring);
        }
        catch (System.Exception e) { UiGuard.Report(GuardKey, e); return null; }
    }

    const string GuardKey = "A vehicle's bulk hold line";

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
