using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Phobos.MedicalSaveBridge.Rules;
using Verse;

namespace Phobos.MedicalSaveBridge.Services;

/// <summary>
/// What the bridge knows about the save being loaded. Recomputed whenever the game reads a save's header, held in memory only
/// and never saved: the next load starts from the header again, and a save written with every active mod makes it inert.
/// </summary>
public static class BridgeState
{
    public const string LogPrefix = "[D'thorga Medical Save Bridge]";

    /// <summary>True while a save that lacks some active mod is loading. Everything the bridge does is gated on this.</summary>
    public static bool Active { get; private set; }

    /// <summary>Lower-cased package IDs of the active mods the save was written without.</summary>
    public static HashSet<string> NewModIds { get; private set; } = new HashSet<string>();

    /// <summary>How many pawns More Injuries would have wiped of wounds and missing parts, had its first-load flag been left set.</summary>
    public static int WipesPrevented { get; private set; }

    static readonly Dictionary<BodyDef, int[]?> maps = new Dictionary<BodyDef, int[]?>();
    static readonly Dictionary<BodyDef, int> remapped = new Dictionary<BodyDef, int>();
    static readonly Dictionary<string, int> outOfRange = new Dictionary<string, int>();
    static readonly Dictionary<string, int> upgraded = new Dictionary<string, int>();

    /// <summary>Called when the game reads a save's header. <paramref name="savedModIds"/> is the header's mod list.</summary>
    public static void Recompute(List<string>? savedModIds)
    {
        Reset();
        NewModIds = ModListRule.NewModIds(savedModIds, LoadedModManager.RunningMods.Select(mod => mod.PackageId));
        Active = NewModIds.Count > 0;
        if (!Active) return;
        // Eager, so the gate line can name the bodies that gained parts.
        foreach (BodyDef body in DefDatabase<BodyDef>.AllDefs)
        {
            MapFor(body);
        }
    }

    /// <summary>The load is over: stop acting. The maps stay for the dev-mode dump until the next header is read.</summary>
    public static void Finish() => Active = false;

    public static void Reset()
    {
        Active = false;
        NewModIds = new HashSet<string>();
        WipesPrevented = 0;
        maps.Clear();
        remapped.Clear();
        outOfRange.Clear();
        upgraded.Clear();
    }

    /// <summary>Whether a body part record's def comes from a mod the save never had.</summary>
    public static bool IsNewRecord(BodyPartRecord record)
    {
        ModContentPack? pack = record.def?.modContentPack;
        return pack != null && NewModIds.Contains(pack.PackageId.ToLowerInvariant());
    }

    /// <summary>The old-to-new index map for a body, or null when none of its parts is new.</summary>
    public static int[]? MapFor(BodyDef body)
    {
        if (maps.TryGetValue(body, out int[]? map)) return map;
        List<BodyPartRecord> parts = body.AllParts;
        var isNew = new bool[parts.Count];
        for (int i = 0; i < parts.Count; i++)
        {
            isNew[i] = IsNewRecord(parts[i]);
        }
        map = PartIndexRule.OldToNewIndices(isNew);
        maps[body] = map;
        return map;
    }

    /// <summary>The bodies that gained parts, with their maps.</summary>
    public static IEnumerable<KeyValuePair<BodyDef, int[]>> BridgedBodies =>
        maps.Where(pair => pair.Value != null).Select(pair => new KeyValuePair<BodyDef, int[]>(pair.Key, pair.Value!));

    public static void CountRemapped(BodyDef body) => Bump(remapped, body);

    public static void CountOutOfRange(BodyDef body, int index) => Bump(outOfRange, body.defName + " index " + index);

    public static void CountUpgraded(Type from, Type to) => Bump(upgraded, from.FullName + " to " + to.FullName);

    public static void CountWipePrevented() => WipesPrevented++;

    static void Bump<TKey>(Dictionary<TKey, int> counts, TKey key) where TKey : notnull
    {
        counts.TryGetValue(key, out int count);
        counts[key] = count + 1;
    }

    /// <summary>One line saying whether the bridge acts on this load, and why.</summary>
    public static string DescribeGate()
    {
        if (!Active)
        {
            return LogPrefix + " INERT: the save was written with every mod that is active now, so there is nothing to bridge.";
        }
        var text = new StringBuilder(LogPrefix).Append(" ACTIVE: the save was written without ")
            .Append(string.Join(", ", NewModIds.OrderBy(id => id)))
            .Append(". ");
        List<KeyValuePair<BodyDef, int[]>> bodies = BridgedBodies.ToList();
        if (bodies.Count == 0)
        {
            text.Append("None of them adds body parts.");
        }
        else
        {
            text.Append("Saved body parts will be re-numbered for: ")
                .Append(string.Join("; ", bodies.Select(pair => pair.Key.defName + " (" + (pair.Key.AllParts.Count - pair.Value.Length) + " new parts)")))
                .Append('.');
        }
        return text.ToString();
    }

    /// <summary>What the bridge did during the load, for the log.</summary>
    public static string DescribeCounts()
    {
        var text = new StringBuilder(LogPrefix).Append(" Load finished.");
        text.Append(" Body parts re-numbered: ");
        text.Append(remapped.Count == 0 ? "none" : string.Join(", ", remapped.Select(pair => pair.Value + " on " + pair.Key.defName)));
        text.Append(". Hediff classes upgraded: ");
        text.Append(upgraded.Count == 0 ? "none" : string.Join(", ", upgraded.Select(pair => pair.Value + " from " + pair.Key)));
        text.Append(". More Injuries first-load wipes prevented: ").Append(WipesPrevented).Append('.');
        if (outOfRange.Count > 0)
        {
            text.Append(" Saved part indices that fit no old part, left as the game resolved them: ")
                .Append(string.Join(", ", outOfRange.Select(pair => pair.Value + " of " + pair.Key)))
                .Append('.');
        }
        return text.ToString();
    }
}
