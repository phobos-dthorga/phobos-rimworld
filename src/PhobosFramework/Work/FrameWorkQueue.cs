using System;
using System.Collections.Generic;
using System.Diagnostics;
using Phobos.Framework.Rules;
using Verse;

namespace Phobos.Framework.Work;

/// <summary>
/// Runs <see cref="FrameWork"/> jobs one at a time, a slice per drawn frame, from <see cref="GameComponent_PhobosFrameWork"/>'s
/// per-frame update (which the game calls every frame, paused or not: Game.UpdatePlay, read from the 1.7 assembly, 7 Nov 2026).
/// Cheap while empty. Made from D'thorga Patches' strategic blast (7 November 2026) so any mod can spread a long job over frames.
/// </summary>
public static class FrameWorkQueue
{
    static readonly List<FrameWork> queue = new();
    static readonly Stopwatch watch = new();
    static Game? game;

    public static int Pending => queue.Count;

    /// <summary>The job running now, or next to run.</summary>
    public static FrameWork? Head => queue.Count == 0 ? null : queue[0];

    public static IReadOnlyList<FrameWork> All => queue;

    /// <summary>Queues a job, unless a queued job that has not started takes it over (<see cref="FrameWork.TryAbsorb"/>).</summary>
    public static void Enqueue(FrameWork work)
    {
        SameGame();
        for (int i = queue.Count - 1; i >= 0; i--)
        {
            if (!queue[i].Started && queue[i].TryAbsorb(work)) return;
        }
        queue.Add(work);
    }

    /// <summary>Forgets every job without finishing it (dev actions, a new game).</summary>
    public static void Clear()
    {
        foreach (FrameWork work in queue)
        {
            try
            {
                work.Dropped();
            }
            catch (Exception error)
            {
                Log.Warning(FrameworkMod.LogPrefix + " " + work.Label + " could not be dropped cleanly: " + error.Message);
            }
        }
        queue.Clear();
    }

    /// <summary>One slice of the job at the head of the queue.</summary>
    internal static void Run()
    {
        if (queue.Count == 0 || !SameGame()) return;
        FrameWork work = queue[0];
        try
        {
            if (!work.StillValid)
            {
                queue.RemoveAt(0);
                work.Dropped();
                return;
            }
            if (!work.Started) work.DoStart();
            work.Frames++;
            float budget = work.BudgetMs;
            int minimum = work.MinimumStepsPerSlice;
            watch.Restart();
            work.BeginSlice();
            try
            {
                int done = 0;
                while (!work.Finished && SliceBudgetRule.MayContinue(watch.Elapsed.TotalMilliseconds, budget, done, minimum))
                {
                    work.Step();
                    done++;
                }
            }
            finally
            {
                work.EndSlice();
                work.WorkMs += watch.Elapsed.TotalMilliseconds;
            }
            if (work.Finished)
            {
                queue.RemoveAt(0);
                work.Finish();
            }
        }
        catch (Exception error)
        {
            queue.Remove(work);
            Log.Error(FrameworkMod.LogPrefix + " " + work.Label + " stopped after an error: " + error);
        }
    }

    /// <summary>A new or loaded game starts with an empty queue: jobs belong to the game that queued them.</summary>
    static bool SameGame()
    {
        if (Current.Game != game)
        {
            queue.Clear();
            game = Current.Game;
        }
        return game != null;
    }
}

/// <summary>Drives <see cref="FrameWorkQueue"/> once per frame. Saves nothing.</summary>
public sealed class GameComponent_PhobosFrameWork : GameComponent
{
    public GameComponent_PhobosFrameWork(Game game) { }

    public override void GameComponentUpdate() => FrameWorkQueue.Run();
}
