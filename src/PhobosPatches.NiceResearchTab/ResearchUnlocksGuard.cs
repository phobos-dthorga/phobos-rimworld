using System.Collections.Generic;
using System.Threading;
using HarmonyLib;
using Verse;

namespace Phobos.NiceResearchTabPatches;

/// <summary>
/// A research project's unlock list is read one caller at a time and never hands out a blank entry (Patches 0.38.0; temporary,
/// docs/development/temporary-patches.md row 23).
/// <para>
/// The fault, seen in the owner's log of 23 Nov 2026: opening Nice Research Tab, the game threw "Value cannot be null (Parameter 'key')" in
/// ResearchPrerequisitesUtility.ComputeResearchPrerequisites, because some project's UnlockedDefs held a null. That stopped Nice Research
/// Tab's details panel half-built, and its Start research button then threw every frame (293 errors in two seconds) until the window was
/// closed; the null stayed in the cached list until the game was restarted. The game's own getter cannot make a null (it sorts by label,
/// which would throw first), and every mod that adds to the list adds only real Defs. What fits: UnlockedDefs caches one list per project,
/// and Vanilla Expanded Framework's patches edit that shared list on each read (PipeSystem adds its processes, VEF removes hidden
/// designators). Nice Research Tab reads every project's list from a background task (ExtraCategoryDef.CacheAll, started by NodeServer once
/// the tree is solved) while the main thread reads the same lists, so two of those edits can run on one list at once, and a List edited by
/// two threads together can be left with an empty slot inside it. The background task's "Finished caching categories" never appeared in
/// that log. Likely, not proven: the owner's restart opened the tab cleanly, as a timing fault would.
/// </para>
/// <para>
/// What it does: the prefix, first of all patches, takes a lock that the finalizer, after every postfix, gives back, so the getter and
/// every mod's patches on it run for one caller at a time and no two edits meet. The postfix, last of all, takes out any null that is there
/// anyway and says once per project in the log which project held it, so a cause other than the race can be named. Uncontended, the lock
/// costs well under a microsecond per read. It touches only the game's own getter; nothing is saved.
/// </para>
/// </summary>
[HarmonyPatch(typeof(ResearchProjectDef), nameof(ResearchProjectDef.UnlockedDefs), MethodType.Getter)]
public static class ResearchUnlocksGuard
{
    /// <summary>Held from before the first patch on the getter to after the last; also guards the two collections below.</summary>
    static readonly object Gate = new();

    /// <summary>Projects already reported this session, by defName, so each is named in the log once.</summary>
    static readonly HashSet<string> reported = new();

    /// <summary>Reports waiting for the main thread: the log and the seen-in-play notes are written there.</summary>
    static readonly List<string> pending = new();

    /// <summary>Whether a reader has had to wait for another since the last note: the race happening, and held off.</summary>
    static bool waitedUnnoted;

    [HarmonyPriority(Priority.First)]
    public static void Prefix(out bool __state)
    {
        __state = Monitor.TryEnter(Gate);
        if (__state) return;
        Monitor.Enter(Gate, ref __state);
        waitedUnnoted = true;
    }

    [HarmonyPriority(Priority.Last)]
    public static void Postfix(ResearchProjectDef __instance, List<Def> __result)
    {
        if (__result != null && __result.IndexOf(null!) >= 0)
        {
            int removed = __result.RemoveAll(d => d == null);
            if (reported.Add(__instance.defName))
            {
                string where = UnityData.IsInMainThread ? "the main thread" : "another thread";
                pending.Add(removed + " blank entr" + (removed == 1 ? "y" : "ies") + " taken out of the unlock list of research project "
                    + __instance.LabelCap + " (" + __instance.defName + "), found on " + where);
            }
        }
        if ((pending.Count > 0 || waitedUnnoted) && UnityData.IsInMainThread) Report();
    }

    public static void Finalizer(bool __state)
    {
        if (__state) Monitor.Exit(Gate);
    }

    /// <summary>Writes the waiting reports and notes, on the main thread, while the lock is held.</summary>
    static void Report()
    {
        if (waitedUnnoted)
        {
            waitedUnnoted = false;
            Seen.Note("patches.research-unlocks-reader-waited");
        }
        if (pending.Count == 0) return;
        foreach (string line in pending)
        {
            NiceResearchTabPatchesMod.Logger.Warn(line + ". The research tab would have failed to open over it. Another mod's patch put it there; "
                + "this project's name says which mods to look at.");
        }
        if (Seen.Wants("patches.research-unlocks-blank-removed")) Seen.Note("patches.research-unlocks-blank-removed", null, string.Join("; ", pending));
        pending.Clear();
    }
}
