using System.Collections.Generic;
using System.Linq;
using Phobos.Framework;
using Phobos.Framework.Items;
using Phobos.Framework.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>Gives a vehicle its work store, where haulers lay out the supplies for its services, repairs and fittings, apart from its
/// cargo. D'thorga Framework adds it to every vehicle with a loadout at start-up; a modder never needs to add it in XML.</summary>
public class CompProperties_PhobosWorkStore : CompProperties
{
    public CompProperties_PhobosWorkStore() => compClass = typeof(CompPhobosWorkStore);
}

/// <summary>
/// What haulers have brought for a vehicle's own work, laid out beside it (Framework 0.272.0; the owner's report of 23 Nov 2026: an A-10
/// with 75 kg of cargo room took its two 35 kg engines for an uprated pair and had no room for the plasteel and components). Supplies for
/// a service, a repair or a fitting were put in the cargo, where a small hold, Vehicle Framework's loading rules and other authors' cargo
/// checks all stood in the way. Vehicle Framework keeps its own upgrades' materials in a store of their own for the same reason
/// (CompUpgradeTree.upgradeContainer); this is ours, as the countermeasure magazine is. It takes no cargo room and adds no weight.
///
/// The work reads this store first and then the cargo (<see cref="CompPhobosVehicleSystems.CountInCargo"/>, <c>TakeFromCargo</c>), so
/// supplies an older save carried in the hold still count. It travels with the vehicle, as Vehicle Framework's does. Once the vehicle has
/// no work pending at all (an order called off, or done with something to spare), what is left is set down beside it.
/// </summary>
public sealed class CompPhobosWorkStore : VehicleComp, IThingHolder
{
    ThingOwner<Thing> held;

    public CompPhobosWorkStore() => held = new ThingOwner<Thing>(this);

    /// <summary>The things laid out, for counting and using.</summary>
    public IEnumerable<Thing> Held => held.InnerListForReading;

    public bool Empty => held.Count == 0;

    /// <summary>Takes what a hauler carries into the store. Returns how many went in.</summary>
    public int Stow(Pawn hauler, Thing carried)
    {
        int moved = ThingMoves.Transfer(hauler.carryTracker.innerContainer, carried, held, carried.stackCount);
        if (moved > 0 && Seen.Wants("vehicles.work-store", Vehicle)) Seen.Note("vehicles.work-store", Vehicle, carried.def.defName + " x" + moved);
        return moved;
    }

    /// <summary>Uses up so many of a supply from the store. Returns how many were taken; the cargo makes up the rest.</summary>
    public int Take(ThingDef def, int count) => PackedThings.Take(held.InnerListForReading.ToList(), def, count);

    /// <summary>
    /// Moves out of the cargo what the waiting work still needs and the store lacks (on its rare look, on a map only): supplies an older save
    /// carried in the hold, or that were loaded by hand, come out of the cargo room they took (the owner's A-10 stood over its 75 kg with two
    /// engines and plasteel for its refit, and could barely take off). Only up to what the work asks; anything else in the hold stays.
    /// </summary>
    public void TakeOutOfCargo(CompPhobosVehicleSystems systems)
    {
        if (!Vehicle.Spawned || Vehicle.Faction != Faction.OfPlayer || Vehicle.inventory?.innerContainer is not { Count: > 0 } cargo) return;
        List<(ThingDef def, int count)> wanted = SupplyRules.Shortfall(systems.SupplyNeeds(), def => PackedThings.Count(held, def));
        int moved = 0;
        foreach ((ThingDef def, int count) in wanted)
        {
            int left = count;
            // A packed machine is a stack of one, so it counts as one here as everywhere.
            foreach (Thing thing in cargo.InnerListForReading.Where(t => PackedThings.Answers(t, def)).ToList())
            {
                if (left <= 0) break;
                // Counted truly (Framework 0.286.2): the game's own answer counts a stack merged into the store as nought, and this loop then
                // emptied the owner's HEMTT of 975 steel.
                int done = ThingMoves.Transfer(cargo, thing, held, System.Math.Min(left, thing.stackCount));
                left -= done;
                moved += done;
            }
        }
        if (moved > 0 && Seen.Wants("vehicles.work-store.from-cargo", Vehicle)) Seen.Note("vehicles.work-store.from-cargo", Vehicle, moved + " things");
    }

    /// <summary>
    /// Sets down beside the vehicle whatever the waiting work cannot use (on its rare look, on a map only): everything once no work waits,
    /// and otherwise what is beyond the work's needs (Framework 0.286.2; the owner's HEMTT of 25 Nov 2026 kept 975 steel after its fittings
    /// were called off, because one other piece of work still waited and the store was only ever emptied whole).
    /// </summary>
    public void SetDownLeftovers(CompPhobosVehicleSystems systems)
    {
        if (Empty || !Vehicle.Spawned || Vehicle.Faction != Faction.OfPlayer) return;
        List<Thing> lots = held.InnerListForReading.ToList();
        List<int> surplus = SupplyRules.Surplus(lots.Select(t => (PackedThings.CountsAs(t)!, t.stackCount)).ToList(), systems.WorkSupplyNeeds());
        int setDown = 0;
        for (int i = 0; i < lots.Count; i++)
        {
            if (surplus[i] <= 0) continue;
            int before = lots[i].stackCount;
            if (held.TryDrop(lots[i], Vehicle.Position, Vehicle.Map, ThingPlaceMode.Near, surplus[i], out Thing _)) setDown += System.Math.Min(before, surplus[i]);
        }
        if (setDown <= 0) return;
        string key = Empty ? "PhobosVehicleSystems_WorkStoreSetDown" : "PhobosVehicleSystems_WorkStoreSurplusSetDown";
        Messages.Message(key.Translate(Vehicle.LabelShort.Named("VEHICLE")), Vehicle, MessageTypeDefOf.NeutralEvent);
        if (!Empty && Seen.Wants("vehicles.work-store.surplus", Vehicle)) Seen.Note("vehicles.work-store.surplus", Vehicle, setDown + " things");
    }

    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
        base.PostDestroy(mode, previousMap);
        // Laid out beside the vehicle, not in it: what survives is left where it stood.
        if (previousMap != null && !held.NullOrEmpty()) held.TryDropAll(Vehicle.Position, previousMap, ThingPlaceMode.Near);
        else held.ClearAndDestroyContents();
    }

    public override string CompInspectStringExtra()
    {
        if (Empty || Vehicle?.Faction != Faction.OfPlayer) return "";
        // Stacks of one kind added up (Framework 0.286.2): thirteen "steel x75" in a row read as a list stuck in a loop.
        IEnumerable<string> single = held.InnerListForReading.Where(t => t.def.stackLimit <= 1).Select(t => t.LabelCap.ToString());
        IEnumerable<string> stacked = held.InnerListForReading.Where(t => t.def.stackLimit > 1).GroupBy(t => (t.def, t.Stuff))
            .Select(g => GenLabel.ThingLabel(g.Key.def, g.Key.Stuff, g.Sum(t => t.stackCount)).CapitalizeFirst());
        string things = stacked.Concat(single).ToCommaList(useAnd: true);
        return "PhobosVehicleSystems_WorkStoreInspect".Translate(things.Named("THINGS"));
    }

    // ---- Holding ----

    public ThingOwner GetDirectlyHeldThings() => held;

    public void GetChildHolders(List<IThingHolder> outChildren) => ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, held);

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Deep.Look(ref held, "PhobosWorkStore_Held", this);
        if (Scribe.mode == LoadSaveMode.PostLoadInit) held ??= new ThingOwner<Thing>(this);
    }
}
