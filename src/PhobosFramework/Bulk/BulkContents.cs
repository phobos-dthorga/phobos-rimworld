using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using RimWorld;
using Verse;

namespace Phobos.Framework.Bulk;

/// <summary>One product a bulk hold carries: how many units, and how much of its keeping time is used (nought fresh, one spoiled).</summary>
public class BulkEntry : IExposable
{
    public ThingDef? def;
    public int count;
    public float used;

    public float Kilograms => def == null ? 0f : count * def.BaseMass;

    public void ExposeData()
    {
        Scribe_Defs.Look(ref def, "def");
        Scribe_Values.Look(ref count, "count");
        Scribe_Values.Look(ref used, "used");
    }
}

/// <summary>
/// What a bulk hold carries, as amounts by product, never stacks (Framework 0.182.0; docs/development/farming-batch-one.md, "The bulk
/// grain hold"): a grain tank, a chaser bin, a field bin, a silo. Freshness is one figure a product, mixed by amount as loads join and aged
/// lazily from the time passed when it is read, at the temperature then (a simplification: the hold does not remember the temperatures in
/// between). BulkHoldRule decides; this keeps the amounts and saves them.
/// </summary>
public class BulkContents : IExposable
{
    List<BulkEntry> entries = new();
    int agedTick = -1;

    public IReadOnlyList<BulkEntry> Entries => entries;

    public bool Empty => entries.Count == 0;

    public float Kilograms
    {
        get
        {
            float total = 0f;
            for (int i = 0; i < entries.Count; i++) total += entries[i].Kilograms;
            return total;
        }
    }

    public int CountOf(ThingDef def) => Entry(def)?.count ?? 0;

    /// <summary>The products it holds, in the player's words: "1,240 wheat, 300 hay".</summary>
    public string Summary => entries.Count == 0
        ? ""
        : entries.Select(e => e.count.ToString("N0") + " " + (e.def?.label ?? "?")).ToCommaList();

    BulkEntry? Entry(ThingDef def)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].def == def) return entries[i];
        }
        return null;
    }

    /// <summary>Whether it would take this product: a hold that mixes takes any; one that keeps a single product takes it empty or alike.</summary>
    public bool Takes(ThingDef def, bool mixesProducts) => BulkHoldRule.Takes(mixesProducts, Empty, Entry(def) != null);

    /// <summary>How many more units of this product fit by weight.</summary>
    public int RoomFor(ThingDef def, float capacityKg) => BulkHoldRule.RoomFor(capacityKg, Kilograms, def.BaseMass);

    /// <summary>Adds units with their used share, mixing the share with what is there. Returns how many went in.</summary>
    public int Add(ThingDef def, int count, float used, float capacityKg, bool mixesProducts)
    {
        if (count <= 0 || !Takes(def, mixesProducts)) return 0;
        int fits = System.Math.Min(count, RoomFor(def, capacityKg));
        if (fits <= 0) return 0;
        BulkEntry? entry = Entry(def);
        if (entry == null)
        {
            entry = new BulkEntry { def = def };
            entries.Add(entry);
        }
        entry.used = BulkHoldRule.Mixed(entry.count, entry.used, fits, used);
        entry.count += fits;
        return fits;
    }

    /// <summary>Takes up to this many units of a product out. Returns how many came out and their used share.</summary>
    public int Take(ThingDef def, int count, out float used)
    {
        used = 0f;
        BulkEntry? entry = Entry(def);
        if (entry == null || count <= 0) return 0;
        int taken = System.Math.Min(count, entry.count);
        used = entry.used;
        entry.count -= taken;
        if (entry.count <= 0) entries.Remove(entry);
        return taken;
    }

    /// <summary>
    /// Ages every product by the time since it was last aged, at this temperature and the store's factor (an aired silo below one).
    /// Products that reach spoiled are thrown away; their labels are returned, for the hold to say so.
    /// </summary>
    public List<string> Age(int now, float temperature, float storeFactor)
    {
        var spoiled = new List<string>();
        // Nothing held: nothing to age, and the clock starts at now for whatever comes in next.
        if (agedTick < 0 || now <= agedTick || entries.Count == 0)
        {
            agedTick = now;
            return spoiled;
        }
        int ticks = now - agedTick;
        agedTick = now;
        float rate = GenTemperature.RotRateAtTemperature(temperature);
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            BulkEntry entry = entries[i];
            int keeping = KeepingTicks(entry.def);
            entry.used = BulkHoldRule.Aged(entry.used, ticks, rate, keeping, storeFactor);
            if (keeping > 0 && entry.used >= 1f)
            {
                spoiled.Add(entry.def?.label ?? "?");
                entries.RemoveAt(i);
            }
        }
        return spoiled;
    }

    /// <summary>How long a product keeps before it starts to rot, in ticks; nought for what never rots.</summary>
    public static int KeepingTicks(ThingDef? def) => def?.GetCompProperties<CompProperties_Rottable>() is { } rot ? rot.TicksToRotStart : 0;

    /// <summary>Makes a stack of up to a stack's worth of a product as items, with its freshness carried over, or null for none.</summary>
    public Thing? MakeStack(ThingDef def, int most)
    {
        int count = Take(def, System.Math.Min(most, def.stackLimit), out float used);
        if (count <= 0) return null;
        Thing thing = ThingMaker.MakeThing(def);
        thing.stackCount = count;
        if (thing.TryGetComp<CompRottable>() is { } rot && KeepingTicks(def) > 0) rot.RotProgress = used * KeepingTicks(def);
        return thing;
    }

    /// <summary>Puts a stack it made back, as when the ground had no room for it: whatever the capacity, as it came from here.</summary>
    public void Return(Thing thing)
    {
        float used = thing.TryGetComp<CompRottable>() is { } rot && KeepingTicks(thing.def) > 0 ? rot.RotProgress / KeepingTicks(thing.def) : 0f;
        Add(thing.def, thing.stackCount, used, float.MaxValue, mixesProducts: true);
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref entries, "entries", LookMode.Deep);
        Scribe_Values.Look(ref agedTick, "agedTick", -1);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            entries ??= new List<BulkEntry>();
            entries.RemoveAll(e => e.def == null || e.count <= 0);
        }
    }
}
