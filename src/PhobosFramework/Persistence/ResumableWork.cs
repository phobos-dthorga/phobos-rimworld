using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Framework.Persistence;

/// <summary>
/// The work left on unfinished jobs, kept by the thing worked on (Framework 0.274.0; the owner's report of 24 Nov 2026: setting up the
/// Billy began again from nothing each time a colonist came back to it). A job that ends early, for a meal, sleep, a raid or another
/// order, leaves its share of the work here, and the next person to take it up carries on from there, as with a building's frame. Kept by
/// the thing's load ID and a job key the mod chooses, so the save holds no reference to a thing that is gone. Read and written with
/// <see cref="ResumableWork"/>; a handful of entries in an ordinary colony, written when a job ends, never on a tick.
/// </summary>
public sealed class GameComponent_PhobosResumableWork : GameComponent
{
    Dictionary<string, float> shareLeft = new();
    Dictionary<string, int> touched = new();

    public GameComponent_PhobosResumableWork(Game game) { }

    /// <summary>The record of the game in play, or null before a game exists.</summary>
    public static GameComponent_PhobosResumableWork? Current => Verse.Current.Game?.GetComponent<GameComponent_PhobosResumableWork>();

    public float? ShareLeft(string key) => shareLeft.TryGetValue(key, out float share) ? share : null;

    public void Keep(string key, float share, int tick)
    {
        if (!ResumeRule.WorthKeeping(share))
        {
            Forget(key);
            return;
        }
        shareLeft[key] = share;
        touched[key] = tick;
    }

    public void Forget(string key)
    {
        shareLeft.Remove(key);
        touched.Remove(key);
    }

    public override void ExposeData()
    {
        if (Scribe.mode == LoadSaveMode.Saving) DropForgotten(Find.TickManager.TicksGame);
        Scribe_Collections.Look(ref shareLeft, "PhobosResumableWork_ShareLeft", LookMode.Value, LookMode.Value);
        Scribe_Collections.Look(ref touched, "PhobosResumableWork_Touched", LookMode.Value, LookMode.Value);
        shareLeft ??= new Dictionary<string, float>();
        touched ??= new Dictionary<string, int>();
    }

    void DropForgotten(int now)
    {
        foreach (string key in shareLeft.Keys.ToList())
        {
            if (!touched.TryGetValue(key, out int tick) || ResumeRule.Forgotten(tick, now, GenDate.TicksPerDay)) Forget(key);
        }
    }
}

/// <summary>Unfinished work by thing and job: what a job driver reads as it starts and writes as it ends.</summary>
public static class ResumableWork
{
    /// <summary>The key for one job on one thing; <paramref name="job"/> names the job and, where one thing can wait for several orders of
    /// it (set up or pack up, one repair or another), which.</summary>
    public static string Key(Thing thing, string job) => thing.GetUniqueLoadID() + "|" + job;

    /// <summary>The work to do now on this job: what was left last time, as a share of today's total, or all of it.</summary>
    public static float WorkLeft(string key, float total) =>
        (float)ResumeRule.WorkLeft(GameComponent_PhobosResumableWork.Current?.ShareLeft(key), total);

    /// <summary>The job ended with <paramref name="workLeft"/> of <paramref name="total"/> still to do: kept for the next person, or
    /// forgotten when it was finished or never begun.</summary>
    public static void Keep(string key, float workLeft, float total) =>
        GameComponent_PhobosResumableWork.Current?.Keep(key, (float)ResumeRule.ShareLeft(workLeft, total), Find.TickManager.TicksGame);

    public static void Forget(string key) => GameComponent_PhobosResumableWork.Current?.Forget(key);

    /// <summary>
    /// A timed wait that is picked up where it was left, in place of the game's Toils_General.Wait for a long piece of work: the ticks left
    /// are kept when the job ends early and forgotten when it finishes. Call it from the driver's MakeNewToils, which the game runs again
    /// for a job loaded from a save; the driver saves <paramref name="state"/> itself (see <see cref="WaitState.Expose"/>), so a load in
    /// the middle carries on too.
    /// </summary>
    public static Toil Wait(JobDriver driver, WaitState state, string key, int ticks, TargetIndex index)
    {
        // As the game's own wait (Toils_General.Wait, read 24 Nov 2026): stop on the spot, face the work, count by the ticks passed.
        Toil toil = ToilMaker.MakeToil("PhobosResumableWait");
        driver.AddFinishAction(_ => state.Remember());
        toil.initAction = () =>
        {
            toil.actor.pather.StopDead();
            state.Key = key;
            state.Total = ticks;
            state.Left = WorkLeft(key, ticks);
        };
        toil.defaultCompleteMode = ToilCompleteMode.Never;
        toil.handlingFacing = true;
        toil.tickIntervalAction = delta =>
        {
            toil.actor.rotationTracker.FaceTarget(toil.actor.CurJob.GetTarget(index));
            state.Left -= delta;
            if (state.Left > 0f) return;
            state.Left = 0f;
            toil.actor.jobs.curDriver.ReadyForNextToil();
        };
        toil.WithProgressBar(index, () => state.Total <= 0f ? 1f : 1f - state.Left / state.Total);
        return toil;
    }

    /// <summary>What a resumable wait keeps on its job driver while it runs.</summary>
    public sealed class WaitState
    {
        public string? Key;
        public float Total;
        public float Left;

        /// <summary>The job ended, finished or not: keep what is left, or forget a finished wait.</summary>
        public void Remember()
        {
            if (Key == null) return;
            if (Left <= 0f) Forget(Key);
            else Keep(Key, Left, Total);
        }

        /// <summary>Call from the driver's ExposeData.</summary>
        public void Expose(string prefix)
        {
            Scribe_Values.Look(ref Key, prefix + "_Key");
            Scribe_Values.Look(ref Total, prefix + "_Total");
            Scribe_Values.Look(ref Left, prefix + "_Left");
        }
    }
}
