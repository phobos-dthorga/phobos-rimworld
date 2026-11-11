using System;
using System.Collections.Generic;
using Phobos.Framework.Persistence;
using Phobos.Framework.Rules;
using Phobos.Framework.Vehicles.Rules;

namespace Phobos.VehicleSystems;

/// <summary>
/// The last day of each shown quantity, one sample per rare tick, for the Systems tab's graphs. Saved as one compact string per
/// quantity (the owner's exception to "derived state is never saved", 8 Nov 2026: a graph cannot be rebuilt); a few kilobytes per
/// vehicle. Quantities a system no longer has are dropped on load.
/// </summary>
public sealed class QuantityHistory
{
    public const int Hours = 24;
    public const int StrideTicks = 250;
    public static readonly int Capacity = HistoryRing.SamplesFor(Hours, StrideTicks, ElapsedRule.TicksPerHour);

    readonly Dictionary<string, HistoryRing> rings = new();
    int lastSampleTick = -1;

    /// <summary>The wear trend's span (Framework 0.86.0): fifteen days, one sample an hour, for the slow readings only (QuantitySpec.wearTrend).</summary>
    public const int WearHours = 15 * 24;
    public const int WearStrideTicks = 2500;
    public static readonly int WearCapacity = HistoryRing.SamplesFor(WearHours, WearStrideTicks, ElapsedRule.TicksPerHour);

    readonly Dictionary<string, HistoryRing> wearRings = new();
    int lastWearTick = -1;

    /// <summary>Counts every wear sample taken, so a view built from the wear rings knows when to build again.</summary>
    public int WearStamp { get; private set; }

    /// <summary>The fifteen days of a slow reading, or null before its first sample or for a reading without a trend.</summary>
    public HistoryRing? WearFor(SystemState state, int quantity) =>
        wearRings.TryGetValue(Key(state.Def, state.Def.quantities[quantity]), out HistoryRing? ring) ? ring : null;

    static string Key(PhobosVehicleSystemDef def, QuantitySpec quantity) => def.defName + "_" + quantity.name;

    /// <summary>The ring of a quantity, or null before its first sample.</summary>
    public HistoryRing? For(SystemState state, int quantity) =>
        rings.TryGetValue(Key(state.Def, state.Def.quantities[quantity]), out HistoryRing? ring) ? ring : null;

    /// <summary>One sample of every shown quantity, no more often than the stride.</summary>
    public void Sample(IReadOnlyList<SystemState> states, int now)
    {
        SampleWear(states, now);
        if (lastSampleTick >= 0 && now - lastSampleTick < StrideTicks) return;
        lastSampleTick = now;
        foreach (SystemState state in states)
        {
            for (int q = 0; q < state.values.Length; q++)
            {
                QuantitySpec quantity = state.Def.quantities[q];
                if (quantity.hidden) continue;
                Ring(state.Def, quantity).Push((float)state.values[q]);
            }
        }
    }

    /// <summary>One sample of every slow reading, no more often than an hour.</summary>
    void SampleWear(IReadOnlyList<SystemState> states, int now)
    {
        if (lastWearTick >= 0 && now - lastWearTick < WearStrideTicks) return;
        lastWearTick = now;
        foreach (SystemState state in states)
        {
            for (int q = 0; q < state.values.Length; q++)
            {
                QuantitySpec quantity = state.Def.quantities[q];
                if (!quantity.wearTrend) continue;
                WearRing(state.Def, quantity).Push((float)state.values[q]);
            }
        }
        WearStamp++;
    }

    HistoryRing WearRing(PhobosVehicleSystemDef def, QuantitySpec quantity)
    {
        string key = Key(def, quantity);
        if (!wearRings.TryGetValue(key, out HistoryRing? ring))
        {
            ring = new HistoryRing(WearCapacity);
            wearRings[key] = ring;
        }
        return ring;
    }

    HistoryRing Ring(PhobosVehicleSystemDef def, QuantitySpec quantity)
    {
        string key = Key(def, quantity);
        if (!rings.TryGetValue(key, out HistoryRing? ring))
        {
            ring = new HistoryRing(Capacity);
            rings[key] = ring;
        }
        return ring;
    }

    /// <summary>Saves and loads every shown quantity's ring of the systems this vehicle has.</summary>
    public void ExposeData(IEnumerable<ResolvedSystem> systems)
    {
        foreach (ResolvedSystem system in systems)
        {
            foreach (QuantitySpec quantity in system.def.quantities)
            {
                if (quantity.hidden) continue;
                string key = Key(system.def, quantity);
                rings.TryGetValue(key, out HistoryRing? ring);
                ScribeHistory.Look(ref ring, "PhobosVehicleSystems_History_" + key, Capacity, quantity.min, quantity.max);
                if (ring != null && ring.Count > 0) rings[key] = ring;
                if (!quantity.wearTrend) continue;
                wearRings.TryGetValue(key, out HistoryRing? wear);
                ScribeHistory.Look(ref wear, "PhobosVehicleSystems_Wear_" + key, WearCapacity, quantity.min, quantity.max);
                if (wear != null && wear.Count > 0) wearRings[key] = wear;
            }
        }
    }

    /// <summary>A day of a sine between each quantity's limits, for checking the graphs without waiting a day (a dev action).</summary>
    public void DebugFill(IReadOnlyList<SystemState> states)
    {
        foreach (SystemState state in states)
        {
            for (int q = 0; q < state.values.Length; q++)
            {
                QuantitySpec quantity = state.Def.quantities[q];
                if (quantity.hidden) continue;
                HistoryRing ring = Ring(state.Def, quantity);
                ring.Clear();
                for (int i = 0; i < Capacity; i++)
                {
                    double share = 0.5 + 0.45 * Math.Sin(i / (double)Capacity * Math.PI * 2 * (1 + q));
                    ring.Push((float)(quantity.min + (quantity.max - quantity.min) * share));
                }
                if (!quantity.wearTrend) continue;
                // Fifteen days rising from where the reading is now toward itself plus a fifth of its range, reset halfway, as a service
                // would: the trend line and its estimate show at once.
                HistoryRing wear = WearRing(state.Def, quantity);
                wear.Clear();
                // An hours count rises an hour an hour while running, so its test line is a couple of days of running, not a fifth of
                // its huge range.
                double newest = state.values[q], span = quantity.unit == QuantityUnit.Hours ? 48.0 : (quantity.max - quantity.min) * 0.2;
                for (int i = 0; i < WearCapacity; i++)
                {
                    double along = (i % (WearCapacity / 2)) / (double)(WearCapacity / 2);
                    wear.Push((float)Math.Max(quantity.min, newest - span + span * along));
                }
                WearStamp++;
            }
        }
    }

    public int RingCount => rings.Count;
}
