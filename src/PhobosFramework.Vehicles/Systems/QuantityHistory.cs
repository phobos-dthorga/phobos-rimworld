using System;
using System.Collections.Generic;
using Phobos.Framework.Persistence;
using Phobos.Framework.Rules;
using Phobos.Framework.Vehicles.Rules;

namespace Phobos.VehicleSystems;

/// <summary>
/// The last day of each shown quantity, one sample per rare tick, for the Systems tab's graphs. Saved as one compact string per
/// quantity (the owner's exception to "derived state is never saved", 9 Nov 2026: a graph cannot be rebuilt); a few kilobytes per
/// vehicle. Quantities a system no longer has are dropped on load.
/// </summary>
public sealed class QuantityHistory
{
    public const int Hours = 24;
    public const int StrideTicks = 250;
    public static readonly int Capacity = HistoryRing.SamplesFor(Hours, StrideTicks, ElapsedRule.TicksPerHour);

    readonly Dictionary<string, HistoryRing> rings = new();
    int lastSampleTick = -1;

    static string Key(PhobosVehicleSystemDef def, QuantitySpec quantity) => def.defName + "_" + quantity.name;

    /// <summary>The ring of a quantity, or null before its first sample.</summary>
    public HistoryRing? For(SystemState state, int quantity) =>
        rings.TryGetValue(Key(state.Def, state.Def.quantities[quantity]), out HistoryRing? ring) ? ring : null;

    /// <summary>One sample of every shown quantity, no more often than the stride.</summary>
    public void Sample(IReadOnlyList<SystemState> states, int now)
    {
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
            }
        }
    }

    public int RingCount => rings.Count;
}
