using System.Collections.Generic;
using HarmonyLib;
using Phobos.Framework.Rules;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Framework.Work;

/// <summary>
/// The loop watch (Framework 0.205.0; the owner's choice of 21 Nov 2026, "notice and tell"; docs/development/pick-up-loops.md): it notices
/// a colonist who fetches the same thing and puts it away again, over and over, and tells the player once, naming the colonist and the
/// thing. It changes nothing in the game. What counts: a job whose first target is that very thing (by its ID, not its kind), a hauling
/// job (any job run by the game's haul-to-a-cell or haul-into-a-container drivers, or theirs) as putting it away and any other as fetching
/// it. The player's colonists only. Its memory is a few small records, forgotten once quiet, and never saved.
/// </summary>
public static class LoopWatch
{
    static readonly Dictionary<long, LoopTrail> trails = new();
    static Game? watchedIn;
    static int nextSweep;

    /// <summary>A job has started for this person: counted if it fetches or puts away a thing.</summary>
    public static void Started(Pawn pawn, Job job)
    {
        if (pawn.Faction != Faction.OfPlayer || !pawn.RaceProps.Humanlike) return;
        if (job.targetA.Thing is not { } thing || thing.def.category != ThingCategory.Item) return;
        if (watchedIn != Current.Game)
        {
            trails.Clear();
            watchedIn = Current.Game;
        }
        int now = Find.TickManager.TicksGame;
        Sweep(now);
        long key = ((long)pawn.thingIDNumber << 32) | (uint)thing.thingIDNumber;
        if (!trails.TryGetValue(key, out LoopTrail? trail)) trails[key] = trail = new LoopTrail();
        if (!LoopWatchRule.Record(trail, PutsAway(job), now)) return;
        Messages.Message("PhobosFramework_LoopNoticed".Translate(pawn.LabelShort.Named("PAWN"), thing.LabelNoCount.Named("THING"),
            trail.turns.Named("COUNT")).Resolve(), new LookTargets(pawn), MessageTypeDefOf.CautionInput);
        if (Seen.Wants("framework.loop-watch.noticed", pawn)) Seen.Note("framework.loop-watch.noticed", pawn, thing.def.label);
    }

    /// <summary>Whether a job puts its thing away: one run by the game's hauling drivers, or a mod's built on them.</summary>
    static bool PutsAway(Job job)
    {
        System.Type? driver = job.def.driverClass;
        return driver != null && (typeof(JobDriver_HaulToCell).IsAssignableFrom(driver) || typeof(JobDriver_HaulToContainer).IsAssignableFrom(driver));
    }

    static readonly List<long> stale = new();

    /// <summary>Forgets quiet records once an in-game hour, so the memory stays small.</summary>
    static void Sweep(int now)
    {
        if (now < nextSweep) return;
        nextSweep = now + GenDate.TicksPerHour;
        stale.Clear();
        foreach (KeyValuePair<long, LoopTrail> pair in trails)
        {
            if (LoopWatchRule.Stale(pair.Value, now)) stale.Add(pair.Key);
        }
        foreach (long key in stale) trails.Remove(key);
    }
}

/// <summary>
/// The loop watch's patch: a job has started. Reason for the patch: the game has no event for a job's start. A postfix; the game's method
/// always runs, and nothing here changes the job. It reads one setting first, and counts only a job that really became the person's
/// current one (another mod's prefix may have put a different job in its place).
/// </summary>
[HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.StartJob))]
public static class Patch_PawnJobTracker_StartJob_LoopWatch
{
    static readonly AccessTools.FieldRef<Pawn_JobTracker, Pawn> PawnOf = AccessTools.FieldRefAccess<Pawn_JobTracker, Pawn>("pawn");

    public static void Postfix(Pawn_JobTracker __instance, Job newJob)
    {
        if (!FrameworkMod.Settings.noticePickUpLoops || newJob == null || __instance.curJob != newJob) return;
        if (PawnOf(__instance) is { } pawn) LoopWatch.Started(pawn, newJob);
    }
}
