using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A cargo net (Framework 0.335.0; docs/development/vehicle-emergent-play.md, EM21(b); the owner's card of 27 Nov 2026, "Also a cargo net of
/// many stacks"): a net laid on the ground as a small stockpile, which your haulers fill by its storage settings as they fill a shelf. A
/// helicopter's sling lifts it with everything lying in it (<see cref="CompPhobosSling"/>): the net takes them up as it leaves the ground
/// and lets them out where it is set down. Its weight is its own and all it holds. Every rule is here; the sling only asks.
/// </summary>
public class Building_PhobosCargoNet : Building_Storage, IThingHolder
{
    ThingOwner<Thing> netted;

    public Building_PhobosCargoNet() => netted = new ThingOwner<Thing>(this, oneStackOnly: false);

    /// <summary>What the net holds while it hangs from a hook; empty while it lies on the ground, where its load lies in its cells.</summary>
    public ThingOwner<Thing> Netted => netted;

    /// <summary>What lies in the net now: its cells' items on the ground, or what it holds in the air.</summary>
    public IEnumerable<Thing> Contents => Spawned ? OnCells() : netted;

    IEnumerable<Thing> OnCells()
    {
        foreach (IntVec3 cell in this.OccupiedRect())
        {
            foreach (Thing thing in cell.GetThingList(Map))
                if (thing != this && thing.def.category == ThingCategory.Item) yield return thing;
        }
    }

    /// <summary>The net's weight with its load, in kilograms: its own and every piece it holds.</summary>
    public double Kilograms => this.GetStatValue(StatDefOf.Mass) + Contents.Sum(t => (double)t.GetStatValue(StatDefOf.Mass) * t.stackCount);

    /// <summary>As the hook takes it up: everything lying in its cells goes up in it.</summary>
    public void Gather()
    {
        foreach (Thing thing in OnCells().ToList())
        {
            thing.DeSpawn();
            if (!netted.TryAdd(thing, canMergeWithExistingStacks: false)) GenSpawn.Spawn(thing, Position, Map);
        }
    }

    /// <summary>As it is set down: what it holds is let out onto its cells, or beside them where they are full.</summary>
    public void Spill()
    {
        if (!Spawned) return;
        foreach (Thing thing in netted.ToList())
        {
            netted.Remove(thing);
            if (!GenPlace.TryPlaceThing(thing, Position, Map, ThingPlaceMode.Near)) GenSpawn.Spawn(thing, Position, Map);
        }
    }

    public override string GetInspectString()
    {
        string text = base.GetInspectString();
        int pieces = Contents.Count();
        string line = "PhobosSling_NetInspect".Translate(pieces.Named("COUNT"), Kilograms.ToString("N0").Named("WEIGHT")).Resolve();
        return text.NullOrEmpty() ? line : text + "\n" + line;
    }

    public ThingOwner GetDirectlyHeldThings() => netted;

    public void GetChildHolders(List<IThingHolder> outChildren) => ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref netted, "PhobosCargoNet_Netted", this, false);
        netted ??= new ThingOwner<Thing>(this, oneStackOnly: false);
    }
}
