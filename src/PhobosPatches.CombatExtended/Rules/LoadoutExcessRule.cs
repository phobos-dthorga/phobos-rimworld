using System.Collections.Generic;

namespace Phobos.CombatExtendedPatches.Rules;

/// <summary>One slot of a loadout, by name: the kind of goods it names, or the kinds a generic slot covers among those carried; and how many.</summary>
public readonly record struct LoadoutSlotView(string? Kind, IReadOnlyCollection<string>? Covers, int Count);

/// <summary>
/// Whether a loadout would drop some of a kind of goods (Patches 0.29.1; docs/development/soldiers-keep-their-guns.md, "Sidearms that
/// loop"): Combat Extended's own rule for what a person carries in excess (Utility_HoldTracker.GetExcessThing, read 21 Nov 2026 from its
/// public source), for one kind, game-free. Each slot takes its count off what is carried: a named slot off its own kind, a generic one off
/// the kinds it covers in the order they are carried. A kind with some left over is dropped when a hold record allows less than is left;
/// with no record, when the loadout drops what it does not list, or when a slot named it and some is still over (the extra copy).
/// </summary>
public static class LoadoutExcessRule
{
    /// <param name="carried">What is carried, by kind, in the order Combat Extended counts it: the weapon in hand, then the pack.</param>
    /// <param name="slots">The loadout's slots, in its order, ad-hoc slots included.</param>
    /// <param name="kind">The kind asked about.</param>
    /// <param name="holdCount">How many of it a hold record allows, or null for no record.</param>
    /// <param name="dropUndefined">Whether the loadout drops what it does not list.</param>
    public static bool WouldDrop(IReadOnlyList<KeyValuePair<string, int>> carried, IReadOnlyList<LoadoutSlotView> slots, string kind, int? holdCount,
        bool dropUndefined)
    {
        var order = new List<string>();
        var left = new Dictionary<string, int>();
        foreach (KeyValuePair<string, int> pair in carried)
        {
            if (!left.TryGetValue(pair.Key, out int had))
            {
                order.Add(pair.Key);
                had = 0;
            }
            left[pair.Key] = had + pair.Value;
        }
        var inLoadout = new HashSet<string>();
        foreach (LoadoutSlotView slot in slots)
        {
            if (slot.Kind != null && left.ContainsKey(slot.Kind))
            {
                left[slot.Kind] -= slot.Count;
                if (left[slot.Kind] <= 0) left.Remove(slot.Kind);
                else inLoadout.Add(slot.Kind);
            }
            if (slot.Covers == null) continue;
            int wanted = slot.Count;
            var used = new List<string>();
            foreach (string key in order)
            {
                if (!left.ContainsKey(key) || !Contains(slot.Covers, key)) continue;
                left[key] -= wanted;
                if (left[key] <= 0)
                {
                    // As Combat Extended counts it: what the slot still wants is what this kind fell short by.
                    wanted = -left[key];
                    used.Add(key);
                }
                else
                {
                    inLoadout.Add(key);
                    break;
                }
            }
            foreach (string key in used) left.Remove(key);
        }
        if (!left.TryGetValue(kind, out int over)) return false;
        return holdCount is { } held ? held < over : dropUndefined || inLoadout.Contains(kind);
    }

    static bool Contains(IReadOnlyCollection<string> kinds, string kind)
    {
        foreach (string each in kinds)
        {
            if (each == kind) return true;
        }
        return false;
    }
}
