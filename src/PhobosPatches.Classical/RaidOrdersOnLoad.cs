using System;
using System.Collections.Generic;
using HarmonyLib;
using Phobos.Framework.Persistence;
using Verse;
using Verse.AI.Group;

namespace Phobos.ClassicalPatches;

/// <summary>
/// A group whose orders the game could not rebuild from a save gets them back (Patches 0.25.0; temporary, docs/development/temporary-patches.md
/// row 20). The game rebuilds a saved object by making an empty one and reading its saved values into it. Vanilla Factions Expanded - Classical's
/// scorpion raid (VFEC.LordJob_ScorpionRaid) has only a constructor that takes arguments, so the game cannot make the empty one: it reports
/// "Constructor on type 'VFEC.LordJob_ScorpionRaid' not found", and the group loads with no orders at all. Everything that then asks the group
/// what its people should do fails, every raider every tick, and the map's pathfinder fails with it (the owner's PG15, 19 Nov 2026: 15,000 errors
/// in two and a half minutes, the log's limit reached, raiders unable to act or leave). This postfix runs while the group is still being read:
/// where the game's own try left no orders, it tries once more through the constructor the class does have, with empty values (D'thorga Framework's
/// DeepLoad), and the orders then read their saved values, their faction and their after-load step as any other would. It matches the fault, not
/// the class, so it does nothing for a group that loaded and stands down by itself once the raid's class can be built with no arguments.
/// </summary>
[HarmonyPatch(typeof(Lord), nameof(Lord.ExposeData))]
public static class RaidOrdersOnLoadPatch
{
    const string OrdersLabel = "lordJob";

    /// <summary>The classes of the orders rebuilt since the last report: filled on the loading thread, reported on the main one.</summary>
    static readonly List<string> rebuilt = new();

    public static void Postfix(ref LordJob ___curJob)
    {
        if (Scribe.mode != LoadSaveMode.LoadingVars || ___curJob != null) return;
        LordJob? orders = null;
        if (!DeepLoad.RetryWithEmptyArguments(ref orders, OrdersLabel, out Type? type) || orders == null) return;
        ___curJob = orders;
        lock (rebuilt)
        {
            rebuilt.Add(type?.FullName ?? "unknown");
            if (rebuilt.Count == 1) LongEventHandler.ExecuteWhenFinished(Report);
        }
    }

    static void Report()
    {
        string classes;
        int count;
        lock (rebuilt)
        {
            count = rebuilt.Count;
            if (count == 0) return;
            classes = string.Join(", ", new HashSet<string>(rebuilt));
            rebuilt.Clear();
        }
        ClassicalPatchesMod.Logger.Info(count + " group(s) loaded with their orders rebuilt (" + classes + "). The game could not build them from the save by itself: "
            + "its error above, \"Constructor on type ... not found\", is that first try, and nothing is wrong now.");
        if (Seen.Wants("patches.raid-orders-rebuilt-on-load")) Seen.Note("patches.raid-orders-rebuilt-on-load", null, count + " group(s): " + classes);
    }
}
